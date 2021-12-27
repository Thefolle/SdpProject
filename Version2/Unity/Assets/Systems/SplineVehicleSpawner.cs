using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;

public class SplineVehicleSpawner : SystemBase
{
    protected override void OnUpdate()
    {
        double elapsedTime = Time.ElapsedTime;
        if (elapsedTime < 5 ) return;

        EntityManager entityManager = World.EntityManager;
        var getCarComponentDataFromEntity = GetComponentDataFromEntity<CarComponentData>();
        var ecb = new EntityCommandBuffer(Allocator.TempJob);

        Entities.ForEach((ref SplineComponentData splineComponentData, in LocalToWorld localToWorld, in Entity spline) =>
        {
            if(splineComponentData.isSpawner &&
            splineComponentData.isOccupied == false && (elapsedTime - splineComponentData.lastTimeSpawned) > 3) 
            /*((splineComponentData.lastSpawnedCar == Entity.Null || !entityManager.Exists(splineComponentData.lastSpawnedCar)
            || entityManager.Exists(splineComponentData.lastSpawnedCar) && 
            getCarComponentDataFromEntity.HasComponent(splineComponentData.lastSpawnedCar) &&
            getCarComponentDataFromEntity[splineComponentData.lastSpawnedCar].SplineId > splineComponentData.id + 1)))*/
            {
                Entity carEntity = entityManager.Instantiate(splineComponentData.carEntity);
                entityManager.SetComponentData(carEntity, new Translation { Value = localToWorld.Position + 0.5f * math.normalize(localToWorld.Up) });

                splineComponentData.isOccupied = true;

                var splineId = splineComponentData.id;
                var TrackEntity = splineComponentData.Track;

                var newCarComponentData = new CarComponentData
                {
                    maxSpeed = 0.25f,
                    splineReachedAtTime = 2f,
                    SplineId = splineId,
                    Track = TrackEntity
                };

                ecb.AddComponent(carEntity, newCarComponentData);

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
