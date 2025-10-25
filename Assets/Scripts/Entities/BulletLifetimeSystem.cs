using Unity.Burst;
using Unity.Entities;

[BurstCompile]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public partial struct BulletLifetimeSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        
        var ecbSingleton = SystemAPI.GetSingleton<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
        
        // 모든 탄환의 수명 감소 및 제거 처리
        foreach (var (lifetime, entity) in SystemAPI.Query<RefRW<BulletLifetime>>()
                                                    .WithAll<BulletTag>()
                                                    .WithEntityAccess())
        {
            lifetime.ValueRW.Value -= deltaTime;
            
            if (lifetime.ValueRO.Value <= 0f)
            {
                ecb.DestroyEntity(entity);
            }
        }
    }
}