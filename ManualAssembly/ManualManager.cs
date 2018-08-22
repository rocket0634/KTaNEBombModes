using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using TMPro;

public class ManualManager : MonoBehaviour
{
    public static ManualManager Instance { get; set; }
    public GameObject ManualManagerObject;

    public void Start()
    {
        Manual._state.SetValue(SceneManager.Instance, SceneManager.State.ModManager);
        LoadingOverlay.Instance.Disable();
        UnityEngine.SceneManagement.Scene ManualManager = UnityEngine.SceneManagement.SceneManager.CreateScene("ManualManagerScene");
        UnityEngine.SceneManagement.SceneManager.SetActiveScene(ManualManager);

        /*test = new GameObject("test").AddComponent<TextMeshProUGUI>();
        test.text = "TEST";
        test.transform.Rotate(90, 0, 0);
        test.fontSize = 20;
        test.fontMaterial = Manual.fontmaterial;
        test.font = Manual.font;*/
    }
}
