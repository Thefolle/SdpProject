using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;

public static class Globals
{
    public static bool startStats = false;
    public static int maxVehicleNumber = 0;
    public static int currentVehicleNumber = 0;
    public static int numberDespawnedVehicles = 0;
    public static int numberOfSmallDistricts = 0;
    public static int numberOfMediumDistricts = 0;
    public static int numberOfLargeDistricts = 0;

    public static double heldTime = 0;
    public static int numberOfVehicleSpawnedInLastSecond = 0;
    public static int numberOfVehicleDespawnedInLastSecond = 0;
}

public class SplineVehicleSpawnerSystem : SystemBase
{

    public static int maxVehicleNumber = 0;
    public int currentVehicleNumber = 0;

    protected override void OnUpdate()
    {
        double elapsedTime = Time.ElapsedTime;
        if (World.GetExistingSystem<StreetSplinePlacerSystem>().Enabled || World.GetExistingSystem<GraphGeneratorSystem>().Enabled) return;

        if (!Globals.startStats)
            Globals.startStats = true;

        Globals.heldTime += Time.DeltaTime;
        if(Globals.heldTime >= 1)
        {
            Globals.heldTime = 0;
            Globals.numberOfVehicleSpawnedInLastSecond = 0;
            Globals.numberOfVehicleDespawnedInLastSecond = 0;
        }

        EntityManager entityManager = World.EntityManager;
        var getCarComponentDataFromEntity = GetComponentDataFromEntity<CarComponentData>();
        var getParentComponentDataFromEntity = GetComponentDataFromEntity<Parent>();
        var getLocalToWorldComponentDataFromEntity = GetComponentDataFromEntity<LocalToWorld>();
        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        var streetLocalToWorld = new LocalToWorld { };

        Entities.ForEach((ref SplineComponentData splineComponentData, in LocalToWorld localToWorld, in Entity spline) =>
        {
            if (splineComponentData.isSpawner && (elapsedTime - splineComponentData.lastTimeTriedToSpawn) > 10 ||
            (splineComponentData.isParkingSpawner && (elapsedTime - splineComponentData.lastTimeTriedToSpawn) > 1) &&
            (Globals.maxVehicleNumber == -1 || Globals.currentVehicleNumber < Globals.maxVehicleNumber) &&
            splineComponentData.isOccupied == false)
            {
                Globals.currentVehicleNumber++;
                Globals.numberOfVehicleSpawnedInLastSecond++;

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
                    HasJustSpawned = true,
                    isOnParkingArea = splineComponentData.isParkingSpawner
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
