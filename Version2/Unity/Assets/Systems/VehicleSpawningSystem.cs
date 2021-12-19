using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Physics;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics.Systems;

public class VehicleSpawningSystem : SystemBase
{

    protected override void OnUpdate()
    {
        EntityManager entityManager = World.EntityManager;

        Entities.ForEach((ref SpawningPointComponentData spawningPointComponentData, in LocalToWorld localToWorld, in Rotation rotation) =>
        {
            if (spawningPointComponentData.LastSpawnedCar == Entity.Null || !entityManager.Exists(spawningPointComponentData.LastSpawnedCar) || math.distance(entityManager.GetComponentData<LocalToWorld>(spawningPointComponentData.LastSpawnedCar).Position, localToWorld.Position) > 32)
            {

                /* Spawn a new car */
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

                spawningPointComponentData.LastSpawnedCar = carEntity;
            }
        }).WithStructuralChanges().Run();
    
    }

}
