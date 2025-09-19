using Unity.Entities;
using Unity.Transforms;
using Unity.Burst;
using Unity.Mathematics;

[BurstCompile]
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

    [BurstCompile]
    private void OnUpdate(ref SystemState state)
    {
        var time = SystemAPI.Time.ElapsedTime - mTimeStart;
        EntityCommandBuffer ecb = default;
        bool needECB = false;
        foreach (var (spawner, tf) in SystemAPI.Query<RefRW<Spawner>, RefRO<LocalTransform>>())
        {
            if(!spawner.ValueRO.EnableSpawn)
                continue;
            if (spawner.ValueRO.NextSpawnTime > time)
                continue;
            if (needECB == false)
            {
                var ecbSingleton = SystemAPI.GetSingleton<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>();
                ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
                needECB = true;
            }

            var newEntity = ecb.Instantiate(spawner.ValueRO.Prefab);

            float3 pos = tf.ValueRO.Position;
            switch (spawner.ValueRO.SpawnType)
            {
                case SpawnType.ENEMY: pos += NextPos(); break;
                case SpawnType.FIRE:  break;
            }
            
            ecb.SetComponent(newEntity, new MoveableData
            {
                Direction = spawner.ValueRO.Direction,
                mSpeed = spawner.ValueRO.Speed
            });
            
            var prefabLT = SystemAPI.GetComponentLookup<LocalTransform>(isReadOnly: true);
            var p = prefabLT[spawner.ValueRO.Prefab];
            ecb.SetComponent(newEntity, LocalTransform.FromPositionRotationScale(pos, p.Rotation, p.Scale));

            // 주기 드리프트 없이
            spawner.ValueRW.NextSpawnTime += spawner.ValueRO.SpawnRate;
        }
    }

    private float3 NextPos()
    {   //randomposition 은 31을 넘으면 안됨
        //Assert.IsTrue(mRandomPosition > 31);
        int linecount = mSpawnIndex % mRandomPosition;
        if (linecount == 0)
        {
            mMaskValue = (1 << mRandomPosition) - 1;
        }
        var random_value = m_Random.NextInt(0, mRandomPosition - linecount);
        if ((mMaskValue & (1 << random_value)) == 0)
        {
            random_value++;
            while ((mMaskValue & (1 << random_value)) == 0)
            {
                random_value++;
            }
        }
        mMaskValue &= ~(1 << random_value);
        mSpawnIndex++;
        return new float3((float)random_value, 0, 0);
    }
}