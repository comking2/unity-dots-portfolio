using UnityEngine;

public class FireProjectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    public int damage = 1;
    public float lifetime = 10f;
    
    private void Start()
    {
        // Collider가 없으면 추가
        if (GetComponent<Collider>() == null)
        {
            SphereCollider collider = gameObject.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = 0.2f;
        }
        
        // Rigidbody가 없으면 추가 (트리거 충돌을 위해 필수)
        if (GetComponent<Rigidbody>() == null)
        {
            Rigidbody rb = gameObject.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = true; // 물리 시뮬레이션 비활성화
        }
        
        // 일정 시간 후 자동 제거
        Destroy(gameObject, lifetime);
    }
    
    private void OnTriggerEnter(Collider other)
    {
        //Debug.Log($"FireProjectile: Trigger entered with {other.name}");
        
        // Enemy와 충돌했는지 확인
        EnemyUnit enemy = other.GetComponent<EnemyUnit>();
        if (enemy != null)
        {
            //Debug.Log($"FireProjectile: Hit enemy {other.name}, dealing {damage} damage");
            
            // 적에게 데미지 적용
            enemy.TakeDamage(damage);
            
            // 투사체 제거
            Destroy(gameObject);
        }
    }
}