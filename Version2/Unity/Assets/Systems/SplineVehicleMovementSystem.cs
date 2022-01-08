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
            if(carComponentData.needToUpdatedPath)
            {
                carComponentData.SplineId = -1;
                carComponentData.needToUpdatedPath = false;
            }

            var mySplineEnd = Entity.Null;

            var mySplineEndComponentData = new SplineComponentData();

            var splines = getChildComponentData[carComponentData.Track];
            foreach (var spline in splines)
            {
                mySplineEndComponentData = getSplineComponentDataFromEntity[spline.Value];

                if (mySplineEndComponentData.id == (carComponentData.SplineId + 1))
                {
                    mySplineEnd = spline.Value;
                    break;
                }
            }

            if (mySplineEnd != Entity.Null && !mySplineEndComponentData.isOccupied )
            {
                //LogErrorFormat("{0}", carComponentData.SplineId);
                var localToWorldSplineStart = getLocalToWorldComponentDataFromEntity[carComponentData.splineStart];
                var localToWorldSplineEnd = getLocalToWorldComponentDataFromEntity[mySplineEnd];
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
                    ecb.SetComponent(mySplineEnd, mySplineEndComponentData);

                    mySplineStartComponentData.isOccupied = false;
                    ecb.SetComponent(carComponentData.splineStart, mySplineStartComponentData);

                    carComponentData.splineStart = mySplineEnd;
                    if (mySplineEndComponentData.isLast)
                    {
                        //LogError("Reached last: " + carEntity.Index);
                        // TOGGLE AND UPDATE TRACK
                        carComponentData.isOnStreet = !carComponentData.isOnStreet;
                        carComponentData.isPathUpdated = false;
                        carComponentData.needToUpdatedPath = true;
                    }
                }
            }
            else
            {
                carComponentData.splineReachedAtTime = elapsedTime;
            }
            // physicsVelocity.Linear = math.normalize(localToWorld.Forward) * carComponentData.Speed / fixedDeltaTime;

        }).Run();

        ecb.Playback(entityManager);
        ecb.Dispose();
    }
}

