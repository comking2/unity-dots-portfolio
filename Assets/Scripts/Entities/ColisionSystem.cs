using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public struct BulletTag : IComponentData {}
public struct EnemyTag  : IComponentData {}

public struct Radius : IComponentData { public float Value; } // 충돌 반지름
public struct Health : IComponentData { public int Value; }
public struct DamageInfo : IComponentData { public int Value; }
public struct HitInfo : IComponentData { public int Damage; } // 피격 표시(후처리용)

public struct EnemySnap { public Entity E; public float3 Pos; public float Radius; }

[BurstCompile]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(ObjectMoveSystem))]
public partial struct CollisionSystem : ISystem
{
    ComponentLookup<Radius> radiusLookup; // 반지름 빠르게 조회
    EntityQuery _enemiesQ;
    EntityQuery _bulletsQ;
    public void OnCreate(ref SystemState state)
    {
        radiusLookup = state.GetComponentLookup<Radius>(true);
         _enemiesQ = SystemAPI.QueryBuilder()
            .WithAll<LocalTransform, EnemyTag>()
            .Build();

        _bulletsQ = SystemAPI.QueryBuilder()
            .WithAll<LocalTransform, BulletTag, DamageInfo>()
            .Build();
    }

    public void OnUpdate(ref SystemState state)
    {
        radiusLookup.Update(ref state);

        var ecbSingleton = SystemAPI.GetSingleton<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>();
        var ecbParallel  = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

        int enemyCount = _enemiesQ.CalculateEntityCount();
        // 1) 적들 스냅샷 컨테이너 (TempJob로 잡에서 채움)
        var enemySnaps = new NativeList<EnemySnap>(Allocator.TempJob);
        enemySnaps.Capacity = math.max(enemySnaps.Capacity, enemyCount);
        // 1-1) 적 수집 잡 (병렬 추가: ParallelWriter + DeferredJobArray)
        var collectJob = new CollectEnemiesJob
        {
            EnemySnaps = enemySnaps.AsParallelWriter(),
            RadiusLookup = radiusLookup
        };
        var handle = collectJob.ScheduleParallel(_enemiesQ, state.Dependency);

        // 2) 총알 vs 적 충돌 잡
        //    적 리스트는 AsDeferredJobArray()로 길이를 런타임에 결정 가능 (수집 잡 완료 전에도 스케줄 OK)
        var bulletJob = new BulletVsEnemiesJob
        {
            EnemySnaps = enemySnaps.AsDeferredJobArray(),
            RadiusLookup = radiusLookup,
            Ecb = ecbParallel,
            ZCull          = 100f,
            DefaultEnemyR  = 0.5f,
            DefaultBulletR = 0.2f
        };
        handle = bulletJob.ScheduleParallel(_bulletsQ, handle);

        // 3) 디스포즈
        handle = enemySnaps.Dispose(handle);

        state.Dependency = handle;
    }
    [BurstCompile]
    public partial struct CollectEnemiesJob : IJobEntity
    {
        public NativeList<EnemySnap>.ParallelWriter EnemySnaps;

        [ReadOnly] public ComponentLookup<Radius> RadiusLookup;

        // WithAll<EnemyTag> 필터는 쿼리에서 이미 적용됨
        void Execute(Entity e, in LocalTransform tf)
        {
            EnemySnaps.AddNoResize(new EnemySnap
            {
                E = e,
                Pos = tf.Position,
                Radius = RadiusLookup.HasComponent(e) ? RadiusLookup[e].Value : 0.5f
            });
        }
    }

    // 총알 ↔ 적 충돌 체크
    [BurstCompile]
    public partial struct BulletVsEnemiesJob : IJobEntity
    {
        [ReadOnly] public NativeArray<EnemySnap> EnemySnaps;
        [ReadOnly] public ComponentLookup<Radius> RadiusLookup;

        public EntityCommandBuffer.ParallelWriter Ecb;

        public float ZCull;
        public float DefaultEnemyR;
        public float DefaultBulletR;

        // BulletTag 필터는 쿼리에서 적용
        void Execute([ChunkIndexInQuery] int sortKey, Entity bullet,in DamageInfo bdi, in LocalTransform btf)
        {
            // Z 컷오프
            if (btf.Position.z > ZCull)
            {
                Ecb.DestroyEntity(sortKey, bullet);
                return;
            }

            // 탄환 반지름
            float br = DefaultBulletR;
            if (RadiusLookup.HasComponent(bullet)) br = math.max(0f, RadiusLookup[bullet].Value);

            float3 bp = btf.Position;

            // 첫 피격만 처리 (여러 적 동시 피격 원하면 break 제거)
            for (int i = 0; i < EnemySnaps.Length; i++)
            {
                float rr = br + math.max(0f, EnemySnaps[i].Radius);
                float3 d = EnemySnaps[i].Pos - bp;
                d.y = 0; // 수평 거리만 체크
                if (math.lengthsq(d) <= rr * rr)
                {
                    Ecb.AddComponent<HitInfo>(sortKey, EnemySnaps[i].E, new HitInfo() { Damage = bdi.Value });
                    Ecb.DestroyEntity(sortKey, bullet);
                    break;
                }
            }
        }
    }
}