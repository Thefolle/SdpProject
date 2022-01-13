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
                var splines = getChildComponentData[carComponentData.Track];
                foreach (var spline in splines)
                {
                    mySplineEndComponentData = getSplineComponentDataFromEntity[spline.Value];

                    if (mySplineEndComponentData.id == 0)
                    {
                        carComponentData.splineEnd = spline.Value;
                        break;
                    }
                }
                carComponentData.needToUpdatedPath = false;
            }

            if (!mySplineEndComponentData.isOccupied )
            {
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

                    mySplineEndComponentData.isOccupied = true;
                    ecb.SetComponent(carComponentData.splineEnd, mySplineEndComponentData);

                    mySplineStartComponentData.isOccupied = false;
                    ecb.SetComponent(carComponentData.splineStart, mySplineStartComponentData);

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
                        var splines = getChildComponentData[carComponentData.Track];
                        foreach (var spline in splines)
                        {
                            mySplineEndComponentData = getSplineComponentDataFromEntity[spline.Value];

                            if (mySplineEndComponentData.id == carComponentData.SplineId)
                            {
                                carComponentData.splineEnd = spline.Value;
                                break;
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
}

