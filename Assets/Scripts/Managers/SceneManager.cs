    using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using Unity.Entities;

/// <summary>
/// 씬 전환 관리를 담당하는 매니저
/// </summary>
public class SceneManager : MonoBehaviour
{
    static SceneManager instance;
    [Header("Scene Settings")]
    public string[] sceneNames = { "SampleGameObjectScene", "ECStutorialScene" };
    public bool showLoadingScreen = true;
    public float minLoadingTime = 1f;
    
    [Header("Loading Settings")]
    public string loadingText = "Loading...";
    public bool asyncLoading = true;
    
    [Header("Performance Tracking")]
    public bool enablePerformanceTracking = true;
    
    // Events
    public System.Action<string> OnSceneLoadStarted;
    public System.Action<string> OnSceneLoadCompleted;
    public System.Action<float> OnLoadingProgress;
    
    private bool isLoading = false;
    private string currentSceneName;
    private AsyncOperation currentAsyncOperation;
    private PerformanceTracker performanceTracker;
    
    void Awake()
    {
        if(instance != null && instance != this)
        {
            DestroyImmediate(gameObject);
            return;
        }   
        instance = this;
        currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        DontDestroyOnLoad(gameObject);
    }
    
    void Start()
    {
        Debug.Log($"SceneManager initialized. Current scene: {currentSceneName}");
        
        // PerformanceTracker 초기화
        if (enablePerformanceTracking)
        {
            InitializePerformanceTracker();
        }
    }
    
    void InitializePerformanceTracker()
    {
        if (performanceTracker == null)
        {
            GameObject trackerObj = new GameObject("PerformanceTracker");
            performanceTracker = trackerObj.AddComponent<PerformanceTracker>();
            DontDestroyOnLoad(trackerObj);
            
            Debug.Log("SceneManager: PerformanceTracker initialized");
        }
    }
    
    void Update()
    {
        // 로딩 진행률 업데이트
        if (isLoading && currentAsyncOperation != null)
        {
            float progress = currentAsyncOperation.progress;
            OnLoadingProgress?.Invoke(progress);
        }
    }
    
    public void LoadScene(string sceneName)
    {
        if (isLoading)
        {
            Debug.LogWarning("Scene is already loading!");
            return;
        }
        
        if (!IsSceneValid(sceneName))
        {
            Debug.LogError($"Scene '{sceneName}' is not in the scene list!");
            return;
        }
        
        StartCoroutine(LoadSceneCoroutine(sceneName));
    }
    
    public void LoadScene(int sceneIndex)
    {
        if (sceneIndex < 0 || sceneIndex >= sceneNames.Length)
        {
            Debug.LogError($"Scene index {sceneIndex} is out of range!");
            return;
        }
        
        LoadScene(sceneNames[sceneIndex]);
    }
    
    public void LoadNextScene()
    {
        int currentIndex = GetCurrentSceneIndex();
        int nextIndex = (currentIndex + 1) % sceneNames.Length;
        LoadScene(nextIndex);
    }
    
    public void LoadPreviousScene()
    {
        int currentIndex = GetCurrentSceneIndex();
        int prevIndex = (currentIndex - 1 + sceneNames.Length) % sceneNames.Length;
        LoadScene(prevIndex);
    }
    
    public void ReloadCurrentScene()
    {
        LoadScene(currentSceneName);
    }
    
    private IEnumerator LoadSceneCoroutine(string sceneName)
    {
        isLoading = true;
        float startTime = Time.unscaledTime;
        
        // DOTS World 정리 (Scene 전환 전)
        CleanupDOTSWorld();
        
        // 로딩 시작
        OnSceneLoadStarted?.Invoke(sceneName);
        
        // 로딩 화면 표시
        if (showLoadingScreen)
        {
            ShowLoadingScreen(true);
        }
        
        // 비동기 씬 로딩
        if (asyncLoading)
        {
            currentAsyncOperation = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);
            currentAsyncOperation.allowSceneActivation = false;
            
            // 로딩 진행률 대기
            while (currentAsyncOperation.progress < 0.9f)
            {
                yield return null;
            }
            
            // 최소 로딩 시간 대기
            float elapsedTime = Time.unscaledTime - startTime;
            if (elapsedTime < minLoadingTime)
            {
                yield return new WaitForSecondsRealtime(minLoadingTime - elapsedTime);
            }
            
            // 씬 활성화
            currentAsyncOperation.allowSceneActivation = true;
            
            // 씬 로딩 완료 대기
            while (!currentAsyncOperation.isDone)
            {
                yield return null;
            }
        }
        else
        {
            // 동기 씬 로딩
            yield return new WaitForSecondsRealtime(minLoadingTime);
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
        }
        
        // 로딩 완료
        currentSceneName = sceneName;
        isLoading = false;
        currentAsyncOperation = null;
        
        // 로딩 화면 숨기기
        if (showLoadingScreen)
        {
            ShowLoadingScreen(false);
        }
        
        OnSceneLoadCompleted?.Invoke(sceneName);
        
        // 성능 추적기에 Scene 변경 알림
        if (performanceTracker != null)
        {
            Debug.Log($"SceneManager: Notifying PerformanceTracker of scene change to '{sceneName}'");
        }
        
        Debug.Log($"Scene '{sceneName}' loaded successfully");
    }
    
    private void ShowLoadingScreen(bool show)
    {
        // 로딩 화면 표시/숨김 (필요시 UI 매니저 연동)
        Debug.Log(show ? $"Loading: {loadingText}" : "Loading completed");
    }
    
    private void CleanupDOTSWorld()
    {
        try
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world != null && world.IsCreated)
            {
                Debug.Log("SceneManager: Cleaning up DOTS World before scene transition");
                
                var entityManager = world.EntityManager;
                
                // 간단한 Entity 정리 - UniversalQuery 사용
                var allEntities = entityManager.UniversalQuery;
                var entityArray = allEntities.ToEntityArray(Unity.Collections.Allocator.Temp);
                
                if (entityArray.Length > 0)
                {
                    Debug.Log($"SceneManager: Destroying {entityArray.Length} entities");
                    entityManager.DestroyEntity(entityArray);
                }
                entityArray.Dispose();
                
                Debug.Log("SceneManager: DOTS World cleanup completed");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"SceneManager: Error during DOTS World cleanup: {e.Message}");
        }
    }
    
    void OnDestroy()
    {
        // 성능 리포트 저장
        if (performanceTracker != null)
        {
            performanceTracker.SavePerformanceReport();
        }
    }
    
    void OnApplicationQuit()
    {
        // 성능 리포트 저장
        if (performanceTracker != null)
        {
            performanceTracker.SavePerformanceReport();
        }
    }
    
    private bool IsSceneValid(string sceneName)
    {
        foreach (string scene in sceneNames)
        {
            if (scene == sceneName)
                return true;
        }
        return false;
    }
    
    private int GetCurrentSceneIndex()
    {
        for (int i = 0; i < sceneNames.Length; i++)
        {
            if (sceneNames[i] == currentSceneName)
                return i;
        }
        return 0;
    }
    
    public void AddScene(string sceneName)
    {
        System.Array.Resize(ref sceneNames, sceneNames.Length + 1);
        sceneNames[sceneNames.Length - 1] = sceneName;
    }
    
    public void RemoveScene(string sceneName)
    {
        var sceneList = new System.Collections.Generic.List<string>(sceneNames);
        sceneList.Remove(sceneName);
        sceneNames = sceneList.ToArray();
    }
    
    // Getter methods
    public string GetCurrentSceneName() => currentSceneName;
    public bool IsLoading() => isLoading;
    public string[] GetSceneNames() => sceneNames;
    public float GetLoadingProgress() => currentAsyncOperation?.progress ?? 0f;
    
    public void SetMinLoadingTime(float time)
    {
        minLoadingTime = Mathf.Max(0f, time);
    }
    
    public void SetAsyncLoading(bool async)
    {
        asyncLoading = async;
    }
    
    public void SetShowLoadingScreen(bool show)
    {
        showLoadingScreen = show;
    }
}