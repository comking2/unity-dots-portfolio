using Unity.Entities;
using UnityEngine;

/// <summary>
/// 화면 상단 중앙에 FPS / Entity 수 / GameObject 수를 표시한다.
/// 씬에 직접 배치하지 않아도 실행 시 자동으로 하나 생성되며, 씬이 바뀌어도 유지된다.
/// 이미 씬에 배치해 둔 경우에는 그것을 그대로 쓰고 새로 만들지 않는다.
/// </summary>
public class PerformanceHUD : MonoBehaviour
{
    /// <summary>자동 생성을 끄고 싶으면 씬 로드 전에 false로 바꾼다.</summary>
    public static bool AutoCreate = true;

    [Header("Display")]
    public bool visible = true;
    [Tooltip("수치 갱신 주기(초). 표시가 너무 흔들리면 값을 키운다.")]
    public float sampleInterval = 0.25f;
    public int fontSize = 20;
    public float panelWidth = 620f;
    public float topMargin = 8f;

    [Tooltip("도달한 최대 엔티티 수를 함께 표시한다. 용량 시연용.")]
    public bool showPeak = true;

    [Header("FPS 색상 기준")]
    public float goodFps = 60f;
    public float warnFps = 30f;

    // FPS 집계
    private float fps;
    private int frames;
    private float elapsed;

    // ECS
    private World cachedWorld;
    private EntityQuery runtimeEntityQuery;
    private bool ecsReady;
    private int entityCount;
    private int peakEntityCount;

    // Traditional
    private int objectCount;
    private int peakObjectCount;

    private float sampleTimer;

    // OnGUI는 프레임당 여러 번 호출되므로 문자열은 샘플링할 때만 만든다.
    private string cachedText = string.Empty;
    private GUIStyle labelStyle;
    private Texture2D panelTexture;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (!AutoCreate) return;
        if (FindObjectOfType<PerformanceHUD>() != null) return;

        var go = new GameObject("[PerformanceHUD]");
        go.AddComponent<PerformanceHUD>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        panelTexture = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
        panelTexture.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.55f));
        panelTexture.Apply();
    }

    private void OnDestroy()
    {
        if (panelTexture != null)
        {
            Destroy(panelTexture);
        }
    }

    private void Update()
    {
        frames++;
        elapsed += Time.unscaledDeltaTime;
        sampleTimer += Time.unscaledDeltaTime;

        if (sampleTimer < sampleInterval) return;
        sampleTimer = 0f;

        if (elapsed > 0f)
        {
            fps = frames / elapsed;
            frames = 0;
            elapsed = 0f;
        }

        entityCount = CountEntities();
        objectCount = ObjectSpawner.spawnParent != null ? ObjectSpawner.spawnParent.childCount : 0;

        if (entityCount > peakEntityCount) peakEntityCount = entityCount;
        if (objectCount > peakObjectCount) peakObjectCount = objectCount;

        RebuildText();
    }

    /// <summary>
    /// 월드가 바뀌었을 때만 쿼리를 다시 만든다. 씬 전환 중에는 월드가 잠깐
    /// 사라지므로 그때는 조용히 0을 반환한다.
    /// </summary>
    private int CountEntities()
    {
        World world = World.DefaultGameObjectInjectionWorld;

        if (world == null || !world.IsCreated)
        {
            cachedWorld = null;
            ecsReady = false;
            return 0;
        }

        if (!ecsReady || !ReferenceEquals(world, cachedWorld))
        {
            // 프리팹/비활성 엔티티는 제외한 실제 런타임 엔티티만 센다.
            runtimeEntityQuery = world.EntityManager.CreateEntityQuery(
                ComponentType.Exclude<Prefab>(),
                ComponentType.Exclude<Disabled>());

            cachedWorld = world;
            ecsReady = true;
        }

        try
        {
            return runtimeEntityQuery.CalculateEntityCount();
        }
        catch (System.Exception)
        {
            // 월드 정리 중이면 다음 샘플에서 다시 잡는다. (여기서 로그를 찍으면 스팸이 된다)
            ecsReady = false;
            return 0;
        }
    }

    private void RebuildText()
    {
        string fpsColor = ColorUtility.ToHtmlStringRGB(
            fps >= goodFps ? Color.green :
            fps >= warnFps ? Color.yellow : Color.red);

        var builder = new System.Text.StringBuilder(160);
        builder.Append("<color=#").Append(fpsColor).Append("><b>FPS ")
               .Append(fps.ToString("F1")).Append("</b></color>");

        if (ecsReady)
        {
            builder.Append("      Entities ").Append(entityCount.ToString("N0"));
            if (showPeak)
            {
                builder.Append(" <color=#888888>(peak ")
                       .Append(peakEntityCount.ToString("N0")).Append(")</color>");
            }
        }

        if (objectCount > 0)
        {
            builder.Append("      Objects ").Append(objectCount.ToString("N0"));
            if (showPeak)
            {
                builder.Append(" <color=#888888>(peak ")
                       .Append(peakObjectCount.ToString("N0")).Append(")</color>");
            }
        }

        cachedText = builder.ToString();
    }

    private void OnGUI()
    {
        if (!visible) return;

        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = fontSize,
                richText = true,
                normal = { textColor = Color.white }
            };
        }

        float height = fontSize + 16f;
        var rect = new Rect((Screen.width - panelWidth) * 0.5f, topMargin, panelWidth, height);

        GUI.DrawTexture(rect, panelTexture);
        GUI.Label(rect, cachedText, labelStyle);
    }

    /// <summary>측정을 새로 시작할 때 최고 기록을 초기화한다.</summary>
    public void ResetPeak()
    {
        peakEntityCount = 0;
        peakObjectCount = 0;
    }

    public void SetVisible(bool value) => visible = value;

    public float GetCurrentFPS() => fps;
    public int GetEntityCount() => entityCount;
    public int GetPeakEntityCount() => peakEntityCount;
    public int GetObjectCount() => objectCount;
}
