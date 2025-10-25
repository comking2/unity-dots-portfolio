using UnityEngine;
using Unity.Mathematics;
using System.Collections;


public class ObjectSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject prefab;
    public float spawnRate = 1f;
    public SpawnType spawnType = SpawnType.ENEMY;
    public bool enableSpawn = true;
    
    [Header("Movement Settings")]
    public Vector3 direction = Vector3.forward;
    public float speed = 5f;
    
    [Header("Random Position Settings")]
    public int randomPosition = 10;
    
    private float nextSpawnTime = 0f;
    private int spawnIndex = 0;
    private int maskValue = 0;
    private float timeStart;

    static public Transform spawnParent;
    void Start()
    {
        timeStart = Time.time;
        
        if (enableSpawn)
        {
            StartCoroutine(SpawnRoutine());
        }
    }
    
    void Update()
    {
        if (enableSpawn)
        {
            float currentTime = Time.time - timeStart;
            
            if (currentTime >= nextSpawnTime)
            {
                SpawnObject();
                nextSpawnTime += spawnRate;
            }
        }
    }
    
    private IEnumerator SpawnRoutine()
    {
        while (enableSpawn)
        {
            yield return new WaitForSeconds(spawnRate);
            
            if (enableSpawn)
            {
                SpawnObject();
            }
        }
    }
    
    private void SpawnObject()
    {
        if (prefab == null) return;
        if(spawnParent == null)
        {
            spawnParent = new GameObject($"{spawnType}_Spawner").transform;
        }
        Vector3 spawnPosition = CalculateSpawnPosition();
        GameObject spawnedObject = Instantiate(prefab, spawnPosition, prefab.transform.rotation, spawnParent);
        
        // 스폰 타입에 따른 컴포넌트 추가
        SetupSpawnedObject(spawnedObject);
        
        // MovableObject 컴포넌트가 있으면 이동 설정
        MovableObject movableObject = spawnedObject.GetComponent<MovableObject>();
        if (movableObject != null)
        {
            movableObject.SetMovement(direction, speed);
        }
        
        // PlayerController가 있으면 (적이 아닌 경우) 이동 설정
        PlayerController playerController = spawnedObject.GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.moveSpeed = speed;
        }
    }
    
    private void SetupSpawnedObject(GameObject spawnedObject)
    {
        switch (spawnType)
        {
            case SpawnType.ENEMY:
                // Enemy 컴포넌트가 없으면 추가
                if (spawnedObject.GetComponent<EnemyUnit>() == null)
                {
                    spawnedObject.AddComponent<EnemyUnit>();
                }
                break;
                
            case SpawnType.FIRE:
                // FireProjectile 컴포넌트가 없으면 추가
                if (spawnedObject.GetComponent<FireProjectile>() == null)
                {
                    spawnedObject.AddComponent<FireProjectile>();
                }
                break;
        }
    }
    
    private Vector3 CalculateSpawnPosition()
    {
        Vector3 basePosition = transform.position;
        
        switch (spawnType)
        {
            case SpawnType.ENEMY:
                return basePosition + CalculateRandomOffset();
            case SpawnType.FIRE:
                return basePosition + new Vector3(0f, 1f, 0.5f);
            default:
                return basePosition;
        }
    }
    
    private Vector3 CalculateRandomOffset()
    {
        int lineCount = spawnIndex % randomPosition;
        if (lineCount == 0)
        {
            maskValue = (1 << randomPosition) - 1;
        }
        
        int randomValue = UnityEngine.Random.Range(0, Mathf.Max(1, randomPosition - lineCount));
        if ((maskValue & (1 << randomValue)) == 0)
        {
            randomValue++;
            while (randomValue < randomPosition && (maskValue & (1 << randomValue)) == 0)
            {
                randomValue++;
            }
        }
        
        maskValue &= ~(1 << (randomValue % Mathf.Max(1, randomPosition)));
        spawnIndex++;
        
        return new Vector3(randomValue, 0f, 0f);
    }
    
    public void SetSpawnEnabled(bool enabled)
    {
        enableSpawn = enabled;
        
        if (enabled)
        {
            StartCoroutine(SpawnRoutine());
        }
    }
    
    public void SetSpawnRate(float rate)
    {
        spawnRate = rate;
    }
    
    public void SetDirection(Vector3 newDirection)
    {
        direction = newDirection;
    }
    
    public void SetSpeed(float newSpeed)
    {
        speed = newSpeed;
    }
}