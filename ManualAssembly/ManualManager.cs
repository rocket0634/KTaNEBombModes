using UnityEngine;
using SceneManagement = UnityEngine.SceneManagement.SceneManager;

public class ManualManager : MonoBehaviour
{
    public void Start()
    {
        Manual._state.SetValue(SceneManager.Instance, SceneManager.State.ModManager);
        LoadingOverlay.Instance.Disable();
        UnityEngine.SceneManagement.Scene ManualManager = SceneManagement.CreateScene("ManualManagerScene");
        Debug.LogFormat(ManualManager.isLoaded.ToString() + " " + ManualManager.path);
        SceneManagement.SetActiveScene(ManualManager);
        var MCL = ManualCheckerLoader.Instance.testObject;
        SceneManagement.MoveGameObjectToScene(MCL, ManualManager);
        MCL.SetActive(true);
        var cam = MCL.GetComponentInChildren<Camera>().gameObject;
        var light = MCL.GetComponentInChildren<Light>().gameObject;
        cam.SetActive(true);
        light.SetActive(true);
        /*test = new GameObject("test").AddComponent<TextMeshProUGUI>();
        test.text = "TEST";
        test.transform.Rotate(90, 0, 0);
        test.fontSize = 20;
        test.fontMaterial = Manual.fontmaterial;
        test.font = Manual.font;
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(test.gameObject, ManualManager);
        UnityEngine.SceneManagement.SceneManager.SetActiveScene(ManualManager);
        Debug.LogFormat("[Manual] " + GameObject.Find("test").scene.name + " " + String.Join(", ", ManualManager.GetRootGameObjects().Select(x => x.name).ToArray()) + " " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);*/
    }
}
