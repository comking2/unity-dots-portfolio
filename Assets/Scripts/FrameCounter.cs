using UnityEngine;
using Unity.Entities;
using Unity.Collections;

public class FrameCounter : MonoBehaviour
{
    // UI / FPS
    float mDeltaTime;
    Rect mRect;
    GUIStyle mStyle;

    // ECS
    EntityManager mEm;
    EntityQuery mRuntimeQ;   // Prefab/Disabled 제외 → 활성 엔티티
    EntityQuery mTotalQ;     // Prefab/Disabled 포함 → 전체 엔티티
    bool mInitialized;

    int mRuntimeCount;
    int mTotalCount;

    public float sampleInterval = 1f;
    float mSampleTimer;

    void OnEnable()
    {
        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null) return;

        mEm = world.EntityManager;

        // 활성(런타임)만
        mRuntimeQ = mEm.CreateEntityQuery(
            ComponentType.Exclude<Prefab>(),
            ComponentType.Exclude<Disabled>());

        // 전체(프리팹/비활성 포함)
        mTotalQ = mEm.CreateEntityQuery(new EntityQueryDesc {
            Options = EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IncludePrefab
        });

        // UI
        mStyle = new GUIStyle {
            alignment = TextAnchor.UpperLeft,
            fontSize = Mathf.Max(14, Screen.height * 2 / 50),
            normal = { textColor = Color.white }
        };
        mRect = new Rect(10, 10, Screen.width, Screen.height * 2 / 100);

        mInitialized = true;
    }

    void OnDisable()
    {
        if (!mInitialized) return;
        //mRuntimeQ.Dispose();
        //mTotalQ.Dispose();
        mInitialized = false;
    }

    void Update()
    {
        if (!mInitialized) return;
        // FPS 스무딩만 Update에서
        mDeltaTime += (Time.unscaledDeltaTime - mDeltaTime) * 0.1f;
    }

    void LateUpdate()
    {
        if (!mInitialized) return;

        mSampleTimer += Time.unscaledDeltaTime;
        if (mSampleTimer < sampleInterval) return;
        mSampleTimer = 0f;

        // 할당 없는 카운트
        mRuntimeCount = mRuntimeQ.CalculateEntityCount();
        mTotalCount   = mTotalQ.CalculateEntityCount();
    }

    void OnGUI()
    {
        if (!mInitialized) return;
        float fps = 1f / Mathf.Max(1e-6f, mDeltaTime);
        GUI.Label(mRect,
            $"{fps:0.} FPS | Entities Runtime: {mRuntimeCount} | Entities TOTAL: {mTotalCount}",
            mStyle);
    }
}