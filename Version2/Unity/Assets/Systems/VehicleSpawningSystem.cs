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

            //var carComponentData = entityManager.GetComponentData<CarComponentData>(carEntity);
            //entityManager.SetComponentData(carEntity, new CarComponentData { 
            //    AngularSpeed = carComponentData.AngularSpeed,
            //    HasJustSpawned = true,
            //    lastTimeTried = carComponentData.lastTimeTried,
            //    maxSpeed = carComponentData.maxSpeed,
            //    rightOvertakeAllowed = carComponentData.rightOvertakeAllowed,
            //    Speed = carComponentData.Speed,
            //    TrackId = carComponentData.TrackId,
            //    tryOvertake = carComponentData.tryOvertake,
            //    vehicleIsOn = VehicleIsOn.SpawningPoint
            //});

            entityManager.SetEnabled(carEntity, true);
        }).WithStructuralChanges().Run();
    
    }

}
