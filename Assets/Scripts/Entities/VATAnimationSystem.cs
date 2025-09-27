using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct VATAnimationSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<VATAnimationState>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        
        foreach (var (animationState, settings, entityTimeProp, speedProp, offsetProp) in SystemAPI
                     .Query<RefRW<VATAnimationState>, RefRO<VATAnimationSettings>, RefRW<VATTimeProperty>, RefRW<VATAnimSpeedProperty>, RefRW<VATAnimOffsetProperty>>())
        {
            
            animationState.ValueRW.ManualTime += deltaTime * settings.ValueRO.Speed;

            entityTimeProp.ValueRW.Value = animationState.ValueRO.ManualTime % (settings.ValueRO.FrameCount / settings.ValueRO.FrameRate);
            speedProp.ValueRW.Value = settings.ValueRO.Speed;
            offsetProp.ValueRW.Value = settings.ValueRO.Offset;
        }
    }
}
