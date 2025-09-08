using Unity.Entities;
using Unity.Transforms;
using Unity.Burst;

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
        foreach (var (moveinfo, tf) in SystemAPI.Query<RefRO<MoveableData>, RefRW<LocalTransform>>())
        {
                tf.ValueRW.Position += moveinfo.ValueRO.Direction * moveinfo.ValueRO.mSpeed * SystemAPI.Time.DeltaTime;
        }
    }
}
