using UnityEngine;
using Unity.Entities;
using Unity.Collections;

/// <summary>
/// ECS 엔티티 디버깅 정보 표시
/// </summary>
public class EntityDebugger : MonoBehaviour
{
    [Header("Display Settings")]
    public bool showEntityInfo = true;
    public float sampleInterval = 1f;
    public int fontSize = 16;
    public Color textColor = Color.green;
    
    [Header("Position")]
    public Vector2 screenPosition = new Vector2(10, 50);
    public TextAnchor alignment = TextAnchor.UpperLeft;
    
    [Header("Entity Queries")]
    public bool showRuntimeEntities = true;
    public bool showTotalEntities = true;
    public bool showSystemInfo = false;
    
    // ECS
    private EntityManager entityManager;
    private EntityQuery runtimeQuery;   // 활성 엔티티만
    private EntityQuery totalQuery;     // 전체 엔티티 (프리팹/비활성 포함)
    private bool isInitialized = false;
    
    // Entity Counts
    private int runtimeCount = 0;
    private int totalCount = 0;
    private int systemCount = 0;
    
    // Timing
    private float sampleTimer = 0f;
    
    // GUI
    private Rect displayRect;
    private GUIStyle textStyle;
    
    void OnEnable()
    {
        InitializeECS();
        InitializeGUI();
    }
    
    void OnDisable()
    {
        CleanupECS();
    }
    
    void Update()
    {
        if (!showEntityInfo || !isInitialized) return;
        
        UpdateEntityCounts();
    }
    
    private void InitializeECS()
    {
        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null)
        {
            Debug.LogWarning("EntityDebugger: No default ECS world found");
            return;
        }
        
        entityManager = world.EntityManager;
        
        // 활성 엔티티만 (Prefab, Disabled 제외)
        runtimeQuery = entityManager.CreateEntityQuery(
            ComponentType.Exclude<Prefab>(),
            ComponentType.Exclude<Disabled>()
        );
        
        // 전체 엔티티 (프리팹, 비활성 포함)
        totalQuery = entityManager.CreateEntityQuery(new EntityQueryDesc
        {
            Options = EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IncludePrefab
        });
        
        // 시스템 수 계산
        if (showSystemInfo)
        {
            systemCount = world.Systems.Count;
        }
        
        isInitialized = true;
        Debug.Log("EntityDebugger initialized");
    }
    
    private void InitializeGUI()
    {
        textStyle = new GUIStyle
        {
            alignment = alignment,
            fontSize = fontSize,
            normal = { textColor = textColor }
        };
        
        displayRect = new Rect(screenPosition.x, screenPosition.y, 400, 120);
    }
    
    private void UpdateEntityCounts()
    {
        sampleTimer += Time.unscaledDeltaTime;
        if (sampleTimer < sampleInterval) return;
        
        sampleTimer = 0f;
        
        try
        {
            if (showRuntimeEntities)
            {
                runtimeCount = runtimeQuery.CalculateEntityCount();
            }
            
            if (showTotalEntities)
            {
                totalCount = totalQuery.CalculateEntityCount();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"EntityDebugger: Error updating entity counts - {e.Message}");
        }
    }
    
    private void CleanupECS()
    {
        isInitialized = false;
        // EntityQuery는 자동으로 정리되므로 수동 Dispose 불필요
    }
    
    void OnGUI()
    {
        if (!showEntityInfo || !isInitialized) return;
        
        string debugInfo = BuildDebugString();
        GUI.Label(displayRect, debugInfo, textStyle);
    }
    
    private string BuildDebugString()
    {
        var info = new System.Text.StringBuilder();
        
        info.AppendLine("=== ECS Debug Info ===");
        
        if (showRuntimeEntities)
        {
            info.AppendLine($"Runtime Entities: {runtimeCount}");
        }
        
        if (showTotalEntities)
        {
            info.AppendLine($"Total Entities: {totalCount}");
        }
        
        if (showSystemInfo)
        {
            info.AppendLine($"Systems: {systemCount}");
        }
        
        // 추가 정보
        info.AppendLine($"Update Interval: {sampleInterval:F1}s");
        
        return info.ToString();
    }
    
    public void SetEntityInfoVisible(bool visible)
    {
        showEntityInfo = visible;
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
    
    public void ToggleRuntimeEntities()
    {
        showRuntimeEntities = !showRuntimeEntities;
    }
    
    public void ToggleTotalEntities()
    {
        showTotalEntities = !showTotalEntities;
    }
    
    public void ToggleSystemInfo()
    {
        showSystemInfo = !showSystemInfo;
    }
    
    // Getter methods
    public int GetRuntimeEntityCount() => runtimeCount;
    public int GetTotalEntityCount() => totalCount;
    public int GetSystemCount() => systemCount;
    public bool IsInitialized() => isInitialized;
}