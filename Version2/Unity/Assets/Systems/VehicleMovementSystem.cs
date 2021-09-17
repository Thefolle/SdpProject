using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Physics;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics.Systems;
using static UnityEngine.Debug;
using Unity.Collections;

public class VehicleMovementSystem : SystemBase
{
    /// <summary>
    /// <para>The degree at which cars stop steering to approach toward the track. This parameter is an indicator of the convergence speed of a car to a track.
    /// It is measured in degrees.</para>
    /// </summary>
    private const float steeringDegree = 20;

    /// <summary>
    /// <para>The algorithm exploits this parameter to determine the behaviour of a car w.r.t. its track.</para>
    /// <para>Greater values imply a better convergence in straight lanes, which is worse during bends. Cars may lose their track during bends for big values. Moreover, you should take into account both the width of the car and the width of the lane.</para>
    /// <para>Lower values imply a better convergence in bends, which is slower in straight lanes.</para>
    /// </summary>
    private const float thresholdDistance = 0.3f;

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

        Entities.ForEach((Entity carEntity, CarComponentData carComponentData, LocalToWorld localToWorld, ref PhysicsVelocity physicsVelocity) =>
        {
            /* Initialize data */
            float factor = 0;
            var raycastInputRight = new RaycastInput
            {
                Start = localToWorld.Position,
                End = localToWorld.Position + 20 * localToWorld.Right,
                Filter = CollisionFilter.Default
            };
            var raycastInputLeft = new RaycastInput
            {
                Start = localToWorld.Position,
                End = localToWorld.Position + 20 * -localToWorld.Right,
                Filter = CollisionFilter.Default
            };
            var rightHits = new NativeList<RaycastHit>(20, Allocator.TempJob);
            var leftHits = new NativeList<RaycastHit>(20, Allocator.TempJob);

            bool isTrackHitFound = false;
            if (physicsWorld.CastRay(raycastInputRight, ref rightHits) && rightHits.Length > 1)
            {
                RaycastHit hit = default;
                foreach (var it in rightHits) {
                    if (getTrackComponentDataFromEntity.HasComponent(it.Entity))
                    {
                        var parent = getParentComponentDataFromEntity[it.Entity].Value;
                        if (getLaneComponentDataFromEntity.HasComponent(parent))
                        {
                            var laneComponentData = getLaneComponentDataFromEntity[parent];
                            if (laneComponentData.id == carComponentData.TrackId)
                            {
                                hit = it;
                                isTrackHitFound = true;

                                var distance = math.distance(localToWorld.Position, hit.Position);
                                if (math.dot(localToWorld.Forward, hit.SurfaceNormal) < 0 && distance < thresholdDistance)
                                {
                                    /* The car is approaching and is near to the track, let's turn it left */
                                    /* Here soften the car trajectory so that it is a softened synusoid*/
                                    var distanceCopy = distance;
                                    if (distanceCopy > 2.5f) distanceCopy = 2.5f;
                                    factor = -carComponentData.Speed * deltaTime;
                                    Log("1");
                                }
                                else if (math.dot(localToWorld.Forward, hit.SurfaceNormal) < -math.cos(math.radians(steeringDegree)) && distance >= thresholdDistance)
                                {
                                    // The car is approaching but is distant from the track
                                    factor = 0;
                                }
                                else if (math.dot(localToWorld.Forward, hit.SurfaceNormal) > -math.cos(math.radians(steeringDegree)) && distance >= thresholdDistance)
                                {
                                    // The car is going away from the track
                                    var distanceCopy = distance;
                                    if (distanceCopy > 2.5f) distanceCopy = 2.5f;
                                    factor = carComponentData.Speed * deltaTime;
                                }
                                else
                                {
                                    // The car is going straight or (it is leaving but its distance from the center of the lane is admissible)
                                    factor = 0;
                                }

                                Log(distance);
                                Log(math.dot(localToWorld.Forward, hit.SurfaceNormal));
                                // find the closest track with that id to avoid conflicts with neighbour streets
                                break;
                            }
                        }
                    }
                }
            }

            if (!isTrackHitFound && physicsWorld.CastRay(raycastInputLeft, ref leftHits) && leftHits.Length > 1)
            {
                RaycastHit hit = default;
                foreach (var it in leftHits)
                {
                    if (getTrackComponentDataFromEntity.HasComponent(it.Entity))
                    {
                        var parent = getParentComponentDataFromEntity[it.Entity].Value;
                        if (getLaneComponentDataFromEntity.HasComponent(parent))
                        {
                            var laneComponentData = getLaneComponentDataFromEntity[parent];
                            if (laneComponentData.id == carComponentData.TrackId)
                            {
                                hit = it;
                                isTrackHitFound = true;

                                var distance = math.distance(localToWorld.Position, hit.Position);
                                if (math.dot(localToWorld.Forward, hit.SurfaceNormal) < 0 && distance < thresholdDistance)
                                {
                                    // The car is approaching and is near to the track, let's turn it right
                                    var distanceCopy = distance;
                                    if (distanceCopy > 2.5f) distanceCopy = 2.5f;
                                    factor = carComponentData.Speed * deltaTime;
                                    Log("1L");
                                } else if (math.dot(localToWorld.Forward, hit.SurfaceNormal) < -math.cos(math.radians(steeringDegree)) && distance >= thresholdDistance)
                                {
                                    // The car is approaching but is distant from the track
                                    factor = 0;
                                } else if (math.dot(localToWorld.Forward, hit.SurfaceNormal) > -math.cos(math.radians(steeringDegree)) && distance >= thresholdDistance)
                                {
                                    // The car is going away from the track
                                    var distanceCopy = distance;
                                    if (distanceCopy > 2.5f) distanceCopy = 2.5f;
                                    factor = -carComponentData.Speed * deltaTime;
                                } else
                                {
                                    // The car is going straight or (it is leaving but its distance from the center of the lane is admissible)
                                    factor = 0;
                                }

                                Log(distance);
                                Log(math.dot(localToWorld.Forward, hit.SurfaceNormal));

                                // find the closest track with that id to avoid conflicts with neighbour streets
                                break;
                            }
                        }
                    }
                }
            }
            rightHits.Dispose();
            leftHits.Dispose();

            if (isTrackHitFound == false)
            {
                /* This scenario can take place if the track is out-of-range or the car is turning at the beginning or at the end of a lane. */
                LogError("The car with id " + carEntity.Index + " cannot find any track with id " + carComponentData.TrackId + " to follow.");
                factor = 0;
            }

            physicsVelocity.Angular.y = carComponentData.AngularSpeed * factor * deltaTime;

            var tmp = physicsVelocity.Linear.y;
            physicsVelocity.Linear = localToWorld.Forward * carComponentData.Speed * deltaTime;
            // Neglect the y speed
            physicsVelocity.Linear.y = tmp;
        }).Run();
    }
}

