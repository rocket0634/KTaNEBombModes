using UnityEngine;

public class ManualManager : MonoBehaviour
{
    static GUIStyle boxStyle;
    const int margin = 20;
    readonly Rect titleBarRect = new Rect(0, 0, 10000, 20);
    Rect windowRect = new Rect(margin, margin, Screen.width - (margin * 2), Screen.height - (margin * 2));
    const string windowTitle = "Manual manager";
    public bool isActive = false;
    void OnGUI()
    {
        if (boxStyle == null)
        {
            boxStyle = new GUIStyle(GUI.skin.box)
            {
                font = ManualCheckerLoader.Instance.specialElite,
                fontSize = 14,
                alignment = TextAnchor.MiddleLeft
            };
        }

        if (!isActive) return;
        GUILayout.Window(654321, windowRect, DrawWindow, windowTitle);
    }

    void DrawWindow(int windowID)
    {
        DrawToolbar();
        GUI.DragWindow(titleBarRect);
    }
    void DrawToolbar()
    {
        GUILayout.BeginHorizontal();
        GUILayout.EndHorizontal();
    }
}
