using UnityEngine;

public class FrameCounter : MonoBehaviour
{
    float deltaTime = 0.0f;
    Rect rect;
    int w, h;
    GUIStyle style;
    void Start()
    {
        w = Screen.width;
        h = Screen.height;
        style = new GUIStyle();
        rect = new Rect(10, 10, w, h * 2 / 100);
        style.alignment = TextAnchor.UpperLeft;
        style.fontSize = h * 2 / 50;
        style.normal.textColor = Color.white;
    }
    void Update()
    {
        // 보정된 델타타임 (smoothed)
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
    }

    void OnGUI()
    {
        float fps = 1.0f / deltaTime;
        string text = string.Format("{0:0.} FPS", fps);
        GUI.Label(rect, text, style);
    }
}