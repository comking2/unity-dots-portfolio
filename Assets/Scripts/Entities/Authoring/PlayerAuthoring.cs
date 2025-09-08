using Unity.Entities;
using UnityEngine;

class PlayerAuthoring : MonoBehaviour
{
    public float mSpeed = 1f;
}

class PlayerBaker : Baker<PlayerAuthoring>
{
    public override void Bake(PlayerAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new PlayerTag());
        AddComponent(entity, new MoveableData(){ mSpeed = authoring.mSpeed, Direction = new Unity.Mathematics.float3() });
    }
}