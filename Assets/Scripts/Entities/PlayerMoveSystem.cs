using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

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
        bool is_stop = Mathf.Epsilon > Mathf.Abs(x);
        float3 dir = is_stop ? float3.zero : math.normalize(new float3(x, 0, 0));
        foreach (var(move, setting_anim) in SystemAPI.Query<RefRW<MoveableData>, RefRW<VATAnimationSettings>>().WithAll<PlayerTag>())
        {
            move.ValueRW.Direction = dir;
            setting_anim.ValueRW.Speed = Mathf.Clamp(-dir.x, -1f, 1f);
        }
    }
}