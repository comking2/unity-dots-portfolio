using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
public struct Spawner : IComponentData
{
    public Entity Prefab;
    public float3 SpawnPosition;
    public float NextSpawnTime;
    public float SpawnRate;
    public SpawnType SpawnType;
    public float3 Direction;
    public float Speed;
    public bool EnableSpawn;

}

public struct MoveInfo : IComponentData
{
    public float3 Direction;
    public float Speed;

}


public enum SpawnType
{
    ENEMY,
    FIRE,
}
class SpawnerAuthoring : MonoBehaviour
{
    public GameObject Prefab;
    public float SpawnRate;
    public SpawnType mSpawnType;

    public float3 Direction;
    public float Speed;
    public bool EnableSpawn = true;
}

class SpawnerBaker : Baker<SpawnerAuthoring>
{
    public override void Bake(SpawnerAuthoring authoring)
    {
        Debug.Log($"Baking SpawnerAuthoring{authoring.name}");
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        var entity_prefab = GetEntity(authoring.Prefab, TransformUsageFlags.Dynamic);
        var spawner = new Spawner
        {
            // By default, each authoring GameObject turns into an Entity.
            // Given a GameObject (or authoring component), GetEntity looks up the resulting Entity.
            Prefab = entity_prefab,
            SpawnPosition = authoring.transform.position,
            NextSpawnTime = 0.0f,
            SpawnRate = authoring.SpawnRate,
            SpawnType = authoring.mSpawnType,
            Direction = authoring.Direction,
            Speed = authoring.Speed,
            EnableSpawn = authoring.EnableSpawn,
        };
        AddComponent(entity, spawner);
        
        AddComponent(entity, new LocalTransform
        {
            Position = authoring.transform.position,
            Rotation = authoring.transform.rotation,
            Scale = 1.0f
        });
    }
}