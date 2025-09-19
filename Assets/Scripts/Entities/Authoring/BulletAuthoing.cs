using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

public class BulletAuthoring : MonoBehaviour
{
    public float Radius = 0.5f;
    public int Damage = 1;
    class Baker : Baker<BulletAuthoring>
    {
        public override void Bake(BulletAuthoring a)
        {
            Debug.Log($"Baking BulletAuthoring {a.name}");
            // 이 GameObject(프리팹)를 엔티티 프리팹으로 변환
            var e = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent<BulletTag>(e);
            AddComponent(e, new Radius { Value = a.Radius });
            AddComponent(e, new DamageInfo { Value = a.Damage });
            AddComponent(e, new MoveableData());
        }
    }
}