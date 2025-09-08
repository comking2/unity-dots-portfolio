using Unity.Mathematics;
using UnityEngine;
using Unity.Entities;
public struct MoveableData : IComponentData
{
    public float mSpeed;
    public float3 Direction;

}
