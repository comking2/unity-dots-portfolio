using Unity.Burst;
using Unity.Entities;

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

        foreach (var (hp, e) in SystemAPI.Query<RefRW<Health>>()
                                         .WithAll<HitTag>()
                                         .WithEntityAccess())
        {
            hp.ValueRW.Value -= 1;          // 데미지 1
            ecb.RemoveComponent<HitTag>(e);
            if (hp.ValueRO.Value <= 0)
                ecb.DestroyEntity(e);       // 풀링이면 비활성 토글 추천
        }
    }
}