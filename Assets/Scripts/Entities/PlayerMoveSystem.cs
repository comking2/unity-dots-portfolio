using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

public struct PlayerTag : IComponentData {}

[UpdateAfter(typeof(InputSystem))]
public partial struct ApplyInputToMoveableSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        // InputSystem이 아닌 SimpleDragInput component를 체크
        state.RequireForUpdate<SimpleDragInput>();
        state.RequireForUpdate<PlayerTag>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var input = SystemAPI.GetSingleton<SimpleDragInput>();

        float x = input.DeltaX;
        float3 dir = x == 0 ? float3.zero : math.normalize(new float3(x, 0, 0));

        foreach (var move in SystemAPI.Query<RefRW<MoveableData>>().WithAll<PlayerTag>())
        {
            move.ValueRW.Direction = dir;
        }
    }
}