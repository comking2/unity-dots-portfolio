using Unity.Entities;
using Unity.Transforms;
using Unity.Burst;
using Unity.Collections;

[BurstCompile]
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

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var moveJob = new MoveJob
        {
            deltaTime = SystemAPI.Time.fixedDeltaTime,
            //RadiusLookup = radiusLookup
        };
        var handle = moveJob.ScheduleParallel(state.Dependency);
        state.Dependency = handle;
    }
    
    [BurstCompile]
    public partial struct MoveJob : IJobEntity
    {
        public float deltaTime;

        // WithAll<EnemyTag> 필터는 쿼리에서 이미 적용됨
        void Execute(Entity e,in MoveableData moveable_data, ref LocalTransform tf)
        {
            tf.Position+= moveable_data.Direction * moveable_data.mSpeed * deltaTime;
        }
    }

}
