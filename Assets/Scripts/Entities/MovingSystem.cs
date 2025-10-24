using Unity.Entities;
using Unity.Transforms;
using Unity.Burst;
using Unity.Collections;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(SpawnerSystem))]
public partial struct ObjectMoveSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        
    }

    public void OnDestroy(ref SystemState state)
    {
    }

    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.fixedDeltaTime;

        if (!VATRuntimeSettings.UseJobs)
        {
            foreach (var (moveableData, transform) in SystemAPI
                         .Query<MoveableData, RefRW<LocalTransform>>())
            {
                IntegrateMovement(moveableData, ref transform.ValueRW, deltaTime);
            }

            return;
        }

        var moveJob = new MoveJob
        {
            deltaTime = deltaTime,
        };
        var handle = moveJob.ScheduleParallel(state.Dependency);
        state.Dependency = handle;
    }
    
    [BurstCompile]
    internal partial struct MoveJob : IJobEntity
    {
        public float deltaTime;

        // WithAll<EnemyTag> 필터는 쿼리에서 이미 적용됨
        void Execute(in MoveableData moveableData, ref LocalTransform transform)
        {
            IntegrateMovement(moveableData, ref transform, deltaTime);
        }
    }

    internal static void IntegrateMovement(in MoveableData moveableData, ref LocalTransform transform, float deltaTime)
    {
        transform.Position += moveableData.Direction * moveableData.mSpeed * deltaTime;
    }

}
