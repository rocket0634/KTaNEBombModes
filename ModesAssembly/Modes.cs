using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using Newtonsoft.Json;
using System.IO;
using System.Text.RegularExpressions;
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
    private BombMode mode = BombMode.Normal;
    private List<TimerComponent> Timers = new List<TimerComponent>();
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
            bomb.GetTimer().text.color = Color.blue;
            bomb.GetTimer().SetRateModifier(normalRate);
            bomb.GetTimer().SetTimeRemaing(1);
            bomb.NumStrikesToLose += 1;
            foreach (BombComponent bombComponent in bomb.BombComponents)
            {
                bombComponent.OnStrike += delegate{ CheckForStrikes(bomb); return false; };
            }
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

    string _filename = null;
    Type _settingsType = null;

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

public abstract class ComponentTimeBase
{

}