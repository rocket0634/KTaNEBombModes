using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using Newtonsoft.Json;
using System.IO;
using System.Reflection;
using System.Linq;

public enum BombMode
{
    Normal,
    Time,
    //VS,
    Zen
}

[RequireComponent(typeof(KMService))]
[RequireComponent(typeof(KMGameInfo))]
class Modes : MonoBehaviour
{
    private List<Bomb> Bombs = null;
    public static BombMode mode = BombMode.Normal;
    private readonly List<TimerComponent> Timers = new List<TimerComponent>();
    private ModesSettings Settings = new ModesSettings();
    private float normalRate = 0;
    private float startTime;
    private float timePenalty;
    private bool TwitchPlaysActive = false;
    private void Awake()
    {
        ModConfig modConfig = new ModConfig("ModeSettings", typeof(ModesSettings));
        Settings = (ModesSettings) modConfig.Settings;
        Settings.ModeActive = Settings.ModeActive.ToLowerInvariant();
        if (Settings.ModeActive.Equals("zen")) mode = BombMode.Zen;
        else if (Settings.ModeActive.Equals("time")) mode = BombMode.Time;
        else mode = BombMode.Normal;
        GetSettings();
        GetComponent<KMGameInfo>().OnStateChange += delegate (KMGameInfo.State state)
        {
            Debug.LogFormat("[Modes] Updating services...");
            StartCoroutine(UpdateServices());
            if (state == KMGameInfo.State.Gameplay && !TwitchPlaysActive && mode == BombMode.Zen)
            {
                StartCoroutine(CheckForBomb());
            }
            else if (TwitchPlaysActive)
            {
                StopCoroutine(CheckForBomb());
            }
        };
    }

    private void GetModServices()
    {
        KMService[] modServices = FindObjectsOfType<KMService>();
        List<string> temp = new List<string>();
        foreach (KMService modService in modServices)
        {
            Debug.LogFormat("[Modes] Checking service {0}", modService);
            if (modService.name.StartsWith("TwitchPlaysService")) temp.Add("TwitchPlaysService");
        }
        if (temp.Contains("TwitchPlaysService"))
        {
            TwitchPlaysActive = true;
            Debug.LogFormat("[Modes] Conflict Detected");
        }
        else TwitchPlaysActive = false;
    }

    private IEnumerator CheckForBomb()
    {
        yield return new WaitUntil(() => (SceneManager.Instance.GameplayState.Bombs != null && SceneManager.Instance.GameplayState.Bombs.Count > 0));
        Bombs = SceneManager.Instance.GameplayState.Bombs;

        foreach (Bomb bomb in Bombs)
        {
            if (bomb.GetTimer() == null || bomb.GetTimer().GetRate() < 0) continue;
            normalRate = -bomb.GetTimer().GetRate();
            StartCoroutine(CheckModules(bomb));
            bomb.GetTimer().text.color = Color.blue;
            bomb.GetTimer().SetRateModifier(normalRate);
            bomb.GetTimer().SetTimeRemaing(1);
            bomb.NumStrikesToLose += 1;
        }
        
    }

    private void CheckForStrikes(Bomb bomb)
    {
        bomb.GetTimer().SetRateModifier(normalRate);
        bomb.GetTimer().SetTimeRemaing(bomb.GetTimer().TimeRemaining + (timePenalty));
        bomb.NumStrikesToLose += 1;
    }

    private IEnumerator UpdateServices()
    {
        GetModServices();
        yield return null;
    }

    private IEnumerator CheckModules(Bomb bomb)
    {
        foreach (BombComponent module in bomb.BombComponents) {
            switch (module.ComponentType)
            {
                case Assets.Scripts.Missions.ComponentTypeEnum.Mod:
                    switch (module.GetComponent<KMBombModule>().ModuleType)
                    {
                        case "TurnTheKey":
                            yield return new CaseTTK(module, bomb, startTime);
                            break;
                        case "ButtonV2":
                            yield return new CaseSquare(module, bomb);
                            break;
                        case "theSwan":
                            yield return new CaseSwan(module, bomb);
                            break;
                    }
                    break;
                case Assets.Scripts.Missions.ComponentTypeEnum.BigButton:
                    yield return new CaseButton(module, bomb);
                    break;
            }
            module.OnStrike += delegate { CheckForStrikes(bomb); return false; };
        }
    }

    private void GetSettings()
    {
        var temp = new List<string>() { Settings.StartTime, Settings.TimePenalty};
        float result = 0;
        float result2 = 0;
        bool check;
        foreach (string time in temp)
        {
            var results = time.Split(':');
            if (time.Contains("m") || time.Contains(":"))
            {
                results = time.Split(':', 'm');
                results[0] = new string(results[0].Where(c => char.IsDigit(c)).ToArray());
                int end = results.Length - 1;
                results[end] = new string(results[end].Where(c => char.IsDigit(c)).ToArray());
                check = float.TryParse(results[end], out result2);
                result = result2;
                check = float.TryParse(results[0], out result2);
                result += (result2 * 60);
            }
            else if (time.EndsWith("s"))
            {
                check = float.TryParse(time.Replace("s", ""), out result);
                result = result * 60;
            }
            else if (time.Length == 2 && float.TryParse(time, out result)) { }
            if (time.Equals(Settings.StartTime)) startTime = result;
            else if (time.Equals(Settings.TimePenalty)) timePenalty = result;
        }
    }
}

class ModesSettings
{
    public string StartTime = "30m";
    public string TimePenalty = "1m";
    public string ModeActive = "Zen";
}

class ModConfig
{
    public ModConfig(string name, Type settingsType)
    {
        _filename = name;
        _settingsType = settingsType;
    }

    readonly string _filename = null;
    readonly Type _settingsType = null;

    string SettingsPath
    {
        get
        {
            return Path.Combine(Application.persistentDataPath, "Modsettings\\" + _filename + ".json");
        }
    }

    public object Settings
    {
        get
        {
            try
            {
                if (!File.Exists(SettingsPath))
                {
                    File.WriteAllText(SettingsPath, JsonConvert.SerializeObject(Activator.CreateInstance(_settingsType), Formatting.Indented));
                }

                return JsonConvert.DeserializeObject(File.ReadAllText(SettingsPath), _settingsType);
            }
            catch
            {
                return Activator.CreateInstance(_settingsType);
            }
        }

        set
        {
            if (value.GetType() == _settingsType)
            {
                File.WriteAllText(SettingsPath, JsonConvert.SerializeObject(value, Formatting.Indented));
            }
        }
    }

    public override string ToString()
    {
        return JsonConvert.SerializeObject(Settings, Formatting.Indented);
    }
}

public class CaseTTK
{
    public CaseTTK(BombComponent bombComponent, Bomb bomb, float startTime)
    {
        module = bombComponent;
        currentBomb = bomb;
        initialTime = startTime;
        _lock = (MonoBehaviour)_lockField.GetValue(module.GetComponent(_componentType));
        if (SceneManager.Instance.GameplayState.Bombs != null) _lock?.StartCoroutine(ReWriteTTK());
        module.GetComponent<KMBombModule>().OnActivate = OnActivate;
    }

    private bool IsTargetTurnTimeCorrect(int turnTime)
    {
        return turnTime < 0 || turnTime == (int)_targetTimeField.GetValue(module.GetComponent(_componentType));
    }

    private bool CanTurnEarlyWithoutStrike(int turnTime)
    {
        int time = (int)_targetTimeField.GetValue(module.GetComponent(_componentType));
        int timeRemaining = (int)currentBomb.GetTimer().TimeRemaining;
        if ((Modes.mode.Equals(BombMode.Zen) && timeRemaining > time)) return false;
        if (Modes.mode.Equals(BombMode.Zen)) 
            return !((int)_targetTimeField.GetValue(module.GetComponent(_componentType)) < time) && IsTargetTurnTimeCorrect(turnTime);
        return false;
    }

    private bool OnKeyTurn(int turnTime = -1)
    {
        bool result = CanTurnEarlyWithoutStrike(turnTime);
        _lock.StartCoroutine(DelayKeyTurn(!result));
        return false;
    }

    private IEnumerator DelayKeyTurn(bool restoreBombTimer, bool causeStrikeIfWrongTime = true, bool bypassSettings = false)
    {
        Animator keyAnimator = (Animator)_keyAnimatorField.GetValue(module.GetComponent(_componentType));
        KMAudio keyAudio = (KMAudio)_keyAudioField.GetValue(module.GetComponent(_componentType));
        int time = (int)_targetTimeField.GetValue(module.GetComponent(_componentType));

        if (!restoreBombTimer)
        {
            currentBomb.GetTimer().TimeRemaining = time + 0.5f + Time.deltaTime;
            yield return null;
        }
        else if (causeStrikeIfWrongTime && time != (int)Mathf.Floor(currentBomb.GetTimer().TimeRemaining))
        {
            module.GetComponent<KMBombModule>().HandleStrike();
            keyAnimator.SetTrigger("WrongTurn");
            keyAudio.PlaySoundAtTransform("WrongKeyTurnFK", module.transform);
            yield return null;
            if (!(bool)_solvedField.GetValue(module.GetComponent(_componentType)))
            {
                yield break;
            }
        }

        module.GetComponent<KMBombModule>().HandlePass();
        _keyUnlockedField.SetValue(module.GetComponent(_componentType), true);
        _solvedField.SetValue(module.GetComponent(_componentType), true);
        keyAnimator.SetBool("IsUnlocked", true);
        keyAudio.PlaySoundAtTransform("TurnTheKeyFX", module.transform);
        yield return null;
    }

    public IEnumerable<Dictionary<string, T>> QueryWidgets<T>(string queryKey, string queryInfo = null)
    {
        return currentBomb.WidgetManager.GetWidgetQueryResponses(queryKey, queryInfo).Select(str => JsonConvert.DeserializeObject<Dictionary<string, T>>(str));
    }

    private void OnActivate()
    {
        string serial = QueryWidgets<string>(KMBombInfo.QUERYKEY_GET_SERIAL_NUMBER).First()["serial"];
        TextMesh textMesh = (TextMesh)_displayField.GetValue(module.GetComponent(_componentType));
        _activatedField.SetValue(module.GetComponent(_componentType), true);

        if (string.IsNullOrEmpty(_previousSerialNumber) || !_previousSerialNumber.Equals(serial) || _keyTurnTimes.Count == 0)
        {
            if (!string.IsNullOrEmpty(_previousSerialNumber) && _previousSerialNumber.Equals(serial))
            {
                Animator keyAnimator = (Animator)_keyAnimatorField.GetValue(module.GetComponent(_componentType));
                KMAudio keyAudio = (KMAudio)_keyAudioField.GetValue(module.GetComponent(_componentType));
                module.GetComponent<KMBombModule>().HandlePass();
                _keyUnlockedField.SetValue(module.GetComponent(_componentType), true);
                _solvedField.SetValue(module.GetComponent(_componentType), true);
                keyAnimator.SetBool("IsUnlocked", true);
                keyAudio.PlaySoundAtTransform("TurnTheKeyFX", module.transform);
                textMesh.text = "88:88";
                return;
            }
            _keyTurnTimes.Clear();
            for (int i = (Modes.mode.Equals(BombMode.Zen) ? 45 : 3); i < (Modes.mode.Equals(BombMode.Zen) ? initialTime : (currentBomb.GetTimer().TimeRemaining - 45)); i += 3)
            {
                _keyTurnTimes.Add(i);
            }
            if (_keyTurnTimes.Count == 0) _keyTurnTimes.Add((int)(currentBomb.GetTimer().TimeRemaining / 2f));

            _keyTurnTimes = _keyTurnTimes.Shuffle().ToList();
            _previousSerialNumber = serial;
        }
        _targetTimeField.SetValue(module.GetComponent(_componentType), _keyTurnTimes[0]);

        string display = $"{_keyTurnTimes[0] / 60:00}:{_keyTurnTimes[0] % 60:00}";
        _keyTurnTimes.RemoveAt(0);

        textMesh.text = display;
    }

    private IEnumerator ReWriteTTK()
    {
        yield return new WaitUntil(() => (bool)_activatedField.GetValue(module.GetComponent(_componentType)));
        yield return new WaitForSeconds(0.1f);
        _stopAllCorotinesMethod.Invoke(module.GetComponent(_componentType), null);

        ((KMSelectable)_lock).OnInteract = () => OnKeyTurn();
        int expectedTime = (int)_targetTimeField.GetValue(module.GetComponent(_componentType));
        if (Math.Abs(expectedTime - currentBomb.GetTimer().TimeRemaining) < 30)
        {
            yield return new WaitForSeconds(0.1f);
            yield break;
        }

        while (!module.IsSolved)
        {
            int time = Mathf.FloorToInt(currentBomb.GetTimer().TimeRemaining);
            if (((!Modes.mode.Equals(BombMode.Zen) && time < expectedTime) || (Modes.mode.Equals(BombMode.Zen) && time > expectedTime)) &&
                !(bool)_solvedField.GetValue(module.GetComponent(_componentType)))
            {
                module.GetComponent<KMBombModule>().HandleStrike();
            }
            yield return new WaitForSeconds(2.0f);
        }
    }


    static CaseTTK()
    {
        _componentType = ReflectionHelper.FindType("TurnKeyModule");
        _lockField = _componentType.GetField("Lock", BindingFlags.Public | BindingFlags.Instance);
        _activatedField = _componentType.GetField("bActivated", BindingFlags.NonPublic | BindingFlags.Instance);
        _solvedField = _componentType.GetField("bUnlocked", BindingFlags.NonPublic | BindingFlags.Instance);
        _targetTimeField = _componentType.GetField("mTargetSecond", BindingFlags.NonPublic | BindingFlags.Instance);
        _stopAllCorotinesMethod = _componentType.GetMethod("StopAllCoroutines", BindingFlags.Public | BindingFlags.Instance);
        _keyAnimatorField = _componentType.GetField("KeyAnimator", BindingFlags.Public | BindingFlags.Instance);
        _displayField = _componentType.GetField("Display", BindingFlags.Public | BindingFlags.Instance);
        _keyUnlockedField = _componentType.GetField("bUnlocked", BindingFlags.NonPublic | BindingFlags.Instance);
        _keyAudioField = _componentType.GetField("mAudio", BindingFlags.NonPublic | BindingFlags.Instance);
        _keyTurnTimes = new List<int>();
    }

    private static Type _componentType = null;
    private static FieldInfo _lockField = null;
    private static FieldInfo _activatedField = null;
    private static FieldInfo _solvedField = null;
    private static FieldInfo _targetTimeField = null;
    private static FieldInfo _keyAnimatorField = null;
    private static FieldInfo _displayField = null;
    private static FieldInfo _keyUnlockedField = null;
    private static FieldInfo _keyAudioField = null;
    private static MethodInfo _stopAllCorotinesMethod = null;

    private static List<int> _keyTurnTimes = null;
    private static string _previousSerialNumber = null;

    private MonoBehaviour _lock = null;
    private BombComponent module;
    private Bomb currentBomb;
    private readonly float initialTime;
}

public class CaseSquare
{
    public CaseSquare(BombComponent bombComponent, Bomb bomb)
    {

    }
}

public class CaseSwan
{
    public CaseSwan(BombComponent bombComponent, Bomb bomb)
    {

    }
}

public class CaseButton
{
    public CaseButton(BombComponent bombComponent, Bomb bomb)
    {

    }
}

public static class ReflectionHelper
{
    public static Type FindType(string fullName)
    {
        return AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetSafeTypes()).FirstOrDefault(t => t.FullName != null && t.FullName.Equals(fullName));
    }

    public static Type FindType(string fullName, string assemblyName)
    {
        return AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetSafeTypes()).FirstOrDefault(t => t.FullName != null && t.FullName.Equals(fullName) && t.Assembly.GetName().Name.Equals(assemblyName));
    }

    public static IEnumerable<Type> GetSafeTypes(this Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException e)
        {
            return e.Types.Where(x => x != null);
        }
        catch (Exception)
        {
            return new List<Type>();
        }
    }
}

public static class GeneralExtensions
{
    public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source)
    {
        return source.OrderBy(x => UnityEngine.Random.value);
    }
}