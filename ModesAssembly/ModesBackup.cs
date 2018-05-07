/*using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using JetBrains.Annotations;
using System.Reflection;
using Assets.Scripts.Pacing;
using System.IO;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

public enum BombMode
{
    Normal,
    Time,
    //VS,
    Zen
}

public static class BombModes
{
    public static BombMode currentMode = BombMode.Normal;
    public static BombMode nextMode = BombMode.Normal;
    public static bool BombActive { get; set; }
    public static string GetName(BombMode mode) { return Enum.GetName(typeof(BombMode), mode); }

    public static bool InMode(BombMode mode) { return currentMode == mode; }

    public static bool Set(BombMode mode, bool state = true)
    {
        if (state == false) mode = BombMode.Normal;

        nextMode = mode;
        if (!BombActive)
        {
            currentMode = mode;
            return true;
        }
        return false;
    }

    public static void Toggle(BombMode mode)
    {
        Set(mode, nextMode != mode);
    }

    public static bool TimeModeOn { get { return InMode(BombMode.Time); } set { Set(BombMode.Time, value); } }
    //public static bool VSModeOn { get { return InMode(BombMode.VS); } set { Set(BombMode.VS, value); } }
    public static bool ZenModeOn { get { return InMode(BombMode.Zen); } set { Set(BombMode.Zen, value); } }

    public static float timedMultiplier = 9;

    //Possibly look into adding VS mode in the future
    /*public static int teamHealth = 0;
    public static int bossHealth = 0;

    public static int GetTeamHealth()
    {
        return teamHealth;
    }

    public static int SubtractBossHealth(int damage)
    {
        bossHealth = bossHealth - damage;
        return bossHealth;
    }*//*

    public static void RefreshModes()
    {
        if (!BombActive && currentMode != nextMode)
        {
            currentMode = nextMode;
            //TODO: Send this to new command mod probably
            Debug.LogFormat("[Modes] Mode is now set to: {0}", Enum.GetName(typeof(BombMode), currentMode));
        }
    }

    public static float GetMultiplier()
    {
        return timedMultiplier;
    }

    public static float GetAdjustedMultiplier()
    {
        //TODO: Make this adjustable in BombMode settings.
        return Math.Min(timedMultiplier, 10.0f);
    }

    public static bool DropMultiplier()
    {
        //TODO: Make this adjustable in BombMode settings.
        if (timedMultiplier > (1.0f + 1.5f))
        {
            timedMultiplier = timedMultiplier - 1.5f;
            return true;
        }
        else
        {
            timedMultiplier = 1.0f;
            return false;
        }
    }

    public static void SetMultiplier(float newMultiplier)
    {
        timedMultiplier = newMultiplier;
    }
}

class TwitchPlaysActive
{
    private static GameObject _gameObject;

    private static IDictionary<string, object> Properties
    {
        get
        {
            return _gameObject == null
                ? null
                : _gameObject.GetComponent<IDictionary<string, object>>();
        }
    }

    public static IEnumerator Refresh()
    {
        for (var i = 0; i < 4 && _gameObject == null; i++)
        {
            _gameObject = GameObject.Find("TwitchPlays_Info");
            yield return null;
        }
    }

    public static bool Installed()
    {
        return _gameObject != null;
    }
}

class Modes : MonoBehaviour
{
    private bool _bombStarted;
    public List<BombCommander> BombCommanders = new List<BombCommander>();
    private int _currentBomb = -1;
    public static Modes Instance = null;
    private void Start()
    {
        StartCoroutine(TwitchPlaysActive.Refresh());
        ModeSettings.LoadDataFromFile();
    }

    public void OnLightsChange(bool on)
    {
        if (_bombStarted || !on) return;
        _bombStarted = true;
    }

    private void OnEnable()
    {
        BombModes.BombActive = true;
        _bombStarted = false;
        StartCoroutine(CheckForBomb());
    }
    private void OnDisable()
    {
        BombModes.BombActive = false;
        BombModes.RefreshModes();
    }

    private IEnumerator CheckForBomb()
    {
        yield return new WaitUntil(() => (SceneManager.Instance.GameplayState.Bombs != null && SceneManager.Instance.GameplayState.Bombs.Count > 0));
        List<Bomb> bombs = SceneManager.Instance.GameplayState.Bombs;

        for (int i = 0; i < GameRoom.GameRoomTypes.Length; i++)
        {
            if (GameRoom.GameRoomTypes[i]() != null && GameRoom.CreateRooms[i](FindObjectsOfType(GameRoom.GameRoomTypes[i]()), out GameRoom.Instance))
            {
                GameRoom.Instance.InitializeBombs(bombs);
                break;
            }
        }
    }

    public void SetBomb(Bomb bomb, int id)
    {
        if (BombCommanders.Count == 0)
            _currentBomb = id == -1 ? -1 : 0;
        BombCommanders.Add(new BombCommander(bomb));
    }
}

public class ModeSettingsData
{
    public int SettingsVersion = 0;

    public int TimeModeStartingTime = 5;
    public float TimeModeStartingMultiplier = 9.0f;
    public float TimeModeMaxMultiplier = 10.0f;
    public float TimeModeMinMultiplier = 1.0f;
    public float TimeModeSolveBonus = 0.1f;
    public float TimeModemultiplierStrikePenalty = 1.5f;
    public float TimeModeTimerStrikePenalty = 0.25f;
    public int TimeModeMinimumTimeLost = 15;
    public int TimeModeMinimumTimeGained = 20;
    public float AwardDropMultiplierOnStrike = 0.80f;
    
    public bool EnableFactoryAutomaticNextBomb = true;

    public bool ZenEnabled = true;
    public bool TimeEnabled = false;
    //public bool VSOn = false;
}

public static class ModeSettings
{
    public static int SettingsVersion = 0;
    public static ModeSettingsData data;

    public static void WriteDataToFile()
    {
        string path = Path.Combine(Application.persistentDataPath, usersSavePath);
        DebugHelper.Log("ModesStrings: writing file {0}", path);
        try
        {
            data.SettingsVersion = SettingsVersion;
            File.WriteAllText(path, SettingsConverter.Serialize(data));
        }
        catch (FileNotFoundException)
        {
            DebugHelper.LogWarning("ModeStrings: File {0} was not found", path);
            return;
        }
        catch (Exception ex)
        {
            DebugHelper.LogException(ex);
            return;
        }
        DebugHelper.Log("ModeStrings: Writing of file {0} completed successfully", path);
    }
    public static bool LoadDataFromFile()
    {
        string path = Path.Combine(Application.persistentDataPath, usersSavePath);
        try
        {
            DebugHelper.Log("ModeStrings: Loading Custom strings data from file: {0}", path);
            data = SettingsConverter.Deserialize<ModeSettingsData>(File.ReadAllText(path));//JsonConvert.DeserializeObject<TwitchPlaySettingsData>(File.ReadAllText(path));
            WriteDataToFile();
        }
        catch (FileNotFoundException)
        {
            DebugHelper.LogWarning("ModeStrings: File {0} was not found.", path);
            data = new ModeSettingsData();
            WriteDataToFile();
            return false;
        }
        catch (Exception ex)
        {
            data = new ModeSettingsData();
            DebugHelper.LogException(ex);
            return false;
        }
        return true;
    }
    public static string usersSavePath = "ModeSettings.json";
}

class SettingsConverter
{
    public static string Serialize(object obj)
    {
        return JsonConvert.SerializeObject(obj, Formatting.Indented, new ColorConverter());
    }

    public static T Deserialize<T>(string json)
    {
        return JsonConvert.DeserializeObject<T>(json, new ColorConverter());
    }
}

class ColorConverter : JsonConverter
{
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        Color color = (Color)value;
        string format = string.Format("{0}, {1}, {2}", (int)(color.r * 255), (int)(color.g * 255), (int)(color.b * 255));
        if (color.a != 1) format += ", " + (int)(color.a * 255);

        writer.WriteValue(format);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        IEnumerable<int?> parts = ((string)reader.Value).Split(',').Select(str => str.Trim().TryParseInt());
        if (parts.Any(x => x == null)) return existingValue;

        float[] values = parts.Select(i => (int)i / 255f).ToArray();
        switch (values.Count())
        {
            case 3:
                return new Color(values[0], values[1], values[2]);
            case 4:
                return new Color(values[0], values[1], values[2], values[3]);
            default:
                return existingValue;
        }
    }

    public override bool CanConvert(Type objectType)
    {
        return typeof(Color) == objectType;
    }
}

public class BombCommander
{
    public static BombCommander Instance = null;

    public BombCommander(Bomb bomb)
    {
        ReuseBombCommander(bomb);
    }

    public void ReuseBombCommander(Bomb bomb)
    {
        Bomb = bomb;
        timerComponent = Bomb.GetTimer();
        widgetManager = Bomb.WidgetManager;
        Selectable = Bomb.GetComponent<Selectable>();
        BombTimeStamp = DateTime.Now;
        bombStartingTimer = CurrentTimer;
        bombSolvableModules = 0;
        bombSolvedModules = 0;
        //SolvedModules = new Dictionary<string, List<TwitchComponentHandle>>();
    }

    public Bomb Bomb = null;
    public Selectable Selectable = null;
    public DateTime BombTimeStamp;
    public TimerComponent timerComponent = null;
    public WidgetManager widgetManager = null;
    public int bombSolvableModules;
    public int bombSolvedModules;
    public float bombStartingTimer;
    public float CurrentTimer
    {
        get => timerComponent.TimeRemaining;
        set => timerComponent.TimeRemaining = (value < 0) ? 0 : value;
    }
    public List<GameRoom> BombHandles = new List<GameRoom>();
}

public abstract class GameRoom : MonoBehaviour
{
    public static GameRoom Instance;

    public delegate Type GameRoomType();
    public delegate bool CreateRoom(UnityEngine.Object[] roomObjects, out GameRoom room);

    protected bool ReuseBombCommander = false;
    protected int BombCount;

    public static GameRoomType[] GameRoomTypes =
    {
        Factory.FactoryType,
        PortalRoom.PortalRoomType,
        Facility.RoomType,
        //ElevatorGameRoom.RoomType,
        DefaultGameRoom.RoomType
    };

    public static CreateRoom[] CreateRooms =
    {
        Factory.TrySetupFactory,
        PortalRoom.TryCreatePortalRoom,
        Facility.TryCreateFacility,
        //ElevatorGameRoom.TryCreateElevatorRoom,
        DefaultGameRoom.TryCreateRoom
    };

    public int BombID { get; protected set; }

    public virtual void InitializeBombs(List<Bomb> bombs)
    {
        int _currentBomb = bombs.Count == 1 ? -1 : 0;
        for (int i = 0; i < bombs.Count; i++)
        {
            Modes.Instance.SetBomb(bombs[i], _currentBomb == -1 ? -1 : i);
        }
        BombCount = (_currentBomb == -1) ? -1 : bombs.Count;
    }

    public virtual IEnumerator ReportBombStatus()
    {
        yield break;
    }
}

public class Factory : GameRoom
{
    private bool _finiteMode = false;
    private bool _infiniteMode = false;
    private bool _zenMode = false;  //For future use.

    public static Type FactoryType()
    {
        if (_factoryType != null) return _factoryType;

        _factoryType = ReflectionHelper.FindType("FactoryAssembly.FactoryRoom");
        if (_factoryType == null)
            return null;

        _factoryBombType = ReflectionHelper.FindType("FactoryAssembly.FactoryBomb");
        _internalBombProperty = _factoryBombType.GetProperty("InternalBomb", BindingFlags.NonPublic | BindingFlags.Instance);
        _bombEndedProperty = _factoryBombType.GetProperty("Ended", BindingFlags.NonPublic | BindingFlags.Instance);

        _factoryModeType = ReflectionHelper.FindType("FactoryAssembly.FactoryGameMode");
        _destroyBombMethod = _factoryModeType.GetMethod("DestroyBomb", BindingFlags.NonPublic | BindingFlags.Instance);

        _factoryStaticModeType = ReflectionHelper.FindType("FactoryAssembly.StaticMode");
        _factoryFiniteModeType = ReflectionHelper.FindType("FactoryAssembly.FiniteSequenceMode");
        _factoryInfiniteModeType = ReflectionHelper.FindType("FactoryAssembly.InfiniteSequenceMode");
        _currentBombField = _factoryFiniteModeType.GetField("_currentBomb", BindingFlags.NonPublic | BindingFlags.Instance);

        _gameModeProperty = _factoryType.GetProperty("GameMode", BindingFlags.NonPublic | BindingFlags.Instance);

        return _factoryType;
    }

    public static bool TrySetupFactory(UnityEngine.Object[] factoryObject, out GameRoom room)
    {
        if (factoryObject == null || factoryObject.Length == 0)
        {
            room = null;
            return false;
        }


        room = new Factory(factoryObject[0]);
        return true;
    }

    private Factory(UnityEngine.Object roomObject)
    {
        DebugHelper.Log("Found gameplay room of type Factory Room");
        _factory = roomObject;
        _gameroom = _gameModeProperty.GetValue(_factory, new object[] { });
        if (_gameroom.GetType() == _factoryStaticModeType) return;

        _infiniteMode = _gameroom.GetType() == _factoryInfiniteModeType;
        _finiteMode = _gameroom.GetType() == _factoryFiniteModeType;
        BombID = -1;
    }

    private static Type _factoryBombType = null;
    private static PropertyInfo _internalBombProperty = null;
    private static PropertyInfo _bombEndedProperty = null;

    private static Type _factoryType = null;
    private static Type _factoryModeType = null;
    private static MethodInfo _destroyBombMethod = null;

    private static Type _factoryStaticModeType = null;
    private static Type _factoryFiniteModeType = null;
    private static Type _factoryInfiniteModeType = null;

    private static PropertyInfo _gameModeProperty = null;
    private static FieldInfo _currentBombField = null;

    private object _factory = null;
    private object _gameroom = null;
}

public class DefaultGameRoom : GameRoom
{
    //The one catch-all room that as of now, should never be reached unless the game developers add in a new room type in the future.
    public static Type RoomType()
    {
        return typeof(GameplayRoom);
    }

    public static bool TryCreateRoom(UnityEngine.Object[] roomObjects, out GameRoom room)
    {
        if (roomObjects == null || roomObjects.Length == 0)
        {
            room = null;
            return false;
        }
        else
        {
            room = new DefaultGameRoom(roomObjects[0]);
            return true;
        }

    }

    private DefaultGameRoom(UnityEngine.Object roomObjects)
    {
        DebugHelper.Log("Found gameplay room of type Gameplay Room");
    }

}

public class Facility : GameRoom
{
    public BombCommander bombCommander = null;

    public static Type RoomType()
    {
        return typeof(FacilityRoom);
    }

    public static bool TryCreateFacility(UnityEngine.Object[] roomObjects, out GameRoom room)
    {
        if (roomObjects == null || roomObjects.Length == 0)
        {
            room = null;
            return false;
        }
        room = new Facility((FacilityRoom)roomObjects[0]);
        return true;
    }

    private Facility(FacilityRoom facilityRoom)
    {
        DebugHelper.Log("Found gameplay room of type Facility Room");
        _facilityRoom = facilityRoom;
    }

    public override IEnumerator ReportBombStatus()
    {
        IEnumerator baseIEnumerator = base.ReportBombStatus();
        while (baseIEnumerator.MoveNext()) yield return baseIEnumerator.Current;

        List<GameRoom> bombHandles = BombCommander.Instance.BombHandles;

        if (!SceneManager.Instance.GameplayState.Mission.PacingEventsEnabled)
            yield break;

        _facilityRoom.PacingActions.RemoveAll(action => action.EventType == PaceEvent.OneMinuteLeft);
        while (bombHandles.TrueForAll(handle => bombCommander.Bomb.HasDetonated))
        {
            if (bombHandles.TrueForAll(handle => bombCommander.Bomb.IsSolved()))
                yield break;
            ToggleEmergencyLights(bombHandles.Any(handle => bombCommander.CurrentTimer < 60f && bombCommander.Bomb.IsSolved()));
            yield return null;
        }
    }

    public bool EmergencyLightsState = false;
    public void ToggleEmergencyLights(bool on)
    {
        if (EmergencyLightsState == on) return;
        EmergencyLightsState = on;
        MethodInfo method = on ? _turnOnEmergencyLightsMethod : _turnOffEmergencyLightsMethod;
        method.Invoke(_facilityRoom, null);
    }
    
    static Facility()
    {
        _turnOffEmergencyLightsMethod = typeof(FacilityRoom).GetMethod("TurnOffEmergencyLights", BindingFlags.NonPublic | BindingFlags.Instance);
        _turnOnEmergencyLightsMethod = typeof(FacilityRoom).GetMethod("TurnOnEmergencyLights", BindingFlags.NonPublic | BindingFlags.Instance);
    }

    private static readonly MethodInfo _turnOffEmergencyLightsMethod = null;
    private static readonly MethodInfo _turnOnEmergencyLightsMethod = null;

    private readonly FacilityRoom _facilityRoom = null;
}

public class PortalRoom : GameRoom
{
    public BombCommander bombCommander = null;
    public static Type PortalRoomType()
    {
        if (_portalRoomType != null) return _portalRoomType;
        _portalRoomType = ReflectionHelper.FindType("PortalRoom", "HexiBombRoom");

        if (_portalRoomType == null)
            return null;

        _redLightsMethod = _portalRoomType.GetMethod("RedLight", BindingFlags.Public | BindingFlags.Instance);
        _roomLightField = _portalRoomType.GetField("RoomLight", BindingFlags.Public | BindingFlags.Instance);

        return _portalRoomType;
    }

    public static bool TryCreatePortalRoom(UnityEngine.Object[] roomObjects, out GameRoom room)
    {
        if (roomObjects == null || roomObjects.Length == 0 || PortalRoomType() == null)
        {
            room = null;
            return false;
        }

        room = new PortalRoom((MonoBehaviour)roomObjects[0]);
        return true;
    }

    private PortalRoom(UnityEngine.Object room)
    {
        DebugHelper.Log("Portal Room created");
        _room = room;
    }

    public override IEnumerator ReportBombStatus()
    {
        IEnumerator baseIEnumerator = base.ReportBombStatus();
        while (baseIEnumerator.MoveNext()) yield return baseIEnumerator.Current;

        List<GameRoom> bombHandles = BombCommander.Instance.BombHandles;
        yield return new WaitUntil(() => SceneManager.Instance.GameplayState.RoundStarted);
        yield return new WaitForSeconds(0.1f);
        _roomLight = (GameObject)_roomLightField.GetValue(_room);

        PaceMaker paceMaker = SceneManager.Instance.GameplayState.GetPaceMaker();
        List<PacingAction> actions = (List<PacingAction>)typeof(PaceMaker).GetField("actions", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(paceMaker);
        actions?.RemoveAll(action => action.EventType == PaceEvent.OneMinuteLeft);

        while (bombHandles.TrueForAll(handle => !bombCommander.Bomb.HasDetonated))
        {
            if (bombHandles.TrueForAll(handle => bombCommander.Bomb.IsSolved()))
                yield break;
            ToggleEmergencyLights(bombHandles.Any(handle => bombCommander.CurrentTimer < 60f && !bombCommander.Bomb.IsSolved()), bombHandles[0]);
            yield return null;
        }
    }

    private bool _emergencyLightsState = false;
    private IEnumerator _emergencyLightsRoutine = null;
    private void ToggleEmergencyLights(bool on, GameRoom handle)
    {
        if (_emergencyLightsState == on) return;
        _emergencyLightsState = on;
        if (!on)
        {
            handle.StopCoroutine(_emergencyLightsRoutine);
            _emergencyLightsRoutine = null;
            _roomLight.GetComponent<Light>().color = new Color(0.5f, 0.5f, 0.5f);
        }
        else
        {
            _emergencyLightsRoutine = (IEnumerator)_redLightsMethod.Invoke(_room, null);
            handle.StartCoroutine(_emergencyLightsRoutine);
        }
    }

    private static Type _portalRoomType = null;
    private static MethodInfo _redLightsMethod = null;
    private static FieldInfo _roomLightField = null;
    private readonly UnityEngine.Object _room = null;
    private GameObject _roomLight = null;
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

public static class DebugHelper
{
    public static void Log(params object[] args)
    {
        Log(string.Join(" ", args.Select(x => x.ToString()).ToArray()));
    }

    [StringFormatMethod("format")]
    public static void Log(string format, params object[] args)
    {
        Debug.LogFormat("[Modes] " + format, args);
    }

    public static void LogWarning(params object[] args)
    {
        LogWarning(string.Join(" ", args.Select(x => x.ToString()).ToArray()));
    }

    [StringFormatMethod("format")]
    public static void LogWarning(string format, params object[] args)
    {
        Debug.LogWarningFormat("[Modes] " + format, args);
    }

    public static void LogError(params object[] args)
    {
        LogError(string.Join(" ", args.Select(x => x.ToString()).ToArray()));
    }

    [StringFormatMethod("format")]
    public static void LogError(string format, params object[] args)
    {
        Debug.LogErrorFormat("[Modes] " + format, args);
    }

    public static void LogException(Exception ex, string message = "An exception has occurred:")
    {
        Log(message);
        Debug.LogException(ex);
    }



    private static StringBuilder _treeBuilder;

    public static void PrintTree(Transform t, Type[] forbiddenTypes = null, bool printComponents = false, bool fromTop = false) => PrintTree(t, forbiddenTypes, printComponents, fromTop, 0);
    private static void PrintTree(Transform t, Type[] forbiddenTypes, bool printComponents, bool fromTop, int level)
    {
        if (level == 0)
        {
            _treeBuilder = new StringBuilder();
            if (fromTop)
            {
                while (t.parent != null)
                    t = t.parent;
            }
        }

        string prefix = "";
        for (int i = 0; i < level; i++)
            prefix += "    ";

        _treeBuilder.Append($"{prefix}name = \"{t.name}\" - Active = {t.gameObject.activeInHierarchy}:{t.gameObject.activeSelf}\n");
        bool moveDown = forbiddenTypes == null || forbiddenTypes.ToList().TrueForAll(x => t.GetComponent(x) == null);
        if (moveDown)
        {
            _treeBuilder.Append($"{prefix} position = {Math.Round(t.localPosition.x, 7)},{Math.Round(t.localPosition.y, 7)},{Math.Round(t.localPosition.z, 7)}\n");
            _treeBuilder.Append($"{prefix} rotation = {Math.Round(t.localEulerAngles.x, 7)},{Math.Round(t.localEulerAngles.y, 7)},{Math.Round(t.localEulerAngles.z, 7)}\n");
            _treeBuilder.Append($"{prefix} scale = {Math.Round(t.localScale.x, 7)},{Math.Round(t.localScale.y, 7)},{Math.Round(t.localScale.z, 7)}\n");

            if (printComponents)
            {
                foreach (Component component in t.GetComponents<Component>())
                {
                    if (component is Transform) continue;
                    _treeBuilder.Append($"{prefix} Component: {component.GetType().FullName}\n");
                }
            }


            for (int i = 0; i < t.childCount; i++)
            {
                PrintTree(t.GetChild(i), forbiddenTypes, printComponents, false, level + 1);
            }
        }

        if (level == 0)
            Log(_treeBuilder.ToString());
    }

    public static void PrintParents(Transform t, bool printComponents = false) => PrintParents(t, 0, printComponents);
    private static void PrintParents(Transform t, int level, bool printComponents)
    {
        if (level == 0)
            _treeBuilder = new StringBuilder();

        string prefix = "";
        for (int i = 0; i < level; i++)
            prefix += "    ";

        _treeBuilder.Append($"{prefix}name = {t.name}\n");
        _treeBuilder.Append($"{prefix} position = {Math.Round(t.localPosition.x, 7)},{Math.Round(t.localPosition.y, 7)},{Math.Round(t.localPosition.z, 7)}\n");
        _treeBuilder.Append($"{prefix} rotation = {Math.Round(t.localEulerAngles.x, 7)},{Math.Round(t.localEulerAngles.y, 7)},{Math.Round(t.localEulerAngles.z, 7)}\n");
        _treeBuilder.Append($"{prefix} scale = {Math.Round(t.localScale.x, 7)},{Math.Round(t.localScale.y, 7)},{Math.Round(t.localScale.z, 7)}\n");

        if (printComponents)
        {
            foreach (Component component in t.GetComponents<Component>())
            {
                if (component is Transform) continue;
                _treeBuilder.Append($"{prefix} Component: {component.GetType().FullName}\n");
            }
        }

        if (t.parent != null)
        {
            PrintParents(t.parent, level + 1, printComponents);
        }

        if (level == 0)
            Log(_treeBuilder.ToString());
    }
}

public static class GeneralExtensions
{
    public static bool EqualsAny(this object obj, params object[] targets)
    {
        return targets.Contains(obj);
    }

    public static bool InRange(this int num, int min, int max)
    {
        return min <= num && num <= max;
    }

    public static string FormatTime(this float seconds)
    {
        bool addMilliseconds = seconds < 60;
        int[] timeLengths = { 86400, 3600, 60, 1 };
        List<int> timeParts = new List<int>();

        if (seconds < 1)
        {
            timeParts.Add(0);
        }
        else
        {
            foreach (int timeLength in timeLengths)
            {
                int time = (int)(seconds / timeLength);
                if (time > 0 || timeParts.Count > 0)
                {
                    timeParts.Add(time);
                    seconds -= time * timeLength;
                }
            }
        }

        string formatedTime = string.Join(":", timeParts.Select((time, i) => timeParts.Count > 2 && i == 0 ? time.ToString() : time.ToString("00")).ToArray());
        if (addMilliseconds) formatedTime += ((int)(seconds * 100)).ToString(@"\.00");

        return formatedTime;
    }

    public static string Join<T>(this IEnumerable<T> values, string separator = " ")
    {
        StringBuilder stringBuilder = new StringBuilder();
        IEnumerator<T> enumerator = values.GetEnumerator();
        if (enumerator.MoveNext()) stringBuilder.Append(enumerator.Current); else return "";

        while (enumerator.MoveNext()) stringBuilder.Append(separator).Append(enumerator.Current);

        return stringBuilder.ToString();
    }

    public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source)
    {
        return source.OrderBy(x => UnityEngine.Random.value);
    }

    //String wrapping code from http://www.java2s.com/Code/CSharp/Data-Types/ForcesthestringtowordwrapsothateachlinedoesntexceedthemaxLineLength.htm
    public static string Wrap(this string str, int maxLength)
    {
        return Wrap(str, maxLength, "");
    }

    public static string Wrap(this string str, int maxLength, string prefix)
    {
        if (string.IsNullOrEmpty(str)) return "";
        if (maxLength <= 0) return prefix + str;

        var lines = new List<string>();

        // breaking the string into lines makes it easier to process.
        foreach (string line in str.Split("\n".ToCharArray()))
        {
            var remainingLine = line.Trim();
            do
            {
                var newLine = GetLine(remainingLine, maxLength - prefix.Length);
                lines.Add(newLine);
                remainingLine = remainingLine.Substring(newLine.Length).Trim();
                // Keep iterating as int as we've got words remaining 
                // in the line.
            } while (remainingLine.Length > 0);
        }

        return string.Join("\n" + prefix, lines.ToArray());
    }

    private static string GetLine(string str, int maxLength)
    {
        // The string is less than the max length so just return it.
        if (str.Length <= maxLength) return str;

        // Search backwords in the string for a whitespace char
        // starting with the char one after the maximum length
        // (if the next char is a whitespace, the last word fits).
        for (int i = maxLength; i >= 0; i--)
        {
            if (char.IsWhiteSpace(str[i]))
                return str.Substring(0, i).TrimEnd();
        }

        // No whitespace chars, just break the word at the maxlength.
        return str.Substring(0, maxLength);
    }

    public static int? TryParseInt(this string number)
    {
        return int.TryParse(number, out int i) ? (int?)i : null;
    }

    public static bool ContainsIgnoreCase(this string str, string value)
    {
        return str.ToLowerInvariant().Contains(value.ToLowerInvariant());
    }

    public static bool EqualsIgnoreCase(this string str, string value)
    {
        return str.Equals(value, StringComparison.InvariantCultureIgnoreCase);
    }

    public static IEnumerable<T> TakeLast<T>(this IEnumerable<T> source, int N)
    {
        return source.Skip(Math.Max(0, source.Count() - N));
    }

    public static bool RegexMatch(this string str, params string[] patterns)
    {
        return str.RegexMatch(out _, patterns);
    }

    public static bool RegexMatch(this string str, out Match match, params string[] patterns)
    {
        if (patterns == null) throw new ArgumentNullException(nameof(patterns));
        match = null;
        foreach (string pattern in patterns)
        {
            try
            {
                Regex r = new Regex(pattern, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
                match = r.Match(str);
                if (match.Success)
                    return true;
            }
            catch (Exception ex)
            {
                DebugHelper.LogException(ex);
            }
        }
        return false;
    }

    public static double TotalSeconds(this DateTime datetime)
    {
        return TimeSpan.FromTicks(datetime.Ticks).TotalSeconds;
    }

    public static bool TryEquals(this string str, string value)
    {
        if (!string.IsNullOrEmpty(str) && !string.IsNullOrEmpty(value)) return str.Equals(value);
        if (str == null && value == null) return true;
        if (str == string.Empty && value == string.Empty) return true;
        return false;
    }

    public static bool TryEquals(this string str, string value, StringComparison comparisonType)
    {
        if (!string.IsNullOrEmpty(str) && !string.IsNullOrEmpty(value)) return str.Equals(value, comparisonType);
        if (str == null && value == null) return true;
        if (str == string.Empty && value == string.Empty) return true;
        return false;
    }

    /// <summary>
    ///     Adds an element to a List&lt;V&gt; stored in the current IDictionary&lt;K, List&lt;V&gt;&gt;. If the specified
    ///     key does not exist in the current IDictionary, a new List is created.</summary>
    /// <typeparam name="K">
    ///     Type of the key of the IDictionary.</typeparam>
    /// <typeparam name="V">
    ///     Type of the values in the Lists.</typeparam>
    /// <param name="dic">
    ///     IDictionary to operate on.</param>
    /// <param name="key">
    ///     Key at which the list is located in the IDictionary.</param>
    /// <param name="value">
    ///     Value to add to the List located at the specified Key.</param>
    public static void AddSafe<K, V>(this IDictionary<K, List<V>> dic, K key, V value)
    {
        if (dic == null)
            throw new ArgumentNullException("dic");
        if (key == null)
            throw new ArgumentNullException("key", "Null values cannot be used for keys in dictionaries.");
        if (!dic.ContainsKey(key))
            dic[key] = new List<V>();
        dic[key].Add(value);
    }
}*/