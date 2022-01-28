using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Debug;

public class SplineVehicleSpawnerSystem : SystemBase
{
    protected override void OnStartRunning()
    {
        base.OnStartRunning();

        var prefabs = GetEntityQuery(typeof(PrefabComponentData)).ToComponentDataArray<PrefabComponentData>(Allocator.Temp)[0];
        SetSingleton(prefabs);
    }

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
        //var getSplineBuffer = GetBufferFromEntity<SplineBufferComponentData>();
        var getSplineComponentData = GetComponentDataFromEntity<SplineComponentData>();

        Entities.ForEach((ref SplineComponentData splineComponentData, in LocalToWorld localToWorld, in Entity spline) =>
        {
            if (splineComponentData.isSpawner)
            {
                var track = splineComponentData.Track;
                var splines = entityManager.GetBuffer<SplineBufferComponentData>(track);
                var precedingSpline = splines[splineComponentData.id - 1].spline;
                var precedingSplineComponentData = entityManager.GetComponentData<SplineComponentData>(precedingSpline);
                var nextSpline = splines[splineComponentData.id + 1].spline;
                var nextSplineComponentData = entityManager.GetComponentData<SplineComponentData>(nextSpline);

                var ltwForward = math.normalize(localToWorld.Forward);

                /* Compute degree */
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

                if (!splineComponentData.isOccupied && !precedingSplineComponentData.isOccupied && !nextSplineComponentData.isOccupied && splineComponentData.isForward && (elapsedTime - splineComponentData.lastTimeTriedToSpawn) > 3 && entityManager.HasComponent<BusStopLinesBuffer>(getParentComponentDataFromEntity[getParentComponentDataFromEntity[track].Value].Value))
                {
                    var busPrefab = GetSingleton<PrefabComponentData>().Bus;
                    Entity bus = ecb.Instantiate(busPrefab);
                    ecb.SetComponent(bus, new Translation { Value = localToWorld.Position + 1f * math.normalize(localToWorld.Up) });
                    ecb.SetComponent(bus, new Rotation { Value = quaternion.RotateY(math.radians(degree)) });

                    var newCarComponentData = new CarComponentData
                    {
                        maxSpeed = 0.25f,
                        splineReachedAtTime = elapsedTime,
                        SplineId = splineComponentData.id,
                        splineStart = spline,
                        splineEnd = spline,
                        Track = splineComponentData.Track,
                        isOnStreet = true,
                        isPathUpdated = true,
                        HasJustSpawned = true,
                        IsBus = true
                    };
                    ecb.SetComponent(bus, newCarComponentData);

                    precedingSplineComponentData.isOccupied = true;
                    ecb.SetComponent(precedingSpline, precedingSplineComponentData);
                    splineComponentData.isOccupied = true;
                    nextSplineComponentData.isOccupied = true;
                    ecb.SetComponent(nextSpline, nextSplineComponentData);

                    var occupiedSplines = ecb.AddBuffer<SplineBufferComponentData>(bus);
                    occupiedSplines.Add(new SplineBufferComponentData { spline = precedingSpline });
                    occupiedSplines.Add(new SplineBufferComponentData { spline = spline });
                    occupiedSplines.Add(new SplineBufferComponentData { spline = nextSpline });

                    splineComponentData.lastTimeTriedToSpawn = elapsedTime;
                    //splineComponentData.lastSpawnedCar = bus;
                }
                else if (!splineComponentData.isOccupied && (elapsedTime - splineComponentData.lastTimeTriedToSpawn) > 3)
                {

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

                    var prefabs = GetSingleton<PrefabComponentData>();
                    Entity carEntity = ecb.Instantiate(prefabs.Car);
                    ecb.SetComponent(carEntity, new Translation { Value = localToWorld.Position + 0.5f * math.normalize(localToWorld.Up) });
                    ecb.SetComponent(carEntity, new Rotation { Value = quaternion.RotateY(math.radians(degree)) });
                    ecb.SetComponent(carEntity, newCarComponentData);

                    splineComponentData.lastTimeTriedToSpawn = elapsedTime;
                    //splineComponentData.lastSpawnedCar = carEntity;
                }
                else if (splineComponentData.isOccupied == true)
                {
                    splineComponentData.lastTimeTriedToSpawn = elapsedTime;
                }
            }
            
        }).WithStructuralChanges().Run();

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}
