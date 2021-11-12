using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Physics;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics.Systems;
using static UnityEngine.Debug;
using Unity.Collections;

public class VehicleSpawningSystem : SystemBase
{

    protected override void OnUpdate()
    {
        PhysicsWorld physicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>().PhysicsWorld;
        EntityManager entityManager = World.EntityManager;
        var getTrackComponentDataFromEntity = GetComponentDataFromEntity<TrackComponentData>();
        var getParentComponentDataFromEntity = GetComponentDataFromEntity<Parent>();
        var getStreetComponentDataFromEntity = GetComponentDataFromEntity<StreetComponentData>();

        var deltaTime = World.Time.DeltaTime;

        Entities.ForEach((ref SpawningPointComponentData spawningPointComponentData, in LocalToWorld localToWorld, in Rotation rotation) =>
        {
            /* Manage spawn period*/
            spawningPointComponentData.CurrentTimeOffset += deltaTime;
            if (spawningPointComponentData.CurrentTimeOffset < spawningPointComponentData.SpawningPeriod)
            {
                return;
            }
            spawningPointComponentData.CurrentTimeOffset -= spawningPointComponentData.SpawningPeriod;

            /* Spawn a new car */
            entityManager.SetEnabled(spawningPointComponentData.CarPrefab, false);
            Entity carEntity = entityManager.Instantiate(spawningPointComponentData.CarPrefab);
            var carHeight = entityManager.GetComponentData<CompositeScale>(carEntity).Value.c1.y;

            entityManager.SetComponentData(carEntity, new Translation { Value = localToWorld.Position + (carHeight * 2) * math.normalize(localToWorld.Up) });
            entityManager.SetComponentData(carEntity, new Rotation { Value = rotation.Value });
            entityManager.SetEnabled(carEntity, true);
        }).WithStructuralChanges().Run();
    
    }

}
