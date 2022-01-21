using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;

public class SplineVehicleSpawnerSystem : SystemBase
{

    public int maxVehicleNumber = 0;
    public int currentVehicleNumber = 0;

    protected override void OnUpdate()
    {
        double elapsedTime = Time.ElapsedTime;
        if (World.GetExistingSystem<StreetSplinePlacerSystem>().Enabled || World.GetExistingSystem<GraphGeneratorSystem>().Enabled) return;

        EntityManager entityManager = World.EntityManager;
        var getCarComponentDataFromEntity = GetComponentDataFromEntity<CarComponentData>();
        var getParentComponentDataFromEntity = GetComponentDataFromEntity<Parent>();
        var getLocalToWorldComponentDataFromEntity = GetComponentDataFromEntity<LocalToWorld>();
        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        var streetLocalToWorld = new LocalToWorld { };

        Entities.ForEach((ref SplineComponentData splineComponentData, in LocalToWorld localToWorld, in Entity spline) =>
        {
            if (splineComponentData.isSpawner && currentVehicleNumber < maxVehicleNumber &&
            splineComponentData.isOccupied == false && (elapsedTime - splineComponentData.lastTimeTriedToSpawn) > 3)
            {
                currentVehicleNumber++;

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

                //splineComponentData.isOccupied = true;

                var splineId = splineComponentData.id;
                var TrackEntity = splineComponentData.Track;

                if (!splineComponentData.isForward)
                    degree += 180;

                var newCarComponentData = new CarComponentData
                {
                    maxSpeed = 0.25f,
                    splineReachedAtTime = elapsedTime,
                    SplineId = splineId,
                    splineStart = spline,
                    splineEnd = spline,
                    Track = TrackEntity,
                    isOnStreet = true,
                    isPathUpdated = true,
                    HasJustSpawned = true
                };

                Entity carEntity = entityManager.Instantiate(splineComponentData.carEntity);
                entityManager.SetComponentData(carEntity, new Translation { Value = localToWorld.Position + 0.5f * math.normalize(localToWorld.Up) });
                entityManager.SetComponentData(carEntity, new Rotation { Value = quaternion.RotateY(math.radians(degree))});
                ecb.AddComponent(carEntity, newCarComponentData);

                splineComponentData.lastTimeTriedToSpawn = elapsedTime;
                splineComponentData.lastSpawnedCar = carEntity;
            }
            else if(splineComponentData.isSpawner && splineComponentData.isOccupied == true)
            {
                splineComponentData.lastTimeTriedToSpawn = elapsedTime;
            }
        }).WithStructuralChanges().Run();

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}
