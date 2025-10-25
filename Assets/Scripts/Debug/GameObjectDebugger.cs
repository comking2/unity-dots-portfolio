using UnityEngine;

/// <summary>
/// GameObject 디버깅 정보 표시 (RenderObject 태그 등)
/// </summary>
public class GameObjectDebugger : MonoBehaviour
{
    [Header("Display Settings")]
    public bool showGameObjectInfo = true;
    public float sampleInterval = 1f;
    public int fontSize = 16;
    public Color textColor = Color.cyan;
    
    [Header("Position")]
    public Vector2 screenPosition = new Vector2(10, 200);
    public TextAnchor alignment = TextAnchor.UpperLeft;
    
    [Header("GameObject Queries")]
    public bool showRenderObjects = true;
    public bool showDetailedInfo = false;
    
    [Header("Tags to Monitor")]
    public string[] tagsToMonitor = { "RenderObject", "Player", "Enemy" };
    
    // GameObject Counts
    private int renderObjectCount = 0;
    private System.Collections.Generic.Dictionary<string, int> tagCounts = new System.Collections.Generic.Dictionary<string, int>();
    
    // Timing
    private float sampleTimer = 0f;
    
    // GUI
    private Rect displayRect;
    private GUIStyle textStyle;
    
    void Start()
    {
        InitializeGUI();
        InitializeTagDictionary();
    }
    
    void Update()
    {
        if (!showGameObjectInfo) return;
        
        UpdateGameObjectCounts();
    }
    
    private void InitializeGUI()
    {
        textStyle = new GUIStyle
        {
            alignment = alignment,
            fontSize = fontSize,
            normal = { textColor = textColor }
        };
        
        displayRect = new Rect(screenPosition.x, screenPosition.y, 400, 200);
    }
    
    private void InitializeTagDictionary()
    {
        tagCounts.Clear();
        foreach (string tag in tagsToMonitor)
        {
            if (!string.IsNullOrEmpty(tag))
            {
                tagCounts[tag] = 0;
            }
        }
    }
    
    private void UpdateGameObjectCounts()
    {
        sampleTimer += Time.unscaledDeltaTime;
        if (sampleTimer < sampleInterval) return;
        
        sampleTimer = 0f;
        
        try
        {
            // RenderObject 태그 수
            if (showRenderObjects)
            {
                renderObjectCount = ObjectSpawner.spawnParent != null ?
                    ObjectSpawner.spawnParent.childCount : 0;
            }
            
            // 기타 태그들 카운트
            UpdateTagCounts();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"GameObjectDebugger: Error updating counts - {e.Message}");
        }
    }
    
    private void UpdateTagCounts()
    {
        var keys = new string[tagCounts.Keys.Count];
        tagCounts.Keys.CopyTo(keys, 0);
        
        foreach (string tag in keys)
        {
            try
            {
                GameObject[] objectsWithTag = GameObject.FindGameObjectsWithTag(tag);
                tagCounts[tag] = objectsWithTag.Length;
            }
            catch (UnityException)
            {
                // 태그가 존재하지 않는 경우
                tagCounts[tag] = 0;
            }
        }
    }
    
    void OnGUI()
    {
        if (!showGameObjectInfo) return;
        
        string debugInfo = BuildDebugString();
        GUI.Label(displayRect, debugInfo, textStyle);
    }
    
    private string BuildDebugString()
    {
        var info = new System.Text.StringBuilder();
        
        info.AppendLine("=== GameObject Debug Info ===");
        
        if (showRenderObjects)
        {
            info.AppendLine($"RenderObjects: {renderObjectCount}");
        }
        
        // 기타 태그 정보
        foreach (var tagCount in tagCounts)
        {
            if (tagCount.Key != "RenderObject" || !showRenderObjects)
            {
                info.AppendLine($"{tagCount.Key}: {tagCount.Value}");
            }
        }
        
        if (showDetailedInfo)
        {
            info.AppendLine($"Update Interval: {sampleInterval:F1}s");
        }
        
        return info.ToString();
    }
    
    public void SetGameObjectInfoVisible(bool visible)
    {
        showGameObjectInfo = visible;
    }
    
    public void SetSampleInterval(float interval)
    {
        sampleInterval = Mathf.Max(0.1f, interval);
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
    
    public void AddTagToMonitor(string tag)
    {
        if (!string.IsNullOrEmpty(tag) && !tagCounts.ContainsKey(tag))
        {
            tagCounts[tag] = 0;
        }
    }
    
    public void RemoveTagFromMonitor(string tag)
    {
        if (tagCounts.ContainsKey(tag))
        {
            tagCounts.Remove(tag);
        }
    }
    
    public void ToggleRenderObjects()
    {
        showRenderObjects = !showRenderObjects;
    }
    
    public void ToggleDetailedInfo()
    {
        showDetailedInfo = !showDetailedInfo;
    }
    
    // Getter methods
    public int GetRenderObjectCount() => renderObjectCount;
    public int GetTagCount(string tag) => tagCounts.ContainsKey(tag) ? tagCounts[tag] : 0;
    
    public void SetTagsToMonitor(string[] tags)
    {
        tagsToMonitor = tags;
        InitializeTagDictionary();
    }
}