using Unity.Entities;
using Unity.Physics;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics.Systems;
using static UnityEngine.Debug;
using Unity.Collections;

public class VehicleMovementSystem : SystemBase
{
    private const int EditorFactor = 2;

    /// <summary>
    /// <para>The degree at which cars stop steering to approach toward the track. This parameter is an indicator of the convergence speed of a car to a track.
    /// It is measured in degrees.</para>
    /// </summary>
    private const float steeringDegree = 60;

    /// <summary>
    /// <para>The algorithm exploits this parameter to determine the behaviour of a car w.r.t. its track.</para>
    /// <para>Greater values imply a better convergence in straight lanes, which is worse during bends. Cars may lose their track during bends for big values. Moreover, you should take into account both the width of the car and the width of the lane.</para>
    /// <para>Lower values imply a better convergence in bends, which is slower in straight lanes.</para>
    /// </summary>
    private const float thresholdDistance = 0.3f;

    /// <summary>
    /// <para>This parameter establishes the range, expressed as distance from the track, in which the car can be considered in the track.</para>
    /// <para>Within the range, the movement algorithm is deactivated and the car proceeds forward. Outside the range, the algorithm works normally.</para>
    /// </summary>
    private const float NegligibleDistance = 0.1f;

    private const float LaneWideness = 2.5f * EditorFactor;

    protected override void OnUpdate()
    {
        float deltaTime = Time.DeltaTime;
        double elapsedTime = Time.ElapsedTime;
        if (elapsedTime < 2) return;

        PhysicsWorld physicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>().PhysicsWorld;
        EntityManager entityManager = World.EntityManager;
        var getTrackComponentDataFromEntity = GetComponentDataFromEntity<TrackComponentData>();
        var getParentComponentDataFromEntity = GetComponentDataFromEntity<Parent>();
        var getLaneComponentDataFromEntity = GetComponentDataFromEntity<LaneComponentData>();
        var getCarComponentDataFromEntity = GetComponentDataFromEntity<CarComponentData>();

        Entities.ForEach((Entity carEntity, LocalToWorld localToWorld, ref PhysicsVelocity physicsVelocity, ref CarComponentData carCompData , in CarComponentData carComponentData) =>
        {
            /* Initialize data */
            float factor = 0;
            float linearFactor = 1;
            var raycastInputRight = new RaycastInput
            {
                Start = localToWorld.Position,
                End = localToWorld.Position + 20 * localToWorld.Right,
                Filter = CollisionFilter.Default
            };
            UnityEngine.Debug.DrawLine(localToWorld.Position, localToWorld.Position + 20 * localToWorld.Right, UnityEngine.Color.green, 0);

            var raycastInputLeft = new RaycastInput
            {
                Start = localToWorld.Position,
                End = localToWorld.Position + 20 * -localToWorld.Right,
                Filter = CollisionFilter.Default
            };
            UnityEngine.Debug.DrawLine(localToWorld.Position, localToWorld.Position + 20 * -localToWorld.Right, UnityEngine.Color.green, 0);

            var rightHits = new NativeList<RaycastHit>(20, Allocator.TempJob);
            var leftHits = new NativeList<RaycastHit>(20, Allocator.TempJob);

            /* now pick the hit with the closest track whose id equals the car's assigned track id*/
            bool isRightHit = false; // the hit can be rightwards or leftwards
            float distance = 0;
            bool isTrackHitFound = false; // flag that tells whether at least one admissible hit has been found
            RaycastHit hit = default;
            var forward = math.normalize(localToWorld.Forward);

            /* Assume that there exists only one admissible hit in the world with the given id*/
            if (physicsWorld.CastRay(raycastInputLeft, ref leftHits) && leftHits.Length > 1)
            {
                foreach (var it in leftHits)
                {
                    if (getTrackComponentDataFromEntity.HasComponent(it.Entity))
                    {
                        var trackComponentData = getTrackComponentDataFromEntity[it.Entity];
                        if (it.Entity.Index == carComponentData.TrackId)
                        {
                            hit = it;

                            isRightHit = false;
                            // Don't compute the distance with math.distance, since only the projection along the surface normal is relevant
                            distance = math.abs(math.dot(localToWorld.Position, hit.SurfaceNormal) - math.dot(hit.Position, hit.SurfaceNormal));
                            isTrackHitFound = true;

                            // hit found, no need to proceed
                            break;
                        }
                    }
                }
            }
            if (!isTrackHitFound && physicsWorld.CastRay(raycastInputRight, ref rightHits) && rightHits.Length > 1)
            {
                foreach (var it in rightHits)
                {
                    if (getTrackComponentDataFromEntity.HasComponent(it.Entity))
                    {
                        var trackComponentData = getTrackComponentDataFromEntity[it.Entity];
                        if (it.Entity.Index == carComponentData.TrackId)
                        {
                            hit = it;

                            distance = math.abs(math.dot(localToWorld.Position, hit.SurfaceNormal) - math.dot(hit.Position, hit.SurfaceNormal));
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
                /* This scenario can take place if the track is out-of-range or the car is turning at the beginning or at the end of a lane. */
                LogError("The car with id " + carEntity.Index + " cannot find any track with id " + carComponentData.TrackId + " to follow.");
                factor = 0;
                linearFactor = 1;
            } else
            {
                if (distance < NegligibleDistance)
                {
                    // The car's distance from the track is negligible
                    factor = 0;
                } else if (math.dot(forward, hit.SurfaceNormal) < 0 && distance < thresholdDistance)
                {
                    /* The car is approaching and is near to the track */
                    factor = 0;
                }
                else if (math.dot(forward, hit.SurfaceNormal) < -math.cos(math.radians(steeringDegree)) && distance >= thresholdDistance)
                {
                    /* The car is approaching but is distant from the track */
                    factor = 0;
                }
                else if (math.dot(forward, hit.SurfaceNormal) > -math.cos(math.radians(steeringDegree)) && distance >= thresholdDistance)
                {
                    /* The car is going away from the track */
                    factor = 1 * (isRightHit ? +1 : -1);
                    linearFactor = 0.3f;
                }
                else
                {
                    /* The car is near and is going straight or it is leaving; let's turn it */
                    /* Here soften the car trajectory so that it is a softened synusoid*/
                    factor = 1 * (isRightHit ? +1 : -1) / (1 + distance);
                }

                /*Log("Car position is: " + localToWorld.Position);
                Log("Track position is: " + hit.Position);
                Log("Hit distance is: " + distance);
                Log(math.dot(forward, hit.SurfaceNormal));
                Log("The current track for the car has id " + hit.Entity.Index);*/
            }
            rightHits.Dispose();
            leftHits.Dispose();

            physicsVelocity.Angular.y = carComponentData.AngularSpeed * factor * deltaTime;

            var tmp = physicsVelocity.Linear.y;
            physicsVelocity.Linear = localToWorld.Forward * carComponentData.Speed * linearFactor * deltaTime;
            // Neglect the y speed
            physicsVelocity.Linear.y = tmp;
        }).Run();
    }
}

