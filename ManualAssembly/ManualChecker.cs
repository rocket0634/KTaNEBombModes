using System;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Reflection;
using System.Text;
using FileVersion = System.Diagnostics.FileVersionInfo;
[RequireComponent(typeof(KMGameInfo))]
[RequireComponent(typeof(KMService))]
public class ManualCheckerLoader : MonoBehaviour
{
    public Texture brochureReplacement;
    public Font specialElite;
    public static ManualCheckerLoader Instance { get; set; }
    internal static ManualManager Manager { get; set; }
    internal static string sharpPath = Path.Combine(Application.dataPath, @"Managed/PdfSharp.dll");

    private void Awake()
    {
        Instance = this;
        ManualChecker ManualChecker = new ManualChecker();
        Manual Manual = new Manual();
        GetComponent<KMGameInfo>().OnStateChange += Manual.OnStateChange;
        StartCoroutine(ManualChecker.OnStateChange());
        Manager = GetComponent<ManualManager>();
    }
}

public class ManualChecker
{
    public List<string> ModdedManuals = new List<string>();
    private MethodInfo BuildManual = typeof(ModManager).GetMethod("BuildManual", BindingFlags.NonPublic | BindingFlags.Instance);
    static FileVersion sharpInfo = FileVersion.GetVersionInfo(ManualCheckerLoader.sharpPath);
    readonly bool open = sharpInfo.FileVersion.Equals("1.32.2608.0");
    private Encoding ascii = new ASCIIEncoding();
    private string newPath;
    private string newDirectory;

    public IEnumerator OnStateChange()
    {
        SceneManager.State state = Manual.SM.CurrentState;
        while (ManualCheckerLoader.Instance.isActiveAndEnabled)
        {
            var currentState = Manual.SM.CurrentState;
            if (!state.Equals(currentState))
            {
                if (state.Equals(SceneManager.State.ModManager)) OnAllModsLoadComplete();
                else if (Manual.button)
                {
                    yield return new WaitForSeconds(0.5f);
                    Manual.button = false;
                    ManualCheckerLoader.Manager.isActive = true;
                }
            }
            state = Manual.SM.CurrentState;
            yield return null;
        }
    }

    public void OnAllModsLoadComplete()
    {
        if (!open) return;
        List<string> ModdedModuleManuals = (List<string>)Manual._GetAllModuleManuals.Invoke(ModManager.Instance, null);
        List<string> ModdedNeedyManuals = (List<string>)Manual._GetAllNeedyModuleManuals.Invoke(ModManager.Instance, null);
        List<string> ModdedAppendixManuals = (List<string>)Manual._GetAllAppendixManuals.Invoke(ModManager.Instance, null);
        ModdedManuals = new List<string>();
        ModdedManuals.AddRange(ModdedModuleManuals.Concat(ModdedNeedyManuals.Concat(ModdedAppendixManuals)));
        string text = Application.persistentDataPath + "/Manual";
        string path = text + "/Manual.pdf";
        string path2 = text + "/ModsOnlyManual.pdf";

        if (ModdedManuals.Any(x => Test(x) == true))
        {
            Debug.LogFormat("[Manual Manager] Outdated manual detected, attempting to rebuild...");
            foreach (string manual in ModdedManuals.Where(x => Test(x) == true))
            {
                Open(manual);
            }
        }

        try
        {
            BuildManual.Invoke(ModManager.Instance, null);
            Debug.LogFormat("[Manual Manager] Build succeeded.");
        }
        catch (TargetInvocationException ex)
        {
            Debug.LogFormat("[Manual Manager] Mod Manager reports message " + ex.InnerException.Message);
        }
    }
    bool Test(string path)
    {
        Stream stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite);
        int length = (int)stream.Length;
        string trail = ReadRawString(stream, length - 131, 130);
        int idx = trail.LastIndexOf("startxref");
        stream.Close();
        return idx == -1;
    }

    void Open(string manual)
    {
        Stream stream = new FileStream(manual, FileMode.Open, FileAccess.ReadWrite);
        var curDoc = Path.GetFileName(manual);
        newDirectory = Path.Combine(Path.GetDirectoryName(manual), @"old");
        if (!Directory.Exists(newDirectory)) Directory.CreateDirectory(newDirectory);
        newPath = Path.Combine(newDirectory, curDoc);
        File.Copy(manual, newPath, true);
        string trail = ReadRawString(stream, 0, (int)stream.Length);
        stream.Position = trail.LastIndexOf("%%EOF", StringComparison.Ordinal);
        stream.SetLength(stream.Position + 5);
        stream.Close();
        Debug.LogFormat("[Manual Manager] Fix applied to manual {0} {1}.", curDoc, Test(manual) ? "failed" : "succeeded");
    }

    private string ReadRawString(Stream stream, int position, int length)
    {
        stream.Position = position;
        byte[] bytes = new byte[length];
        stream.Read(bytes, 0, length);
        return ascii.GetString(bytes, 0, bytes.Length);
    }
}
