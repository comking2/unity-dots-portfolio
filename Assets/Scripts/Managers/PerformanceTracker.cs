using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;

public enum SceneType
{
    Traditional,
    ECS,
    Unknown
}

[System.Serializable]
public class PerformanceDataPoint
{
    public string sceneName;
    public SceneType sceneType;
    public float time;
    public int objectCount;
    public int entityCount;
    public float fps;
    public float deltaTime;
    
    // 디버거에서 가져온 추가 정보 (Scene별로 다름)
    public string debugInfo;
    
    public PerformanceDataPoint(string scene, SceneType type, float t, int objCount, int entCount, float f, float dt)
    {
        sceneName = scene;
        sceneType = type;
        time = t;
        objectCount = objCount;
        entityCount = entCount;
        fps = f;
        deltaTime = dt;
    }
}

public class PerformanceTracker : MonoBehaviour
{
    [Header("Tracking Settings")]
    public float sampleInterval = 1f; // 1초마다 샘플링
    public bool enableTracking = true;
    public string outputFileName = "performance_report";
    
    private List<PerformanceDataPoint> performanceData = new List<PerformanceDataPoint>();
    private float lastSampleTime = 0f;
    private float sessionStartTime;
    
    // FPS 계산용
    private float fpsAccumulator = 0f;
    private int fpsFrameCount = 0;
    private float fpsTimeLeft;
    
    private static PerformanceTracker instance;
    
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            sessionStartTime = Time.realtimeSinceStartup;
            fpsTimeLeft = sampleInterval;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        Debug.Log("PerformanceTracker: Started tracking performance data");
    }
    
    void Update()
    {
        if (!enableTracking) return;
        
        // FPS 계산
        fpsTimeLeft -= Time.unscaledDeltaTime;
        fpsAccumulator += Time.unscaledDeltaTime;
        fpsFrameCount++;
        
        // 샘플링 간격마다 데이터 수집
        if (Time.realtimeSinceStartup - lastSampleTime >= sampleInterval)
        {
            CollectPerformanceData();
            lastSampleTime = Time.realtimeSinceStartup;
        }
    }
    
    void CollectPerformanceData()
    {
        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        float currentTime = Time.realtimeSinceStartup - sessionStartTime;
        
        // FPS 계산 (0으로 나누기 방지)
        float avgFPS = fpsAccumulator > 0f ? fpsFrameCount / fpsAccumulator : 0f;
        float avgDeltaTime = fpsFrameCount > 0 ? fpsAccumulator / fpsFrameCount : 0f;
        
        // Scene 타입 및 디버거 감지
        SceneType sceneType = DetectSceneType();
        
        // 데이터 포인트 생성
        PerformanceDataPoint dataPoint = new PerformanceDataPoint(
            currentScene, 
            sceneType,
            currentTime, 
            0, // objectCount - 아래에서 설정
            0, // entityCount - 아래에서 설정
            avgFPS, 
            avgDeltaTime
        );
        
        // Scene 타입에 따른 데이터 수집
        if (sceneType == SceneType.Traditional)
        {
            CollectTraditionalData(dataPoint);
        }
        else if (sceneType == SceneType.ECS)
        {
            CollectECSData(dataPoint);
        }
        else
        {
            // 기본 데이터 수집
            dataPoint.objectCount = CountGameObjects();
            dataPoint.entityCount = CountEntities();
        }
        
        performanceData.Add(dataPoint);
        
        // FPS 카운터 리셋
        fpsAccumulator = 0f;
        fpsFrameCount = 0;
        
        //Debug.Log($"Performance Sample: Scene={currentScene} ({sceneType}), Objects={dataPoint.objectCount}, Entities={dataPoint.entityCount}, FPS={avgFPS:F1}");
    }
    
    SceneType DetectSceneType()
    {
        try
        {
            // GameObjectDebugger 확인 (Traditional)
            var gameObjectDebugger = FindObjectOfType<GameObjectDebugger>();
            if (gameObjectDebugger != null)
            {
                return SceneType.Traditional;
            }
            
            // EntityDebugger 확인 (ECS)
            var entityDebugger = FindObjectOfType<EntityDebugger>();
            if (entityDebugger != null)
            {
                return SceneType.ECS;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"PerformanceTracker: Error detecting scene type - {e.Message}");
        }
        
        return SceneType.Unknown;
    }
    
    void CollectTraditionalData(PerformanceDataPoint dataPoint)
    {
        var gameObjectDebugger = FindObjectOfType<GameObjectDebugger>();
        if (gameObjectDebugger != null)
        {
            // GameObjectDebugger에서 이미 계산된 데이터 가져오기
            dataPoint.objectCount = CountGameObjects();
            dataPoint.entityCount = 0; // Traditional scene에는 entity 없음
            
            // GameObjectDebugger의 정보를 문자열로 저장
            dataPoint.debugInfo = $"Players:{CountGameObjectsWithTag("Player")}, Enemies:{CountGameObjectsWithTag("Enemy")}, RenderObjects:{CountGameObjectsWithTag("RenderObject")}";
            
            //Debug.Log($"Traditional Data - {dataPoint.debugInfo}");
        }
        else
        {
            // 기본 데이터
            dataPoint.objectCount = CountGameObjects();
            dataPoint.entityCount = 0;
            dataPoint.debugInfo = "No GameObjectDebugger found";
        }
    }
    
    void CollectECSData(PerformanceDataPoint dataPoint)
    {
        var entityDebugger = FindObjectOfType<EntityDebugger>();
        if (entityDebugger != null)
        {
            // EntityDebugger에서 이미 계산된 데이터 가져오기
            dataPoint.objectCount = CountGameObjects();
            dataPoint.entityCount = CountEntities();
            
            // EntityDebugger의 정보를 문자열로 저장
            dataPoint.debugInfo = $"Entities:{dataPoint.entityCount}, Systems:{CountActiveSystems()}";
            
            //Debug.Log($"ECS Data - {dataPoint.debugInfo}");
        }
        else
        {
            // 기본 데이터
            dataPoint.objectCount = CountGameObjects();
            dataPoint.entityCount = CountEntities();
            dataPoint.debugInfo = "No EntityDebugger found";
        }
    }
    
    int CountGameObjectsWithTag(string tag)
    {
        try
        {
            GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag(tag);
            return taggedObjects != null ? taggedObjects.Length : 0;
        }
        catch (UnityException)
        {
            // Tag가 존재하지 않는 경우
            return 0;
        }
    }
    
    int CountActiveSystems()
    {
        try
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world != null && world.IsCreated)
            {
                return world.Systems.Count;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"PerformanceTracker: Error counting systems - {e.Message}");
        }
        
        return 0;
    }
    
    int CountGameObjects()
    {
        // 활성화된 모든 GameObject 개수
        GameObject[] allObjects = FindObjectsOfType<GameObject>(true);
        
        // DontDestroyOnLoad 객체는 제외하고 카운트
        int count = 0;
        foreach (GameObject obj in allObjects)
        {
            if (obj.scene.isLoaded && obj.scene.name != "DontDestroyOnLoad")
            {
                count++;
            }
        }
        
        return count;
    }
    
    int CountEntities()
    {
        try
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world != null && world.IsCreated)
            {
                var entityManager = world.EntityManager;
                
                // UniversalQuery 사용 (모든 Entity 포함)
                var query = entityManager.UniversalQuery;
                return query.CalculateEntityCount();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"PerformanceTracker: Error counting entities - {e.Message}");
        }
        
        return 0;
    }
    
    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SavePerformanceReport();
        }
    }
    
    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            SavePerformanceReport();
        }
    }
    
    void OnDestroy()
    {
        SavePerformanceReport();
    }
    
    void OnApplicationQuit()
    {
        SavePerformanceReport();
    }
    
    public void SavePerformanceReport()
    {
        if (performanceData.Count == 0)
        {
            Debug.Log("PerformanceTracker: No data to save");
            return;
        }
        
        try
        {
            string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string fileName = $"{outputFileName}_{timestamp}.txt";
            string filePath = Path.Combine(Application.persistentDataPath, fileName);
            
            StringBuilder report = new StringBuilder();
            
            // 헤더
            report.AppendLine("========================================");
            report.AppendLine("        PERFORMANCE TRACKING REPORT");
            report.AppendLine("========================================");
            report.AppendLine($"Generated: {System.DateTime.Now}");
            report.AppendLine($"Session Duration: {Time.realtimeSinceStartup - sessionStartTime:F2} seconds");
            report.AppendLine($"Total Samples: {performanceData.Count}");
            report.AppendLine();
            
            // Scene별로 데이터 그룹화
            var sceneGroups = performanceData.GroupBy(data => data.sceneName).ToList();
            
            // Scene별 요약 및 상세 데이터
            foreach (var sceneGroup in sceneGroups)
            {
                string sceneName = sceneGroup.Key;
                var sceneData = sceneGroup.ToList();
                
                report.AppendLine($"========================================");
                report.AppendLine($"SCENE: {sceneName}");
                report.AppendLine($"========================================");
                
                // Scene 통계
                float avgFPS = sceneData.Average(d => d.fps);
                float minFPS = sceneData.Min(d => d.fps);
                float maxFPS = sceneData.Max(d => d.fps);
                int maxObjects = sceneData.Max(d => d.objectCount);
                int maxEntities = sceneData.Max(d => d.entityCount);
                int avgObjects = (int)sceneData.Average(d => d.objectCount);
                int avgEntities = (int)sceneData.Average(d => d.entityCount);
                
                SceneType sceneType = sceneData.First().sceneType;
                
                report.AppendLine($"Scene Type: {sceneType}");
                report.AppendLine($"Sample Count: {sceneData.Count}");
                report.AppendLine($"FPS - Avg: {avgFPS:F1}, Min: {minFPS:F1}, Max: {maxFPS:F1}");
                report.AppendLine($"Objects - Avg: {avgObjects}, Max: {maxObjects}");
                report.AppendLine($"Entities - Avg: {avgEntities}, Max: {maxEntities}");
                
                // 디버거 정보 샘플
                var latestData = sceneData.OrderByDescending(d => d.time).FirstOrDefault();
                if (latestData != null && !string.IsNullOrEmpty(latestData.debugInfo))
                {
                    report.AppendLine($"Latest Debug Info: {latestData.debugInfo}");
                }
                
                report.AppendLine();
                
                // Scene별 상세 데이터 테이블
                report.AppendLine("Time(s)\tObjects\tEntities\tFPS\tDeltaTime(ms)\tDebug Info");
                report.AppendLine("--------------------------------------------------------------------------------");
                
                foreach (var data in sceneData.OrderBy(d => d.time))
                {
                    string debugInfo = string.IsNullOrEmpty(data.debugInfo) ? "N/A" : data.debugInfo;
                    report.AppendLine($"{data.time:F1}\t{data.objectCount}\t{data.entityCount}\t\t{data.fps:F1}\t{data.deltaTime * 1000:F1}\t\t{debugInfo}");
                }
                
                report.AppendLine();
                
                // Scene별 성능 분석
                report.AppendLine("PERFORMANCE ANALYSIS:");
                report.AppendLine("--------------------");
                
                // 객체 수 대비 FPS 분석
                var objectFPSCorrelation = AnalyzeObjectFPSCorrelation(sceneData);
                report.AppendLine($"Object Count vs FPS Correlation: {objectFPSCorrelation}");
                
                // 성능 등급 평가
                string performanceGrade = EvaluatePerformanceGrade(avgFPS);
                report.AppendLine($"Performance Grade: {performanceGrade}");
                
                report.AppendLine();
            }
            
            // 전체 요약
            report.AppendLine("========================================");
            report.AppendLine("OVERALL SUMMARY");
            report.AppendLine("========================================");
            
            foreach (var sceneGroup in sceneGroups)
            {
                var sceneData = sceneGroup.ToList();
                float avgFPS = sceneData.Average(d => d.fps);
                int avgObjects = (int)sceneData.Average(d => d.objectCount);
                int avgEntities = (int)sceneData.Average(d => d.entityCount);
                SceneType sceneType = sceneData.First().sceneType;
                
                report.AppendLine($"{sceneGroup.Key,-20} ({sceneType,-11}) | Avg FPS: {avgFPS:F1} | Avg Objects: {avgObjects,4} | Avg Entities: {avgEntities,4}");
            }
            
            File.WriteAllText(filePath, report.ToString());
            Debug.Log($"PerformanceTracker: Report saved to {filePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"PerformanceTracker: Failed to save report - {e.Message}");
        }
    }
    
    string AnalyzeObjectFPSCorrelation(List<PerformanceDataPoint> sceneData)
    {
        if (sceneData.Count < 2) return "Insufficient data";
        
        // 객체 수가 증가할 때 FPS가 감소하는지 분석
        var sortedByObjects = sceneData.OrderBy(d => d.objectCount + d.entityCount).ToList();
        float firstFPS = sortedByObjects.Take(sortedByObjects.Count / 3).Average(d => d.fps);
        float lastFPS = sortedByObjects.Skip(sortedByObjects.Count * 2 / 3).Average(d => d.fps);
        
        float performanceImpact = firstFPS - lastFPS;
        
        if (performanceImpact > 10) return "High negative correlation (significant performance impact)";
        else if (performanceImpact > 5) return "Moderate negative correlation";
        else if (performanceImpact > 0) return "Low negative correlation";
        else return "No significant correlation";
    }
    
    string EvaluatePerformanceGrade(float avgFPS)
    {
        if (avgFPS >= 55) return "A (Excellent)";
        else if (avgFPS >= 45) return "B (Good)";
        else if (avgFPS >= 30) return "C (Acceptable)";
        else if (avgFPS >= 20) return "D (Poor)";
        else return "F (Unacceptable)";
    }
    
    Dictionary<string, SceneStatistics> GenerateSceneStatistics()
    {
        var stats = new Dictionary<string, SceneStatistics>();
        
        foreach (var data in performanceData)
        {
            if (!stats.ContainsKey(data.sceneName))
            {
                stats[data.sceneName] = new SceneStatistics();
            }
            
            var stat = stats[data.sceneName];
            stat.sampleCount++;
            stat.totalFPS += data.fps;
            stat.avgFPS = stat.totalFPS / stat.sampleCount;
            stat.maxObjects = Mathf.Max(stat.maxObjects, data.objectCount);
            stat.maxEntities = Mathf.Max(stat.maxEntities, data.entityCount);
        }
        
        return stats;
    }
    
    [System.Serializable]
    public class SceneStatistics
    {
        public int sampleCount = 0;
        public float totalFPS = 0f;
        public float avgFPS = 0f;
        public int maxObjects = 0;
        public int maxEntities = 0;
    }
    
    // 수동으로 리포트 저장 (디버그용)
    [ContextMenu("Save Report Now")]
    public void SaveReportManually()
    {
        SavePerformanceReport();
    }
    
    // 데이터 초기화
    [ContextMenu("Clear Data")]
    public void ClearPerformanceData()
    {
        performanceData.Clear();
        sessionStartTime = Time.realtimeSinceStartup;
        Debug.Log("PerformanceTracker: Data cleared");
    }
}