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
        var handle = new MoveJob
        {
            deltaTime = SystemAPI.Time.DeltaTime,
        }.ScheduleParallel(state.Dependency);
        state.Dependency = handle;
    }
}

[BurstCompile]
    public partial struct MoveJob : IJobEntity
    {
        public float deltaTime;

        // WithAll<EnemyTag> 필터는 쿼리에서 이미 적용됨
        void Execute(Entity e,ref VATAnimationState animation_state, in VATAnimationSettings settings,ref VATTimeProperty entity_time_property, ref VATAnimOffsetProperty animoffset_property)
        {
            animation_state.ManualTime += deltaTime * settings.Speed;
            var framesSecond = settings.FrameCount / settings.FrameRate;
            if (animation_state.ManualTime < 0)
            {
                animation_state.ManualTime += framesSecond;
            }
            entity_time_property.Value = animation_state.ManualTime % framesSecond;
            animoffset_property.Value = settings.Offset;
        }
    }
