using UnityEngine;
using System.Reflection;
using System.Collections;

public class ModManualManager
{
    private MethodInfo TransitionToScreen = typeof(MenuManager).GetMethod("TransitionToScreen", BindingFlags.Instance | BindingFlags.NonPublic);
    public void Start()
    {
        //var obj = ManualCheckerLoader.Instance.prefab.AddComponent<ManualScreen>();
        //ManualScreen ManualScreenPrefab = obj;
        //MenuScreen screen = Object.Instantiate(ManualScreenPrefab);
        //screen.Load();
        //screen.gameObject.SetActive(false);
        //ManualCheckerLoader.Instance.StartCoroutine(Transit(screen));
    }

    private IEnumerator Transit(MenuScreen screen)
    {
        yield return new WaitUntil(() => SceneManager.Instance.CurrentState != SceneManager.State.Transitioning);
        TransitionToScreen.Invoke(MenuManager.Instance, new object[] { screen, MenuManager.TransitionType.PushAndReplace, null });
    }
}

public class ManualScreen : MenuScreen
{
    private void Start()
    {
        gameObject.SetActive(true);
        gameObject.GetComponentInChildren<Camera>().gameObject.SetActive(true);
        gameObject.GetComponentInChildren<Light>().gameObject.SetActive(true);
    }
}

