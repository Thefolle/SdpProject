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

        EntityManager entityManager = World.EntityManager;
        var ecb = new EntityCommandBuffer(Allocator.TempJob);

        Entities.ForEach((ref Translation translation, ref CarComponentData carComponentData, ref Rotation rotation, in Entity carEntity) =>
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
                        if (!carComponentData.isOnStreet) rotation.Value = localToWorldSplineEnd.Rotation;
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

