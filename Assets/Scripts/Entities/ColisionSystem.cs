using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public struct BulletTag : IComponentData {}
public struct EnemyTag  : IComponentData {}

public struct Radius : IComponentData { public float Value; } // 충돌 반지름
public struct Health : IComponentData { public int Value; }
public struct HitTag : IComponentData {} // 피격 표시(후처리용)

[BurstCompile]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(ObjectMoveSystem))]
public partial struct CollisionSystem : ISystem
{
    ComponentLookup<Radius> radiusLookup; // 반지름 빠르게 조회

    public void OnCreate(ref SystemState state)
    {
        radiusLookup = state.GetComponentLookup<Radius>(true);
    }

    public void OnUpdate(ref SystemState state)
    {
        radiusLookup.Update(ref state);

        // 지연 재생 ECB (FixedStep 끝에서 자동 Playback)
        var ecb = SystemAPI
            .GetSingleton<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        // 1) 적들 스냅샷 (위치 + 엔티티 + 반지름)
        var enemyPositions = new NativeList<float3>(Allocator.Temp);
        var enemyEntities  = new NativeList<Entity>(Allocator.Temp);
        var enemyRadii     = new NativeList<float>(Allocator.Temp);

        foreach (var (tf, e) in SystemAPI.Query<RefRO<LocalTransform>>()
                                         .WithAll<EnemyTag>()
                                         .WithEntityAccess())
        {
            enemyPositions.Add(tf.ValueRO.Position);
            enemyEntities.Add(e);

            float r = 0.5f;
            if (radiusLookup.HasComponent(e)) r = radiusLookup[e].Value;
            enemyRadii.Add(r);
        }

        // 2) 탄환 vs 적 거리 체크
        foreach (var (btf, bEnt) in SystemAPI.Query<RefRO<LocalTransform>>()
                                             .WithAll<BulletTag>()
                                             .WithEntityAccess())
        {
            // 탄환 반지름
            if(btf.ValueRO.Position.z > 100f)
            {
                ecb.DestroyEntity(bEnt);
                continue;
            }
            float br = 0.2f;
            if (radiusLookup.HasComponent(bEnt)) br = radiusLookup[bEnt].Value;

            float3 bp = btf.ValueRO.Position;

            // 간단히 첫 피격만 처리(여러 적에 동시 피격시키려면 break 제거)
            for (int i = 0; i < enemyEntities.Length; i++)
            {
                float rr = br + enemyRadii[i];
                float3 d = enemyPositions[i] - bp;

                if (math.lengthsq(d) <= rr * rr)
                {
                    // 피격 기록: 적에 HitTag, 탄환 제거(또는 풀 반납용 토글)
                    ecb.AddComponent<HitTag>(enemyEntities[i]);
                    ecb.DestroyEntity(bEnt); // 풀링이면 SetComponentEnabled<Active>(bEnt, false)

                    break;
                }
            }
        }

        enemyPositions.Dispose();
        enemyEntities.Dispose();
        enemyRadii.Dispose();
    }
}
