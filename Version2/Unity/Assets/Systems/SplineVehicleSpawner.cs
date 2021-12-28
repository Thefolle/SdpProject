using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;

public class SplineVehicleSpawner : SystemBase
{
    protected override void OnUpdate()
    {
        double elapsedTime = Time.ElapsedTime;
        if (elapsedTime < 4 || World.GetExistingSystem<StreetSplinePlacerSystem>().Enabled) return;

        EntityManager entityManager = World.EntityManager;
        var getCarComponentDataFromEntity = GetComponentDataFromEntity<CarComponentData>();
        var getParentComponentDataFromEntity = GetComponentDataFromEntity<Parent>();
        var getLocalToWorldComponentDataFromEntity = GetComponentDataFromEntity<LocalToWorld>();
        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        var streetLocalToWorld = new LocalToWorld { };

        Entities.ForEach((ref SplineComponentData splineComponentData, in LocalToWorld localToWorld, in Entity spline) =>
        {
            if(splineComponentData.isSpawner &&
            splineComponentData.isOccupied == false && (elapsedTime - splineComponentData.lastTimeSpawned) > 3) 
            /*((splineComponentData.lastSpawnedCar == Entity.Null || !entityManager.Exists(splineComponentData.lastSpawnedCar)
            || entityManager.Exists(splineComponentData.lastSpawnedCar) && 
            getCarComponentDataFromEntity.HasComponent(splineComponentData.lastSpawnedCar) &&
            getCarComponentDataFromEntity[splineComponentData.lastSpawnedCar].SplineId > splineComponentData.id + 1)))*/
            {
                var ltwForward = math.normalize(localToWorld.Forward);
                int degree = 0;
                if (math.abs(ltwForward.x - 1f) < 0.00001 || math.abs(ltwForward.x - (-1f)) < 0.00001)
                {
                    if (math.abs(ltwForward.x - 1f) < 0.00001)
                        degree = 90;
                    else
                        degree = -90;
                }
                else if (math.abs(ltwForward.z - 1f) < 0.00001 || math.abs(ltwForward.z - (-1f)) < 0.00001)
                {
                    if (ltwForward.z == 1f)
                        degree = 0;
                    else
                        degree = 180;
                }
                else if (math.abs(ltwForward.x - 0.7071067) < 0.00001 && math.abs(ltwForward.z - 0.7071067) < 0.00001 ||
                math.abs(ltwForward.x - (-0.7071067)) < 0.00001 && math.abs(ltwForward.z - 0.7071067) < 0.00001)
                {
                    if (math.abs(ltwForward.x - 0.7071067) < 0.00001 && math.abs(ltwForward.z - 0.7071067) < 0.00001)
                        degree = 45;
                    else
                        degree = -45;
                }

                splineComponentData.isOccupied = true;

                var splineId = splineComponentData.id;
                var TrackEntity = splineComponentData.Track;

                if (!splineComponentData.isForward)
                    degree += 180;

                var newCarComponentData = new CarComponentData
                {
                    maxSpeed = 0.25f,
                    splineReachedAtTime = 2f,
                    SplineId = splineId,
                    Track = TrackEntity,
                    isOnStreet = true,
                    isPathUpdated = true,
                    HasJustSpawned = true
                };

                /*var newRotation = new Rotation
                {
                    Value = new quaternion(0, localToWorld.Rotation.value.y, 0, localToWorld.Rotation.value.w)
                };*/

                Entity carEntity = entityManager.Instantiate(splineComponentData.carEntity);
                entityManager.SetComponentData(carEntity, new Translation { Value = localToWorld.Position + 0.5f * math.normalize(localToWorld.Up) });
                entityManager.SetComponentData(carEntity, new Rotation { Value = quaternion.RotateY(math.radians(degree))});
                ecb.AddComponent(carEntity, newCarComponentData);
                //ecb.AddComponent(carEntity, newRotation);

                /*var carComponentData = getCarComponentDataFromEntity[carEntity];
                entityManager.SetComponentData(carEntity, new CarComponentData
                {
                    maxSpeed = 0.25f,
                    SplineId = splineComponentData.id,
                    Track = getParentComponentDataFromEntity[spline].Value
                });*/

                splineComponentData.lastTimeSpawned = elapsedTime;
                splineComponentData.lastSpawnedCar = carEntity;
            }
        }).WithStructuralChanges().Run();

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}
