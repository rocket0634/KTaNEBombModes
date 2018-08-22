using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using Newtonsoft.Json;
using System.IO;
using System.Reflection;
using System.Linq;
using TMPro;

[RequireComponent(typeof(KMService))]
[RequireComponent(typeof(KMGameInfo))]
public class Manual : MonoBehaviour
{
    //Use reflection to get currentState, as it is a protected variable
    public static FieldInfo _state = typeof(SceneManager).GetField("currentState", BindingFlags.NonPublic | BindingFlags.Instance);
    public static Material fontmaterial;
    public static TMP_FontAsset font;
    //Due to button placement, I've made a new texture to fit the buttons in
    public Texture brochureReplacement;

    private void Start()
    {
        GetComponent<KMGameInfo>().OnStateChange += OnStateChange;
    }

    private void OnStateChange(KMGameInfo.State state)
    {
        if (!transform.gameObject.activeInHierarchy) return;
        if (state == KMGameInfo.State.Setup)
        {
            StartCoroutine(CheckForBrochure());
        }
    }

    private IEnumerator CheckForBrochure()
    {
        yield return null;
        
        //Get the SetupRoom to call the ModManagerHoldable
        SetupRoom setupRoom = (SetupRoom)SceneManager.Instance.CurrentRoom;
        ModManagerHoldable _brochure = setupRoom.ModManagerHoldable;
        //Grab ModButton and move it to give room for the new button
        Selectable ModButton = _brochure.OpenModManagerButton;
        ModButton.transform.Translate(0, 0, 0.01f);
        //Clone the ModButton
        Selectable ManualButton = Instantiate(ModButton, ModButton.transform);
        //Grab the Brochure's selectable to add the new button to the list of its children selectables.
        Selectable Brochure = _brochure.GetComponent<Selectable>();
        //Actually move the new button
        ManualButton.transform.Translate(-0.005f, -0.002f, 0.005f);
        //Quickly grab the texture for the middle panel so I can replace it.
        MeshRenderer replace = _brochure.GetComponent<Transform>().Find("PanelMiddleBack").GetComponent<MeshRenderer>();
        replace.material.mainTexture = brochureReplacement;
        Brochure.Children = Brochure.Children.Concat(new[] { ManualButton, null }).ToArray();
        //The text autosizing makes the text smaller, which I do not want. So for now, setting this to false.
        ManualButton.GetComponentInChildren<TextMeshPro>().enableAutoSizing = false;
        ManualButton.GetComponentInChildren<TextMeshPro>().text = "Manage Manuals";
        //Using this to deal with see through text in the Manual manager, maybe.
        fontmaterial = ManualButton.GetComponentInChildren<TextMeshPro>().fontMaterial;
        font = ManualButton.GetComponentInChildren<TextMeshPro>().font;
        /*Getting ready to call EnterModManagerStateFromSetup, however, I will be replacing modManagerScene with my own scene
        As such, I'll basically be copying this method here, and replacing the last line.*/
        ManualButton.OnInteract += delegate () { OnInteract(); return false; };
    }

   private void OnInteract()
    {
        //Shorthand
        var sm = SceneManager.Instance;
        //CurrentState is protected, use reflection to change it
        _state.SetValue(sm, SceneManager.State.Transitioning);
        sm.SetupState.PrepareForExitState();
        LoadingOverlay.Instance.Enable();
        sm.FadeOut(sm.SetupState.FadeOutTime, delegate
        {
            sm.SaveData();
            sm.SetupState.ExitState();
            ManualManager.Instance = new ManualManager();
            ManualManager.Instance.Start();
        });
    }
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