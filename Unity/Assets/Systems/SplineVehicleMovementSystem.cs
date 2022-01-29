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
        double elapsedTime = Time.ElapsedTime;

        if (World.GetExistingSystem<StreetSplinePlacerSystem>().Enabled || World.GetExistingSystem<GraphGeneratorSystem>().Enabled) return;

        var getSplineComponentDataFromEntity = GetComponentDataFromEntity<SplineComponentData>();
        var getChildComponentData = GetBufferFromEntity<Child>();
        var getLocalToWorldComponentDataFromEntity = GetComponentDataFromEntity<LocalToWorld>();
        var getParentComponentData = GetComponentDataFromEntity<Parent>();
        var getSplineBufferComponentData = GetBufferFromEntity<SplineBufferComponentData>();
        var getTrackComponentData = GetComponentDataFromEntity<TrackComponentData>();
        var getLocalToWorldComponentData = GetComponentDataFromEntity<LocalToWorld>();


        EntityManager entityManager = World.EntityManager;
        var ecb = new EntityCommandBuffer(Allocator.TempJob);

        Entities.ForEach((ref Translation translation, ref CarComponentData carComponentData, ref Rotation rotation, in Entity carEntity) =>
        {
            if (!carComponentData.IsBus)
            {
                var mySplineStartComponentData = getSplineComponentDataFromEntity[carComponentData.splineStart];
                if (carComponentData.HasReachedDestination)
                {
                    mySplineStartComponentData.isOccupied = false;
                    ecb.SetComponent(carComponentData.splineStart, mySplineStartComponentData);
                    ecb.SetComponent(carEntity, new AskToDespawnComponentData { Asked = true });
                    //carComponentData.askToDespawn = true;
                    return;
                }
                if (carComponentData.needToUpdatedPath && !carComponentData.isPathUpdated)
                {
                    carComponentData.splineReachedAtTime = elapsedTime;
                    return;
                }

                var mySplineEndComponentData = getSplineComponentDataFromEntity[carComponentData.splineEnd];
                if (carComponentData.needToUpdatedPath)
                {
                    carComponentData.SplineId = 0;
                    var splineBufferComponentData = getSplineBufferComponentData[carComponentData.Track];
                    if (splineBufferComponentData.Length > carComponentData.SplineId) // splineId may refer to a non-existing spline (off-by-one error)
                    {
                        var spline = splineBufferComponentData[carComponentData.SplineId].spline;
                        carComponentData.splineEnd = spline;
                    }
                    carComponentData.needToUpdatedPath = false;
                }

                //if(!mySplineEndComponentData.isOccupied)
                if (!mySplineEndComponentData.isOccupied || (mySplineEndComponentData.isOccupied && carComponentData.isOccupying))
                {
                    carComponentData.isOccupying = true;

                    mySplineEndComponentData.isOccupied = true;
                    ecb.SetComponent(carComponentData.splineEnd, mySplineEndComponentData);

                    var localToWorldSplineStart = getLocalToWorldComponentDataFromEntity[carComponentData.splineStart];
                    var localToWorldSplineEnd = getLocalToWorldComponentDataFromEntity[carComponentData.splineEnd];
                    var journeyLength = UnityEngine.Vector3.Distance(localToWorldSplineStart.Position, localToWorldSplineEnd.Position);
                    var distCovered = (elapsedTime - carComponentData.splineReachedAtTime) * carComponentData.maxSpeed * 100;
                    var fractionOfJourney = (float)distCovered / journeyLength;

                    translation.Value = UnityEngine.Vector3.Lerp(localToWorldSplineStart.Position, localToWorldSplineEnd.Position, fractionOfJourney);
                    //if (!carComponentData.isOnStreet) rotation.Value = UnityEngine.Quaternion.Lerp(localToWorldSplineStart.Rotation, localToWorldSplineEnd.Rotation, fractionOfJourney);

                    if (math.all(localToWorldSplineEnd.Position == translation.Value))
                    {
                        if (mySplineEndComponentData.isDespawner)
                        {
                            mySplineStartComponentData.isOccupied = false;
                            ecb.SetComponent(carComponentData.splineStart, mySplineStartComponentData);
                            mySplineEndComponentData.isOccupied = false;
                            ecb.SetComponent(carComponentData.splineEnd, mySplineEndComponentData);

                            ecb.SetComponent(carEntity, new AskToDespawnComponentData { Asked = true });

                            //carComponentData.askToDespawn = true;
                            return;
                        }
                        else
                        {
                            if (mySplineEndComponentData.isParkingEntrance) carComponentData.isOnParkingArea = true;
                            else if (mySplineEndComponentData.isParkingExit) carComponentData.isOnParkingArea = false;

                            if (!carComponentData.isOnStreet || carComponentData.isOnParkingArea) rotation.Value = localToWorldSplineEnd.Rotation;

                            carComponentData.SplineId = carComponentData.SplineId + 1;
                            carComponentData.splineReachedAtTime = elapsedTime;

                            mySplineStartComponentData.isOccupied = false;
                            ecb.SetComponent(carComponentData.splineStart, mySplineStartComponentData);



                            carComponentData.isOccupying = false;

                            carComponentData.splineStart = carComponentData.splineEnd;

                            if (mySplineEndComponentData.isLast)
                            {
                                // TOGGLE AND UPDATE TRACK
                                carComponentData.isOnStreet = !carComponentData.isOnStreet;
                                carComponentData.isPathUpdated = false;
                                carComponentData.needToUpdatedPath = true;
                            }
                            else
                            {
                                bool overtakeEnabled = true;
                                if (overtakeEnabled)
                                {
                                    carComponentData.splineEnd = getNextNode(entityManager, getSplineBufferComponentData, getTrackComponentData, getSplineComponentDataFromEntity, carComponentData, carEntity);
                                }
                                else
                                {
                                    var splineBufferComponentData = getSplineBufferComponentData[carComponentData.Track];
                                    if (splineBufferComponentData.Length > carComponentData.SplineId) // splineId may refer to a non-existing spline (off-by-one error)
                                    {
                                        var spline = splineBufferComponentData[carComponentData.SplineId].spline;
                                        carComponentData.splineEnd = spline;
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    carComponentData.splineReachedAtTime = elapsedTime;
                }
            }
            else
            {
                if (carComponentData.HasReachedDestination)
                {
                    var occupiedSplines = getSplineBufferComponentData[carEntity]; // bus occupied splines
                    foreach (var spline in occupiedSplines)
                    {
                        var splineComponentData = getSplineComponentDataFromEntity[spline.spline];
                        splineComponentData.isOccupied = false;
                    }
                    entityManager.SetComponentData(carEntity, new AskToDespawnComponentData { Asked = true });
                    Globals.currentBusNumber--;
                    return;
                }
                if (carComponentData.needToUpdatedPath && !carComponentData.isPathUpdated)
                {
                    carComponentData.splineReachedAtTime = elapsedTime;
                    return;
                }

                var splineEnd = carComponentData.splineEnd;
                if (getLocalToWorldComponentData[splineEnd].Position.x == translation.Value.x && getLocalToWorldComponentData[splineEnd].Position.z == translation.Value.z) // check the position neglecting the y coordinate
                {
                    /* Keep in mind that carComponentData.splineId refers to the spline at the center of the bus */
                    var splineBuffer = getSplineBufferComponentData[carComponentData.Track]; // beware that the track is updated as soon as the front spline is free, not when the center of the bus reaches the new track
                    int frontSplineId; // the id of the spline in front of the bus

                    if (carComponentData.needToUpdatedPath && carComponentData.isPathUpdated)
                    {
                        frontSplineId = 0;
                        var firstSplineComponentData = getSplineComponentDataFromEntity[splineBuffer[frontSplineId].spline];
                        if (firstSplineComponentData.isOccupied) return; // retry on next frame in this same if block
                        carComponentData.needToUpdatedPath = false;
                        carComponentData.SplineId = -1; // prepare for next frames, don't use in this frame
                    }
                    else
                    {
                        frontSplineId = carComponentData.SplineId + 2;
                    }

                    // if the front spline is the one after the last one (that is, it does not exist)
                    if (frontSplineId == splineBuffer.Length)
                    {
                        carComponentData.needToUpdatedPath = true;
                        carComponentData.isPathUpdated = false;
                        carComponentData.isOnStreet = !carComponentData.isOnStreet;
                        return;
                    }
                    var frontSpline = splineBuffer[frontSplineId].spline;
                    var frontSplineComponentData = getSplineComponentDataFromEntity[frontSpline];

                    if (frontSplineComponentData.isOccupied) return; // retry on next frame

                    frontSplineComponentData.isOccupied = true;
                    entityManager.SetComponentData(frontSpline, frontSplineComponentData);


                    if (!carComponentData.isOnStreet) rotation.Value = getLocalToWorldComponentData[splineEnd].Rotation;

                    var occupiedSplines = getSplineBufferComponentData[carEntity]; // bus occupied splines
                    var firstOccupiedSplineComponentData = getSplineComponentDataFromEntity[occupiedSplines[0].spline];
                    firstOccupiedSplineComponentData.isOccupied = false; // free the rearmost occupied spline
                    entityManager.SetComponentData(occupiedSplines[0].spline, firstOccupiedSplineComponentData);

                    occupiedSplines.RemoveAt(0); // dequeue
                    occupiedSplines.Add(new SplineBufferComponentData { spline = frontSpline }); // enqueue

                    carComponentData.splineStart = splineEnd;
                    carComponentData.splineEnd = occupiedSplines[1].spline;
                    carComponentData.SplineId++;

                    carComponentData.splineReachedAtTime = elapsedTime;
                }
                else // lerp
                {
                    var localToWorldSplineStart = getLocalToWorldComponentData[carComponentData.splineStart];
                    var localToWorldSplineEnd = getLocalToWorldComponentData[carComponentData.splineEnd];
                    var journeyLength = UnityEngine.Vector3.Distance(localToWorldSplineStart.Position, localToWorldSplineEnd.Position);
                    var distCovered = (elapsedTime - carComponentData.splineReachedAtTime) * carComponentData.maxSpeed * 100;
                    var fractionOfJourney = (float)distCovered / journeyLength;

                    translation.Value = UnityEngine.Vector3.Lerp(localToWorldSplineStart.Position, localToWorldSplineEnd.Position, fractionOfJourney);
                }

            }

        }).Run();

        ecb.Playback(entityManager);
        ecb.Dispose();
    }

    static Entity getNextNode(EntityManager entityManager, BufferFromEntity<SplineBufferComponentData> getSplineBufferComponentData, ComponentDataFromEntity<TrackComponentData> getTrackComponentData, ComponentDataFromEntity<SplineComponentData> getSplineComponentDataFromEntity, CarComponentData carComponentData, Entity carEntity)
    {
        Entity entityToReturn = Entity.Null;

        var splineBufferComponentData = getSplineBufferComponentData[carComponentData.Track];
        if (splineBufferComponentData.Length > carComponentData.SplineId) // splineId may refer to a non-existing spline (off-by-one error)
        {
            var spline = splineBufferComponentData[carComponentData.SplineId].spline;
            entityToReturn = spline;
        }

        var splineToReturnComponentData = getSplineComponentDataFromEntity[entityToReturn];

        if (splineToReturnComponentData.isOccupied && !splineToReturnComponentData.isLast && carComponentData.isOnStreet)
        {
            // Try overtake on left, if there is a left track in the same direction
            var myTrackComponentData = getTrackComponentData[carComponentData.Track];
            var leftTrack = myTrackComponentData.leftTrack;
            if(leftTrack != Entity.Null)
            {
                splineBufferComponentData = getSplineBufferComponentData[leftTrack];
                if (splineBufferComponentData.Length > carComponentData.SplineId) // splineId may refer to a non-existing spline (off-by-one error)
                {
                    var spline = splineBufferComponentData[carComponentData.SplineId].spline;
                    var thisSplineComponentData = getSplineComponentDataFromEntity[spline];
                    if (!thisSplineComponentData.isOccupied)
                    {
                        carComponentData.Track = leftTrack;
                        entityManager.SetComponentData<CarComponentData>(carEntity, carComponentData);
                        entityToReturn = spline;
                    }
                }
            }
        }
        
        return entityToReturn;
    }
}

