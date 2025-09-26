using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

/// <summary>
/// Authoring component for configuring VAT playback on an entity.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public sealed class VATAuthoring : MonoBehaviour
{
    public VATClipAsset Clip;
    public Material Material;

    public float PlaybackSpeed = 1f;
    public float PhaseOffset;
    public float RandomOffsetRange;
    public bool IsShaderTimeOverrideEnabled;
    public bool ShouldPlayOverrideOnStart = true;
    public float ShaderTimeOverrideStartTime;
    public float ShaderTimeOverrideSpeed = 1f;

    class Baker : Baker<VATAuthoring>
    {
        public override void Bake(VATAuthoring authoring)
        {
            var meshRenderer = authoring.GetComponent<MeshRenderer>();
            var meshFilter = authoring.GetComponent<MeshFilter>();

            var material = authoring.Material != null ? authoring.Material : meshRenderer.sharedMaterial;
            
            if (authoring.Material != null && meshRenderer.sharedMaterial != authoring.Material)
            {
                meshRenderer.sharedMaterial = authoring.Material;
            }

            if (meshFilter.sharedMesh != authoring.Clip.Mesh)
            {
                meshFilter.sharedMesh = authoring.Clip.Mesh;
            }

            int frameCount = math.max(1, authoring.Clip.FrameCount);
            float frameRate = math.max(0.0001f, authoring.Clip.FrameRate);
            float speed = math.abs(authoring.PlaybackSpeed) < math.FLT_MIN_NORMAL ? 1f : authoring.PlaybackSpeed;

            float offset = authoring.PhaseOffset;
            if (authoring.RandomOffsetRange > math.FLT_MIN_NORMAL)
            {
                uint seed = (uint)math.hash(new int2(authoring.GetInstanceID(), 0));
                seed = seed == 0 ? 1u : seed;
                var random = new Unity.Mathematics.Random(seed);
                offset += random.NextFloat(-authoring.RandomOffsetRange, authoring.RandomOffsetRange);
            }

            var entity = GetEntity(meshRenderer, TransformUsageFlags.Renderable);

            DependsOn(meshRenderer);
            DependsOn(meshRenderer.sharedMaterial);
            DependsOn(authoring.Clip);
            DependsOn(authoring.Clip.Mesh);
            DependsOn(authoring.Clip.PositionTexture);
            DependsOn(authoring.Clip.NormalTexture);

            AddComponent(entity, new VATAnimationSettings
            {
                FrameCount = frameCount,
                FrameRate = frameRate,
                Speed = speed,
                Offset = offset
            });

            AddComponent(entity, new VATAnimationState { Initialized = 0 });
            AddComponent(entity, new VATAnimStartTimeProperty { Value = 0f });
            AddComponent(entity, new VATAnimSpeedProperty { Value = speed });
            AddComponent(entity, new VATAnimOffsetProperty { Value = offset });
            AddComponent(entity, new VATFrameCountProperty { Value = frameCount });
            AddComponent(entity, new VATFrameRateProperty { Value = frameRate });
            AddComponent(entity, new VATVertsPerRowProperty { Value = authoring.Clip.VertsPerRow });
            AddComponent(entity, new VATSliceHeightProperty { Value = authoring.Clip.SliceHeight });

            if (authoring.IsShaderTimeOverrideEnabled)
            {
                AddComponent(entity, new EntitiesUnitShaderTimeController
                {
                    CurrentTime = authoring.ShaderTimeOverrideStartTime,
                    Speed = authoring.ShaderTimeOverrideSpeed,
                    UseOverride = 1,
                    Paused = (byte)(authoring.ShouldPlayOverrideOnStart ? 0 : 1)
                });

                AddComponent(entity, new EntitiesUnitShaderTimeProperty
                {
                    Value = authoring.ShaderTimeOverrideStartTime
                });

                AddComponent(entity, new EntitiesUnitShaderTimeEnabledProperty
                {
                    Value = 1f
                });
            }
        }
    }
}
