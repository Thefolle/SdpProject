using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using static UnityEngine.Debug;
using Unity.Collections;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public class SplineVehicleMovementSystem : SystemBase
{
    protected override void OnUpdate()
    {
        float deltaTime = Time.fixedDeltaTime;
        double elapsedTime = Time.ElapsedTime;
        float fixedDeltaTime = UnityEngine.Time.fixedDeltaTime;
        if (elapsedTime < 4 || World.GetExistingSystem<StreetSplinePlacerSystem>().Enabled) return;

        var getSplineComponentDataFromEntity = GetComponentDataFromEntity<SplineComponentData>();
        var getTrackComponentDataFromEntity = GetComponentDataFromEntity<TrackComponentData>();
        var getParentComponentDataFromEntity = GetComponentDataFromEntity<Parent>();
        var getChildComponentData = GetBufferFromEntity<Child>();
        var getPositionComponentDataFromEntity = GetComponentDataFromEntity<Translation>();
        var getLocalToWorldComponentDataFromEntity = GetComponentDataFromEntity<LocalToWorld>();

        EntityManager entityManager = World.EntityManager;

        Entities.ForEach((ref Translation translation, ref CarComponentData carComponentData, ref Rotation rotation,
            in Entity carEntity, in LocalToWorld localToWorld) =>
        {
            if (!carComponentData.isPathUpdated) return;
            var mySplineStart = new Entity();
            var mySplineStartComponentData = new SplineComponentData();
            var mySplineEnd = new Entity();
            bool arrived = false;

            if (getChildComponentData.HasComponent(carComponentData.Track))
            {
                var splines = getChildComponentData[carComponentData.Track];
                foreach (var spline in splines)
                {
                    /*     var splineComponentData = getSplineComponentDataFromEntity[spline.Value];
                        if (!getSplineComponentDataFromEntity[spline.Value].isLast)
                        {
                            if (getSplineComponentDataFromEntity[spline.Value].id == carComponentData.SplineId)
                            {
                                mySplineStart = spline.Value;
                                mySplineStartComponentData = splineComponentData;
                                entityManager.SetComponentData(spline.Value, new SplineComponentData
                                {
                                    id = splineComponentData.id,
                                    isLast = splineComponentData.isLast,
                                    Track = splineComponentData.Track,
                                    isOccupied = true
                                });
                            }
                            if (getSplineComponentDataFromEntity[spline.Value].id == (carComponentData.SplineId + 1))
                            {
                                mySplineEnd = spline.Value;
                            }
                        }
                        else
                        {
                            if (getSplineComponentDataFromEntity[spline.Value].id == (carComponentData.SplineId + 1))
                            {
                                mySplineEnd = spline.Value;
                                arrived = true;
                                break;
                            }

                        } */

                    var splineComponentData = getSplineComponentDataFromEntity[spline.Value];
                    if (getSplineComponentDataFromEntity[spline.Value].id == carComponentData.SplineId)
                    {
                        if (!getSplineComponentDataFromEntity[spline.Value].isLast)
                        {
                            // start and end
                                entityManager.SetComponentData(spline.Value, new SplineComponentData
                                {
                                    id = splineComponentData.id,
                                    isLast = splineComponentData.isLast,
                                    Track = splineComponentData.Track,
                                    isSpawner = splineComponentData.isSpawner,
                                    carEntity = splineComponentData.carEntity,
                                    lastSpawnedCar = splineComponentData.lastSpawnedCar,
                                    lastTimeSpawned = splineComponentData.lastTimeSpawned,
                                    isForward = splineComponentData.isForward,
                                    isOccupied = true
                                });

                                mySplineStart = spline.Value;
                                mySplineStartComponentData = splineComponentData;
                        }
                        else
                        {
                            // start and NOT end
                            entityManager.SetComponentData(spline.Value, new SplineComponentData
                            {
                                id = splineComponentData.id,
                                isLast = splineComponentData.isLast,
                                Track = splineComponentData.Track,
                                isSpawner = splineComponentData.isSpawner,
                                carEntity = splineComponentData.carEntity,
                                lastSpawnedCar = splineComponentData.lastSpawnedCar,
                                lastTimeSpawned = splineComponentData.lastTimeSpawned,
                                isForward = splineComponentData.isForward,
                                isOccupied = true
                            });
                            mySplineStart = spline.Value;
                            mySplineStartComponentData = splineComponentData;
                            arrived = true;

                            // TOGGLE AND UPDATE TRACK
                            carComponentData.isOnStreet = !carComponentData.isOnStreet;
                            carComponentData.isPathUpdated = false;


                            break;
                        }
                    }
                    else if (getSplineComponentDataFromEntity[spline.Value].id == (carComponentData.SplineId + 1))
                    {
                        mySplineEnd = spline.Value;
                    }
                }

            }

            if (getSplineComponentDataFromEntity.HasComponent(mySplineStart) && getSplineComponentDataFromEntity.HasComponent(mySplineEnd))
                if (!arrived)
                {
                    if (!getSplineComponentDataFromEntity[mySplineEnd].isOccupied)
                    {
                        var localToWorldSplineStart = getLocalToWorldComponentDataFromEntity[mySplineStart];
                        var localToWorldSplineEnd = getLocalToWorldComponentDataFromEntity[mySplineEnd];
                        var journeyLength = UnityEngine.Vector3.Distance(localToWorldSplineStart.Position, localToWorldSplineEnd.Position);
                        var distCovered = (elapsedTime - carComponentData.splineReachedAtTime) * carComponentData.maxSpeed * 100;
                        var fractionOfJourney = (float)distCovered / journeyLength;
                        translation.Value = UnityEngine.Vector3.Lerp(localToWorldSplineStart.Position, localToWorldSplineEnd.Position, fractionOfJourney);
                        //rotation.Value = UnityEngine.Quaternion.Lerp(localToWorldSplineStart.Rotation, localToWorldSplineEnd.Rotation, fractionOfJourney);
                        if (math.all(localToWorldSplineEnd.Position == localToWorld.Position))
                        {
                            carComponentData.SplineId = carComponentData.SplineId + 1;
                            carComponentData.splineReachedAtTime = elapsedTime;
                            entityManager.SetComponentData(mySplineStart, new SplineComponentData
                            {
                                id = mySplineStartComponentData.id,
                                isLast = mySplineStartComponentData.isLast,
                                Track = mySplineStartComponentData.Track,
                                isSpawner = mySplineStartComponentData.isSpawner,
                                carEntity = mySplineStartComponentData.carEntity,
                                lastSpawnedCar = mySplineStartComponentData.lastSpawnedCar,
                                lastTimeSpawned = mySplineStartComponentData.lastTimeSpawned,
                                isForward = mySplineStartComponentData.isForward,
                                isOccupied = false
                            });
                        }
                    }
                    else
                        carComponentData.splineReachedAtTime = elapsedTime;
                }
            // physicsVelocity.Linear = math.normalize(localToWorld.Forward) * carComponentData.Speed / fixedDeltaTime;

        }).Run();

    }
}

