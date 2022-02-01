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

        if (World.GetExistingSystem<EndInitializationBarrierSystem>().Enabled) return;

        var getSplineComponentDataFromEntity = GetComponentDataFromEntity<SplineComponentData>();
        var getSplineBufferComponentData = GetBufferFromEntity<SplineBufferComponentData>();
        var getTrackComponentData = GetComponentDataFromEntity<TrackComponentData>();
        var getLocalToWorldComponentData = GetComponentDataFromEntity<LocalToWorld>();
        var getSemaphoreStateComponentData = GetComponentDataFromEntity<SemaphoreStateComponentData>();

        EntityManager entityManager = World.EntityManager;
        var ecb = new EntityCommandBuffer(Allocator.TempJob);

        Entities.ForEach((ref Translation translation, ref CarComponentData carComponentData, ref Rotation rotation, ref DynamicBuffer<SplineBufferComponentData> occupiedSplines, ref PollComponentData pollComponentData, in Entity carEntity) =>
        {
            /* Check if it is time to poll or not*/
            if (pollComponentData.Poll > 0)
            {
                pollComponentData.Poll = (pollComponentData.Poll + 1) % 64;
                return;
            }

            int NumberOfOccupiedSplines = getSplineBufferComponentData[carEntity].Length; // 1 for cars, 3 for buses as of now

            if (carComponentData.HasReachedDestination)
            {
                foreach (var spline in occupiedSplines)
                {
                    var splineComponentData = getSplineComponentDataFromEntity[spline.spline];
                    splineComponentData.isOccupied = false;
                    entityManager.SetComponentData(spline.spline, splineComponentData);
                }
                entityManager.SetComponentData(carEntity, new AskToDespawnComponentData { Asked = true });
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
                var splineEndComponentData = getSplineComponentDataFromEntity[splineEnd];
                if (splineEndComponentData.isDespawner) // for instance, a parking entrance
                {
                    carComponentData.HasReachedDestination = true; // trigger the if above on next frame

                    return;
                }

                /* Adjust rotation */
                if (carComponentData.isOnParkingArea || !carComponentData.isOnStreet)
                {
                    rotation.Value = getLocalToWorldComponentData[splineEnd].Rotation;
                }
                else if (carComponentData.isOnStreet && splineEndComponentData.id == 0)
                {
                    rotation.Value = quaternion.RotateY(math.radians(splineEndComponentData.degreeRotationStreet));
                }

                var splineBuffer = getSplineBufferComponentData[carComponentData.Track]; // beware that the track is updated as soon as the front spline is free, not when the center of the vehicle reaches the new track
                int frontSplineId; // the id of the spline in front of the vehicle

                if (carComponentData.needToUpdatedPath && carComponentData.isPathUpdated)
                {
                    frontSplineId = 0;
                    var firstSplineComponentData = getSplineComponentDataFromEntity[splineBuffer[frontSplineId].spline];
                    if (firstSplineComponentData.isOccupied) return; // retry afterwards in this same if block; here must be sure that the next spline will be occupied
                    if (getSemaphoreStateComponentData.HasComponent(splineBuffer[frontSplineId].spline))
                    {
                        var semaphoreStateComponentData = getSemaphoreStateComponentData[splineBuffer[frontSplineId].spline];
                        if (!semaphoreStateComponentData.IsGreen) return;
                    }
                    /* if the next spline can be occupied, then update the car state */
                    carComponentData.needToUpdatedPath = false;
                    carComponentData.SplineId = -(NumberOfOccupiedSplines / 2 + 1); // prepare for next frames, don't use in this frame
                }
                else
                {
                    frontSplineId = carComponentData.SplineId + NumberOfOccupiedSplines / 2 + 1; // take the first spline at the front of the vehicle
                }

                // if the front spline is the one after the last one (that is, it does not exist)
                if (frontSplineId == splineBuffer.Length)
                {
                    carComponentData.needToUpdatedPath = true;
                    carComponentData.isPathUpdated = false;
                    carComponentData.isOnStreet = !carComponentData.isOnStreet;
                    return;
                }

                Entity frontSpline;
                if (carComponentData.IsBus)
                {
                    frontSpline = splineBuffer[frontSplineId].spline;
                } else
                {
                    frontSpline = getNextNode(entityManager, getSplineBufferComponentData, getTrackComponentData, getSplineComponentDataFromEntity, carComponentData, carEntity, frontSplineId);
                }

                var frontSplineComponentData = getSplineComponentDataFromEntity[frontSpline];

                if (frontSplineComponentData.isOccupied)
                {
                    pollComponentData.Poll++;
                    return; // retry on next frame
                }

                if (getSemaphoreStateComponentData.HasComponent(frontSpline))
                {
                    var semaphoreStateComponentData = getSemaphoreStateComponentData[frontSpline];
                    if (!semaphoreStateComponentData.IsGreen) return;
                }

                frontSplineComponentData.isOccupied = true;
                entityManager.SetComponentData(frontSpline, frontSplineComponentData);

                var firstOccupiedSplineComponentData = getSplineComponentDataFromEntity[occupiedSplines[0].spline];
                firstOccupiedSplineComponentData.isOccupied = false; // free the rearmost occupied spline
                entityManager.SetComponentData(occupiedSplines[0].spline, firstOccupiedSplineComponentData);

                occupiedSplines.RemoveAt(0); // dequeue
                occupiedSplines.Add(new SplineBufferComponentData { spline = frontSpline }); // enqueue

                carComponentData.splineStart = splineEnd;
                carComponentData.splineEnd = occupiedSplines[NumberOfOccupiedSplines / 2].spline;
                carComponentData.SplineId++;

                carComponentData.splineReachedAtTime = elapsedTime;

                splineEndComponentData = getSplineComponentDataFromEntity[carComponentData.splineEnd];
                if (splineEndComponentData.isParkingEntrance) carComponentData.isOnParkingArea = true;
                else if (splineEndComponentData.isParkingExit) carComponentData.isOnParkingArea = false;
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

        }).Run();

        ecb.Playback(entityManager);
        ecb.Dispose();

    }

    static Entity getNextNode(EntityManager entityManager, BufferFromEntity<SplineBufferComponentData> getSplineBufferComponentData, ComponentDataFromEntity<TrackComponentData> getTrackComponentData, ComponentDataFromEntity<SplineComponentData> getSplineComponentDataFromEntity, CarComponentData carComponentData, Entity carEntity, int frontSplineId)
    {
        Entity entityToReturn = Entity.Null;

        var splineBufferComponentData = getSplineBufferComponentData[carComponentData.Track];
        if (splineBufferComponentData.Length > frontSplineId) // splineId may refer to a non-existing spline (off-by-one error)
        {
            var spline = splineBufferComponentData[frontSplineId].spline;
            entityToReturn = spline; // the default spline to return is the next one in the current track
        }

        var splineToReturnComponentData = getSplineComponentDataFromEntity[entityToReturn];

        /* If the front spline is occupied, check if its left spline is free */
        if (splineToReturnComponentData.isOccupied && !splineToReturnComponentData.isLast && carComponentData.isOnStreet)
        {
            // Try overtake on left, if there is a left track in the same direction
            var myTrackComponentData = getTrackComponentData[carComponentData.Track];
            var leftTrack = myTrackComponentData.leftTrack;
            if (leftTrack != Entity.Null)
            {
                splineBufferComponentData = getSplineBufferComponentData[leftTrack];
                if (splineBufferComponentData.Length > frontSplineId) // splineId may refer to a non-existing spline (off-by-one error)
                {
                    var spline = splineBufferComponentData[frontSplineId].spline;
                    var thisSplineComponentData = getSplineComponentDataFromEntity[spline];
                    if (!thisSplineComponentData.isOccupied)
                    {
                        carComponentData.Track = leftTrack;
                        entityManager.SetComponentData(carEntity, carComponentData);
                        entityToReturn = spline;
                    }
                }
            }
        }

        return entityToReturn;
    }
}

