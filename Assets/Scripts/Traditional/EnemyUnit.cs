using UnityEngine;

public class EnemyUnit : MonoBehaviour
{
    [Header("Enemy Settings")]
    public int health = 1;
    public float colliderRadius = 0.5f;
    
    private void Start()
    {
        // Collider가 없으면 추가
        if (GetComponent<Collider>() == null)
        {
            CapsuleCollider collider = gameObject.AddComponent<CapsuleCollider>();
            collider.isTrigger = true;
            collider.radius = colliderRadius;
            collider.height = 2f;
        }
        
        // Rigidbody가 없으면 추가 (트리거 충돌을 위해 필수)
        if (GetComponent<Rigidbody>() == null)
        {
            Rigidbody rb = gameObject.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = true; // 물리 시뮬레이션 비활성화
        }
    }
    
    public void TakeDamage(int damage)
    {
        Debug.Log($"EnemyUnit {name}: Taking {damage} damage, health: {health} -> {health - damage}");
        health -= damage;
        
        if (health <= 0)
        {
            Die();
        }
    }
    
    private void Die()
    {
        Debug.Log($"EnemyUnit {name}: Destroyed!");
        // 적 제거
        Destroy(gameObject);
    }
}