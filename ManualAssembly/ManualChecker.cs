using System;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using System.Reflection;
[RequireComponent(typeof(KMGameInfo))]
[RequireComponent(typeof(KMService))]
public class ManualCheckerLoader : MonoBehaviour
{
    public Texture brochureReplacement;
    public GameObject testObject;
    public static ManualCheckerLoader Instance { get; set; }
    private void Awake()
    {
        Instance = this;
        ManualChecker ManualChecker = new ManualChecker();
        Manual Manual = new Manual();
        GetComponent<KMGameInfo>().OnStateChange += Manual.OnStateChange;
        StartCoroutine(ManualChecker.OnStateChange());
    }
}

public class ManualChecker
{
    public List<string> ModdedManuals = new List<string>();
    private bool e;
    public PdfDocument pdfDocument = new PdfDocument();
    private FieldInfo _preloadedScreens = typeof(MenuManager).GetField("preloadedScreens", BindingFlags.NonPublic | BindingFlags.Instance);

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
                    //yield return new WaitUntil(() => ((Dictionary<MenuManager.ScreenType,MenuScreen>)_preloadedScreens.GetValue(MenuManager.Instance)).ContainsKey(MenuManager.ScreenType.ModManagerMainMenu));
                    //yield return new WaitUntil(() => ((Dictionary<MenuManager.ScreenType,MenuScreen>)_preloadedScreens.GetValue(MenuManager.Instance))[MenuManager.ScreenType.ModManagerMainMenu] == MenuManager.Instance.CurrentScreen);
                    Manual.button = false;
                    ModManualManager ManualManager = new ModManualManager();
                    ManualManager.Start();
                }
            }
            state = Manual.SM.CurrentState;
            yield return null;
        }
    }

    public void OnAllModsLoadComplete()
    {
        List<string> ModdedModuleManuals = (List<string>)Manual._GetAllModuleManuals.Invoke(ModManager.Instance, null);
        List<string> ModdedNeedyManuals = (List<string>)Manual._GetAllNeedyModuleManuals.Invoke(ModManager.Instance, null);
        List<string> ModdedAppendixManuals = (List<string>)Manual._GetAllAppendixManuals.Invoke(ModManager.Instance, null);
        ModdedManuals.AddRange(ModdedModuleManuals.Concat(ModdedNeedyManuals.Concat(ModdedAppendixManuals)));
        string text = Application.persistentDataPath + "/Manual";
        string path = text + "/Manual.pdf";
        string path2 = text + "/ModsOnlyManual.pdf";
        PdfDocument pdfDocument4 = ManualHelper(ModdedManuals, null, false);
        PdfDocument ModOnlyManual = PdfReader.Open(path2, PdfDocumentOpenMode.Import);
        if (e) return;

        if (pdfDocument4.PageCount != ModOnlyManual.PageCount)
        {
            Debug.LogFormat("[Manual Manager] Outdated manual detected, attempting to recreate...");
            int num = 16;
            int num2 = 20;
            Stream stream = new MemoryStream(Resources.Load<TextAsset>("PC/Manual/Bomb-Defusal-Manual_1").bytes);
            PdfDocument pdfDocument2 = PdfReader.Open(stream, PdfDocumentOpenMode.Import);
            pdfDocument = new PdfDocument();
            ManualHelper(null, pdfDocument2, true, 0, num);
            ManualHelper(ModdedModuleManuals, null, false);
            ManualHelper(null, pdfDocument2, true, num, num2);
            ManualHelper(ModdedNeedyManuals, null, false);
            ManualHelper(null, pdfDocument2, true, num2, pdfDocument2.PageCount);
            ManualHelper(ModdedAppendixManuals, null, false);
            if (e) return;
            try
            {
                pdfDocument.Save(path);
                pdfDocument4.Save(path2);
                Debug.LogFormat("[Manual Manager] Manual recreation successful.");
            }
            catch (Exception ex)
            {
                Debug.LogFormat("[Manual Manager] Unable to save new manual, aborting recreation. Resulting error:\n{0}", ex.Message);
            }
        }
    }

    private PdfDocument ManualHelper(List<string> path, PdfDocument vanManual, bool use, int start = 0, int count = 0)
    {
        string currentFile = "";
        try
        {
            if (!use)
            {
                foreach (string modManual in path)
                {
                    currentFile = modManual;
                    vanManual = PdfReader.Open(modManual, PdfDocumentOpenMode.Import);
                    for (int i = 0; i < vanManual.PageCount; i++) pdfDocument.AddPage(vanManual.Pages[i]);
                }
                return pdfDocument;
            }
            else
            {
                for (int i = start; i < count; i++)
                {
                    currentFile = "the Bomb Defusal Manual, page #" + i;
                    pdfDocument.AddPage(vanManual.Pages[i]);
                }
                return pdfDocument;
            }
        }
        catch (Exception ex)
        {
            Debug.LogFormat("[Manual Manager] There was an issue loading manual from {0}, manual will be unable to be recreated. Resulting Error:\n{1}", currentFile, ex.Message);
            e = true;
            return null;
        }
    }
}
