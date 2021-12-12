using Unity.Entities;
using Unity.Physics;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics.Systems;
using static UnityEngine.Debug;
using Unity.Collections;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public class VehicleMovementSystem : SystemBase
{
    private const int EditorFactor = 2;

    /// <summary>
    /// <para>The degree at which cars stop steering to approach toward the track. This parameter is an indicator of the convergence speed of a car to a track.
    /// It is measured in degrees.</para>
    /// </summary>
    private readonly int SteeringDegree = (int)(math.cos(math.radians(75)) * 100);

    /// <summary>
    /// <para>The algorithm exploits this parameter to determine the behaviour of a car w.r.t. its track.</para>
    /// <para>Greater values imply a better convergence in straight lanes, which is worse during bends. Cars may lose their track during bends for big values. Moreover, you should take into account both the width of the car and the width of the lane.</para>
    /// <para>Lower values imply a better convergence in bends, which is slower in straight lanes.</para>
    /// </summary>
    private const int thresholdDistance = 1;

    /// <summary>
    /// <para>This parameter establishes the range, expressed as distance from the track, in which the car can be considered in the track.</para>
    /// <para>Within the range, the movement algorithm is deactivated and the car proceeds forward. Outside the range, the algorithm works normally.</para>
    /// </summary>
    private const int NegligibleDistance = 1;

    /// <summary>
    /// <para>The constant is equal to cos(80) * 100</para>
    /// </summary>
    private readonly int Cos80 = (int)(math.cos(math.radians(80)) * 100);

    /// <summary>
    /// <para>The constant is equal to cos(60) * 100</para>
    /// </summary>
    private readonly int Cos60 = (int)(math.cos(math.radians(60)) * 100);



    protected override void OnUpdate()
    {
        float deltaTime = Time.fixedDeltaTime;
        double elapsedTime = Time.ElapsedTime;
        if (elapsedTime < 2) return;

        PhysicsWorld physicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>().PhysicsWorld;
        var getTrackComponentDataFromEntity = GetComponentDataFromEntity<TrackComponentData>();
        var getParentComponentDataFromEntity = GetComponentDataFromEntity<Parent>();

        /* capture local variables */
        var steeringDegree = SteeringDegree;
        var cos60 = Cos60;
        var cos80 = Cos80;

        var trackFilter = new CollisionFilter
        {
            BelongsTo = 1 << 0,
            CollidesWith = 1 << 0,
            GroupIndex = 0
        };
        var rightHits = new NativeList<RaycastHit>(20, Allocator.TempJob);
        var leftHits = new NativeList<RaycastHit>(20, Allocator.TempJob);

        Entities.ForEach((ref PhysicsVelocity physicsVelocity, ref CarComponentData carComponentData, ref Rotation rotation, in Entity carEntity, in LocalToWorld localToWorld) =>
        {
            if (carComponentData.HasJustSpawned) return;

            /* Initialize data */
            int angularFactor = 0;
            int linearFactor = 1;
            
            var raycastInputRight = new RaycastInput
            {
                Start = localToWorld.Position,
                End = localToWorld.Position + 20 * localToWorld.Right,
                Filter = trackFilter
            };
            //DrawLine(raycastInputRight.Start, raycastInputRight.End, UnityEngine.Color.green, 0);
            var raycastInputLeft = new RaycastInput
            {
                Start = localToWorld.Position,
                End = localToWorld.Position + 20 * -localToWorld.Right,
                Filter = trackFilter
            };
            //DrawLine(raycastInputLeft.Start, raycastInputLeft.End, UnityEngine.Color.green, 0);

            /* now pick the hit with the closest track whose id equals the car's assigned track id*/
            bool isRightHit = false; // the hit can be rightwards or leftwards
            int distance = 0;
            bool isTrackHitFound = false; // flag that tells whether at least one admissible hit has been found
            RaycastHit hit = default;
            var forward = math.normalize(localToWorld.Forward);

            /* Assume that there exists only one admissible hit in the world with the given id*/
            if (physicsWorld.CastRay(raycastInputLeft, ref leftHits))
            {
                foreach (var it in leftHits)
                {
                    if (getTrackComponentDataFromEntity.HasComponent(it.Entity))
                    {
                        if (it.Entity.Index == carComponentData.Track.Index)
                        {
                            hit = it;

                            isRightHit = false;
                            // Don't compute the distance with math.distance, since only the projection along the surface normal is relevant
                            distance = (int)(100 * math.abs(math.dot(localToWorld.Position, hit.SurfaceNormal) - math.dot(hit.Position, hit.SurfaceNormal)));
                            isTrackHitFound = true;

                            // hit found, no need to proceed
                            break;
                        }
                    }
                }
            }
            if (!isTrackHitFound && physicsWorld.CastRay(raycastInputRight, ref rightHits))
            {
                foreach (var it in rightHits)
                {
                    if (getTrackComponentDataFromEntity.HasComponent(it.Entity))
                    {
                        if (it.Entity.Index == carComponentData.Track.Index)
                        {
                            hit = it;

                            distance = (int)(100 * math.abs(math.dot(localToWorld.Position, hit.SurfaceNormal) - math.dot(hit.Position, hit.SurfaceNormal)));
                            isRightHit = true;
                            isTrackHitFound = true;

                            // hit found, no need to proceed
                            break;
                        }
                    }
                }
            }
                
            if (isTrackHitFound == false)
            {
                /* This scenario can take place if:
                 * 
                 * - the track is out-of-range or non-existing;
                 * - the car has lost the track for some frames: for instance, when it is turning
                 * at the beginning or at the end of a lane.
                 * 
                 * The second scenario is evaluated as acceptable since all the observed configurations have led the car
                 * to converge toward its assigned track id. See docs for further details.
                 */

                angularFactor = 0;
                linearFactor = 1;
            } else
            {
                int gap = (int) (100 * math.dot(forward, hit.SurfaceNormal));

                if (distance < 20 * NegligibleDistance && math.abs(gap) < cos80 && carComponentData.vehicleIsOn == VehicleIsOn.Street)
                {
                    // The car's distance from the track is negligible
                    LogFormat("1 ({0}), distance: {1}, gap: {2}, surface normal: {3}", carEntity.Index, distance, gap, hit.SurfaceNormal);
                    int angle = 0;
                    if (hit.SurfaceNormal.x > 0.9 && !isRightHit || hit.SurfaceNormal.x < -0.9 && isRightHit) angle = 0;
                    else if (hit.SurfaceNormal.z > 0.9 && isRightHit || hit.SurfaceNormal.z < -0.9 && !isRightHit) angle = 90;
                    else if (hit.SurfaceNormal.x < -0.9 && !isRightHit || hit.SurfaceNormal.x > 0.9 && isRightHit) angle = 180;
                    else if (hit.SurfaceNormal.z < -0.9 && isRightHit || hit.SurfaceNormal.z > 0.9 && !isRightHit) angle = 270;
                    rotation.Value = quaternion.RotateY(math.radians(angle));
                    angularFactor = 0;
                }
                else if (gap < 0 && distance < 50 * thresholdDistance)
                {
                    /* The car is approaching and is near to the track */
                    /* Here soften the car trajectory so that it is a softened synusoid: the effect is achieved by
                     * using a lowered angular factor w.r.t. the scenario where the car is near but it is leaving
                     */
                    LogFormat("2 ({0}), distance: {1}, gap: {2}", carEntity.Index, distance, gap);
                    angularFactor = 1 * (!isRightHit ? +1 : -1);
                }
                else if (gap < -steeringDegree && distance >= 50 * thresholdDistance)
                {
                    /* The car is approaching but is distant from the track */
                    LogFormat("3 ({0}), distance: {1}, gap: {2}", carEntity.Index, distance, gap);
                    angularFactor = 0;
                }
                else if (gap >= -steeringDegree && distance >= 50 * thresholdDistance)
                {
                    /* The car is going away and it is distant from the track */
                    LogFormat("4 ({0}), distance: {1}, gap: {2}", carEntity.Index, distance, gap);
                    angularFactor = 3 * (isRightHit ? +1 : -1);
                }
                else if (gap >= 0 && distance < 50 * thresholdDistance)
                {
                    LogFormat("5 ({0}), distance: {1}, gap: {2}", carEntity.Index, distance, gap);
                    /* The car is near and is going straight or it is leaving; let's turn it */
                    angularFactor = 3 * (isRightHit ? +1 : -1);
                } else
                {
                    angularFactor = 0;
                    linearFactor = 1;
                    LogErrorFormat("The car with id {0} reached an unpredicted state (gap: {1}, distance: {2})", carEntity.Index, gap, distance);
                }

                if ((carComponentData.vehicleIsOn == VehicleIsOn.Cross || carComponentData.vehicleIsOn == VehicleIsOn.PassingFromStreetToCross || carComponentData.vehicleIsOn == VehicleIsOn.PassingFromCrossToStreet) && gap > cos60)
                {
                    linearFactor = 0;
                }

                //Log("Car position is: " + localToWorld.Position);
                //Log("Track position is: " + hit.Position);
                //Log("Hit distance is: " + distance);
                //Log(math.dot(forward, hit.SurfaceNormal));
                //Log("The current track for the car has id " + hit.Entity.Index);
            }

            physicsVelocity.Angular.y = carComponentData.AngularSpeed * angularFactor * deltaTime;

            var tmp = physicsVelocity.Linear.y;
            physicsVelocity.Linear = forward * carComponentData.Speed * linearFactor * deltaTime;
            // Neglect the y speed
            physicsVelocity.Linear.y = tmp;
        }).Run();

        rightHits.Dispose();
        leftHits.Dispose();
    }
}

