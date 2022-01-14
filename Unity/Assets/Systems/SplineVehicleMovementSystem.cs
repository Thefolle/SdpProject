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
                //if (!carComponentData.isOnStreet) { 
                //    var splines = getChildComponentData[carComponentData.Track];
                //    foreach (var spline in splines)
                //    {
                //        mySplineEndComponentData = getSplineComponentDataFromEntity[spline.Value];

                //        if (mySplineEndComponentData.id == 0)
                //        {
                //            carComponentData.splineEnd = spline.Value;
                //            break;
                //        }
                //    }
                //}
                //else // splineBuffer is available only for streets
                //{
                    var splineBufferComponentData = getSplineBufferComponentData[carComponentData.Track];
                    if (splineBufferComponentData.Length > carComponentData.SplineId) // splineId may refer to a non-existing spline (off-by-one error)
                    {
                        var spline = splineBufferComponentData[carComponentData.SplineId].spline;
                        carComponentData.splineEnd = spline;
                    }
                //}
                carComponentData.needToUpdatedPath = false;
            }

            if(!mySplineEndComponentData.isOccupied)
            //if (!mySplineEndComponentData.isOccupied || (mySplineEndComponentData.isOccupied && carComponentData.isOccupying))
            {
                //carComponentData.isOccupying = true;

                var localToWorldSplineStart = getLocalToWorldComponentDataFromEntity[carComponentData.splineStart];
                var localToWorldSplineEnd = getLocalToWorldComponentDataFromEntity[carComponentData.splineEnd];
                var journeyLength = UnityEngine.Vector3.Distance(localToWorldSplineStart.Position, localToWorldSplineEnd.Position);
                var distCovered = (elapsedTime - carComponentData.splineReachedAtTime) * carComponentData.maxSpeed * 100;
                var fractionOfJourney = (float)distCovered / journeyLength;

                translation.Value = UnityEngine.Vector3.Lerp(localToWorldSplineStart.Position, localToWorldSplineEnd.Position, fractionOfJourney);
                //if (!carComponentData.isOnStreet) rotation.Value = UnityEngine.Quaternion.Lerp(localToWorldSplineStart.Rotation, localToWorldSplineEnd.Rotation, fractionOfJourney);

                if (math.all(localToWorldSplineEnd.Position == translation.Value))
                {
                    if (!carComponentData.isOnStreet) rotation.Value = localToWorldSplineEnd.Rotation;
                    carComponentData.SplineId = carComponentData.SplineId + 1;
                    carComponentData.splineReachedAtTime = elapsedTime;

                    mySplineStartComponentData.isOccupied = false;
                    ecb.SetComponent(carComponentData.splineStart, mySplineStartComponentData);

                    mySplineEndComponentData.isOccupied = true;
                    ecb.SetComponent(carComponentData.splineEnd, mySplineEndComponentData);

                    //carComponentData.isOccupying = false;

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
                        // Overtake algorithm is too heavy in its current implementation
                        bool overtakeEnabled = false;
                        if (overtakeEnabled)
                        {
                            carComponentData.splineEnd = getNextNode(entityManager, getChildComponentData, getSplineComponentDataFromEntity, getParentComponentData, carComponentData, ecb, carEntity);
                        }
                        else
                        {
                            //if (!carComponentData.isOnStreet)
                            //{
                            //    var splines = getChildComponentData[carComponentData.Track];
                            //    foreach (var spline in splines)
                            //    {
                            //        mySplineEndComponentData = getSplineComponentDataFromEntity[spline.Value];

                            //        if (mySplineEndComponentData.id == carComponentData.SplineId)
                            //        {
                            //            carComponentData.splineEnd = spline.Value;
                            //            break;
                            //        }
                            //    }
                            //}
                            //else // splineBuffer is available only for streets
                            //{
                                var splineBufferComponentData = getSplineBufferComponentData[carComponentData.Track];
                                if (splineBufferComponentData.Length > carComponentData.SplineId) // splineId may refer to a non-existing spline (off-by-one error)
                                {
                                    var spline = splineBufferComponentData[carComponentData.SplineId].spline;
                                    carComponentData.splineEnd = spline;
                                }
                            //}
                            
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

    static Entity getNextNode(EntityManager entityManager, BufferFromEntity<Child> getChildComponentData, ComponentDataFromEntity<SplineComponentData> getSplineComponentDataFromEntity, ComponentDataFromEntity<Parent> getParentComponentData, CarComponentData carComponentData, EntityCommandBuffer ecb, Entity carEntity)
    {
        Entity entityToReturn = Entity.Null;
        var splines = getChildComponentData[carComponentData.Track];
        SplineComponentData mySplineEndComponentData;
        mySplineEndComponentData.isOccupied = false;
        mySplineEndComponentData.isLast = false;

        foreach (var spline in splines)
        {
            mySplineEndComponentData = getSplineComponentDataFromEntity[spline.Value];

            if (mySplineEndComponentData.id == carComponentData.SplineId)
            {
                entityToReturn = spline.Value;
                break;
            }
        }

        if (mySplineEndComponentData.isOccupied && !mySplineEndComponentData.isLast && carComponentData.isOnStreet)
        {
            // Try overtake on left
            var lane = getParentComponentData[carComponentData.Track].Value;
            var laneName = entityManager.GetName(lane).ToString();
            if (laneName.Contains("Lane"))
            {
                var laneDirection = laneName.Substring(0, laneName.IndexOf('-'));
                var laneNumberInDirection = System.Int32.Parse(laneName.Substring(laneName.LastIndexOf('-') + 1));
                if (laneNumberInDirection != 0) // Avoid to look at your left, you're at the left most lane
                {
                    var currentStreet = getParentComponentData[lane].Value;
                    foreach (var laneBrother in getChildComponentData[currentStreet])
                    {
                        var thisLaneName = entityManager.GetName(laneBrother.Value);
                        if (thisLaneName.Contains("Lane"))
                        {
                            var thisLaneDirection = thisLaneName.Substring(0, thisLaneName.IndexOf('-'));
                            var thisLaneNumberInDirection = System.Int32.Parse(thisLaneName.Substring(thisLaneName.LastIndexOf('-') + 1));
                            if (laneDirection.Equals(thisLaneDirection) && laneNumberInDirection - 1 == thisLaneNumberInDirection)
                            {
                                var leftTrack = getChildComponentData[laneBrother.Value][0].Value;
                                var leftTrackSplines = getChildComponentData[leftTrack];
                                foreach (var spline in leftTrackSplines)
                                {
                                    mySplineEndComponentData = getSplineComponentDataFromEntity[spline.Value];

                                    if (mySplineEndComponentData.id == carComponentData.SplineId)
                                    {
                                        if (!mySplineEndComponentData.isOccupied) // If it is occupied, just go forward
                                        {
                                            entityToReturn = spline.Value;
                                            carComponentData.Track = leftTrack;
                                            //ecb.SetComponent(carEntity, carComponentData);
                                            entityManager.SetComponentData<CarComponentData>(carEntity, carComponentData);
                                        }
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        return entityToReturn;
    }
}

