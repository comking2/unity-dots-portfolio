using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

public class EnemyAuthoring : MonoBehaviour
{
    public float Radius = 0.5f;
    public int Health = 1;
    public Vector3 Direction = Vector3.forward;
    public float Speed = 2f;

    class Baker : Baker<EnemyAuthoring>
    {
        public override void Bake(EnemyAuthoring a)
        {
            Debug.Log($"Baking EnemyAuthoring {a.name}");
            // 이 GameObject(프리팹)를 엔티티 프리팹으로 변환
            var e = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent<EnemyTag>(e);
            AddComponent(e, new Radius { Value = a.Radius });
            AddComponent(e, new Health { Value = a.Health });
            AddComponent(e, new MoveableData
            {
                Direction = (float3)a.Direction,
                mSpeed = a.Speed
            });
        }
    }
}