using UnityEngine;

/// <summary>
/// 간단한 FPS 카운터 - FPS 표시만 담당
/// </summary>
public class SimpleFrameCounter : MonoBehaviour
{
    [Header("FPS Settings")]
    public bool showFPS = true;
    public float updateInterval = 0.5f;
    public int fontSize = 20;
    public Color textColor = Color.yellow;
    
    [Header("Position")]
    public Vector2 screenPosition = new Vector2(10, 10);
    public TextAnchor alignment = TextAnchor.UpperLeft;
    
    private float fps = 0f;
    private float frameCount = 0;
    private float deltaTime = 0f;
    private float lastUpdate = 0f;
    
    private Rect displayRect;
    private GUIStyle textStyle;
    
    void Start()
    {
        InitializeGUI();
    }
    
    void Update()
    {
        if (!showFPS) return;
        
        CalculateFPS();
    }
    
    private void InitializeGUI()
    {
        textStyle = new GUIStyle
        {
            alignment = alignment,
            fontSize = fontSize,
            normal = { textColor = textColor }
        };
        
        displayRect = new Rect(screenPosition.x, screenPosition.y, 200, 30);
    }
    
    private void CalculateFPS()
    {
        frameCount++;
        deltaTime += Time.unscaledDeltaTime;
        
        if (Time.unscaledTime - lastUpdate > updateInterval)
        {
            fps = frameCount / deltaTime;
            frameCount = 0;
            deltaTime = 0f;
            lastUpdate = Time.unscaledTime;
        }
    }
    
    void OnGUI()
    {
        if (!showFPS) return;
        
        GUI.Label(displayRect, $"FPS: {fps:F1}", textStyle);
    }
    
    public void SetFPSVisible(bool visible)
    {
        showFPS = visible;
    }
    
    public void SetUpdateInterval(float interval)
    {
        updateInterval = interval;
    }
    
    public void SetTextColor(Color color)
    {
        textColor = color;
        if (textStyle != null)
        {
            textStyle.normal.textColor = color;
        }
    }
    
    public void SetPosition(Vector2 position)
    {
        screenPosition = position;
        displayRect.x = position.x;
        displayRect.y = position.y;
    }
    
    public float GetCurrentFPS()
    {
        return fps;
    }
}