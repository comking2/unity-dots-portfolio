using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections.LowLevel.Unsafe;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public partial struct SpawnerSystem : ISystem
{
    private Random m_Random;
    
    private int mSpawnIndex;                // 현재 인덱스
    
    private int mRandomPosition;
    private int mMaskValue;
    private float mTimeStart;
    
    public void OnCreate(ref SystemState state)
    {
        m_Random.InitState();
        mSpawnIndex = 0;
        mRandomPosition = 10;
        mMaskValue = 0;
        mTimeStart = (float)SystemAPI.Time.ElapsedTime;
        
        
        //ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);
    }

    public void OnDestroy(ref SystemState state) { }

    private void OnUpdate(ref SystemState state)
    {
        float currentTime = (float)(SystemAPI.Time.ElapsedTime - mTimeStart);
        var prefabLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);

        if (!VATRuntimeSettings.UseJobs)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            var random = m_Random;
            var spawnIndex = mSpawnIndex;
            var maskValue = mMaskValue;

            foreach (var (spawner, tf) in SystemAPI.Query<RefRW<Spawner>, RefRO<LocalTransform>>())
            {
                var prefabTransform = prefabLookup[spawner.ValueRO.Prefab];

                if (ProcessSpawn(ref spawner.ValueRW, tf.ValueRO, currentTime, ref random, ref spawnIndex, ref maskValue,
                        mRandomPosition, prefabTransform, out var spawnTransform, out var moveData))
                {
                    var newEntity = ecb.Instantiate(spawner.ValueRO.Prefab);
                    ecb.SetComponent(newEntity, moveData);
                    ecb.SetComponent(newEntity, spawnTransform);
                }
            }

            m_Random = random;
            mSpawnIndex = spawnIndex;
            mMaskValue = maskValue;
            return;
        }

        prefabLookup.Update(ref state);
        var jobRandom = new NativeReference<Random>(Allocator.TempJob);
        jobRandom.Value = m_Random;
        var jobIndex = new NativeReference<int>(Allocator.TempJob);
        jobIndex.Value = mSpawnIndex;
        var jobMask = new NativeReference<int>(Allocator.TempJob);
        jobMask.Value = mMaskValue;

        var ecbWriter = SystemAPI.GetSingleton<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

        var job = new SpawnJob
        {
            CurrentTime = currentTime,
            RandomPosition = mRandomPosition,
            PrefabLookup = prefabLookup,
            ECB = ecbWriter,
            RandomRef = jobRandom,
            SpawnIndexRef = jobIndex,
            MaskRef = jobMask
        };

        var handle = job.Schedule(state.Dependency);
        handle.Complete();
        state.Dependency = default;

        m_Random = jobRandom.Value;
        mSpawnIndex = jobIndex.Value;
        mMaskValue = jobMask.Value;

        jobRandom.Dispose();
        jobIndex.Dispose();
        jobMask.Dispose();
    }

    internal static bool ProcessSpawn(ref Spawner spawner, in LocalTransform transform, float currentTime,
        ref Random random, ref int spawnIndex, ref int maskValue, int randomPosition, in LocalTransform prefabTransform,
        out LocalTransform spawnTransform, out MoveableData moveData)
    {
        spawnTransform = default;
        moveData = default;

        if (!spawner.EnableSpawn || spawner.NextSpawnTime > currentTime)
        {
            return false;
        }

        randomPosition = math.max(1, randomPosition);
        float3 position = transform.Position;
        switch (spawner.SpawnType)
        {
            case SpawnType.ENEMY:
                position += NextSpawnOffset(ref random, ref spawnIndex, ref maskValue, randomPosition);
                break;
            case SpawnType.FIRE:
                position += new float3(0f, 1f, 0.5f);
                break;
        }

        moveData = new MoveableData
        {
            Direction = spawner.Direction,
            mSpeed = spawner.Speed
        };

        spawnTransform = LocalTransform.FromPositionRotationScale(position, prefabTransform.Rotation, prefabTransform.Scale);
        spawner.NextSpawnTime += spawner.SpawnRate;
        return true;
    }

    internal static float3 NextSpawnOffset(ref Random random, ref int spawnIndex, ref int maskValue, int randomPosition)
    {
        int lineCount = spawnIndex % randomPosition;
        if (lineCount == 0)
        {
            maskValue = (1 << randomPosition) - 1;
        }

        int randomValue = random.NextInt(0, math.max(1, randomPosition - lineCount));
        if ((maskValue & (1 << randomValue)) == 0)
        {
            randomValue++;
            while (randomValue < randomPosition && (maskValue & (1 << randomValue)) == 0)
            {
                randomValue++;
            }
        }

        maskValue &= ~(1 << (randomValue % math.max(1, randomPosition)));
        spawnIndex++;
        return new float3(randomValue, 0f, 0f);
    }

    [BurstCompile]
    internal partial struct SpawnJob : IJobEntity
    {
        public float CurrentTime;
        public int RandomPosition;
        [ReadOnly] public ComponentLookup<LocalTransform> PrefabLookup;
        public EntityCommandBuffer.ParallelWriter ECB;
        [NativeDisableParallelForRestriction] public NativeReference<Random> RandomRef;
        [NativeDisableParallelForRestriction] public NativeReference<int> SpawnIndexRef;
        [NativeDisableParallelForRestriction] public NativeReference<int> MaskRef;

        void Execute([EntityIndexInQuery] int sortKey, RefRW<Spawner> spawner, in LocalTransform transform)
        {
            var random = RandomRef.Value;
            var spawnIndex = SpawnIndexRef.Value;
            var maskValue = MaskRef.Value;

            var prefabTransform = PrefabLookup[spawner.ValueRO.Prefab];

            if (ProcessSpawn(ref spawner.ValueRW, transform, CurrentTime, ref random, ref spawnIndex, ref maskValue,
                    RandomPosition, prefabTransform, out var spawnTransform, out var moveData))
            {
                var newEntity = ECB.Instantiate(sortKey, spawner.ValueRO.Prefab);
                ECB.SetComponent(sortKey, newEntity, moveData);
                ECB.SetComponent(sortKey, newEntity, spawnTransform);
            }

            RandomRef.Value = random;
            SpawnIndexRef.Value = spawnIndex;
            MaskRef.Value = maskValue;
        }
    }
}
