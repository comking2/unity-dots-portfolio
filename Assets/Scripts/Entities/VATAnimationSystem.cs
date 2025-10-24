using Unity.Burst;
using Unity.Entities;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct VATAnimationSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<VATAnimationState>();
    }

    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        if (!VATRuntimeSettings.UseJobs)
        {
            foreach (var (animationState, settings, entityTime, animOffset) in SystemAPI
                         .Query<RefRW<VATAnimationState>, RefRO<VATAnimationSettings>, RefRW<VATTimeProperty>, RefRW<VATAnimOffsetProperty>>())
            {
                AdvanceAnimation(ref animationState.ValueRW, settings.ValueRO, ref entityTime.ValueRW, ref animOffset.ValueRW, deltaTime);
            }

            return;
        }

        state.Dependency = new MoveJob
        {
            deltaTime = deltaTime,
        }.ScheduleParallel(state.Dependency);
    }

    [BurstCompile]
    internal static void AdvanceAnimation(ref VATAnimationState animationState, in VATAnimationSettings settings,
        ref VATTimeProperty entityTimeProperty, ref VATAnimOffsetProperty animOffsetProperty, float deltaTime)
    {
        animationState.ManualTime += deltaTime * settings.Speed;
        var framesSecond = settings.FrameCount / settings.FrameRate;
        if (animationState.ManualTime < 0)
        {
            animationState.ManualTime += framesSecond;
        }

        entityTimeProperty.Value = animationState.ManualTime % framesSecond;
        animOffsetProperty.Value = settings.Offset;
    }
}

[BurstCompile]
internal partial struct MoveJob : IJobEntity
{
    public float deltaTime;

    void Execute(ref VATAnimationState animationState, in VATAnimationSettings settings,
        ref VATTimeProperty entityTimeProperty, ref VATAnimOffsetProperty animOffsetProperty)
    {
        VATAnimationSystem.AdvanceAnimation(ref animationState, settings, ref entityTimeProperty, ref animOffsetProperty, deltaTime);
    }
}
