using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

[BurstCompile]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(CollisionSystem))]
public partial struct ApplyHitSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI
            .GetSingleton<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);
            

        foreach (var (hp, hit, ltf, e) in SystemAPI.Query<RefRW<Health>, RefRO<HitInfo>, RefRO<LocalTransform>>()
                                         .WithAll<HitInfo>()
                                         .WithEntityAccess())
        {
            hp.ValueRW.Value -= hit.ValueRO.Damage;
            ecb.RemoveComponent<HitInfo>(e);
            if (hp.ValueRO.Value <= 0)
                ecb.DestroyEntity(e);       // 풀링이면 비활성 토글 추천
        }
    }
}