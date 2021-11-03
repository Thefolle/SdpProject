using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Physics;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics.Systems;
using static UnityEngine.Debug;
using Unity.Collections;

public class AntiCollisionSystem : SystemBase
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

        Entities.ForEach((Entity carEntity, LocalToWorld localToWorld, ref PhysicsVelocity physicsVelocity, ref CarComponentData carCompData, in CarComponentData carComponentData) =>
        {
            // Anti-collision Raycasts
            float speedFactor;                   // This factor is for regulating the raycasts wrt the car velocity
            if (carComponentData.Speed >= 20)
            {
                speedFactor = 0.03f * carComponentData.Speed;
            }
            else
            {
                speedFactor = 0;
            }
            //speedFactor = 0.03f * carComponentData.Speed;

            var StartR = new float3();
            var EndR = new float3();
            var StartL = new float3();
            var EndL = new float3();

            if (carCompData.tryOvertake == false)
            {
                StartR = localToWorld.Position + 1 * localToWorld.Forward + 1 * localToWorld.Right;
                EndR = localToWorld.Position + 5.5f * localToWorld.Forward + speedFactor * localToWorld.Forward + 1 * localToWorld.Right;
                StartL = localToWorld.Position + 1 * localToWorld.Forward - 1 * localToWorld.Right;
                EndL = localToWorld.Position + 5.5f * localToWorld.Forward + speedFactor * localToWorld.Forward - 1 * localToWorld.Right;
            } else // is overtaking, less starting value of anti-collision raycast 
            {
                StartR = localToWorld.Position + 1 * localToWorld.Forward + 1 * localToWorld.Right;
                EndR = localToWorld.Position + 1.5f * localToWorld.Forward + speedFactor * localToWorld.Forward + 1 * localToWorld.Right;
                StartL = localToWorld.Position + 1 * localToWorld.Forward - 1 * localToWorld.Right;
                EndL = localToWorld.Position + 1.5f * localToWorld.Forward + speedFactor * localToWorld.Forward - 1 * localToWorld.Right;
            }

            //var StartR = localToWorld.Position + 1 * localToWorld.Forward + 1 * localToWorld.Right;
            //var EndR = localToWorld.Position + 2.5f * localToWorld.Forward + speedFactor * localToWorld.Forward + 1 * localToWorld.Right;
            var raycastCollisionRight = new RaycastInput
            {
                // Start = localToWorld.Position + x0 * localToWorld.Forward + y0 * localToWorld.Right,    // Assign the value x0 and y0 in order to be positioned at the front extreme right side of the car.
                // End = localToWorld.Position + x1 * localToWorld.Forward + speedFactor * localToWorld.Forward + y0 * localToWorld.Right,      // x1 must be fixed, speedfactor is variable wrt the car velocity
                Start = StartR,
                End = EndR,
                Filter = CollisionFilter.Default
            };
            UnityEngine.Debug.DrawLine(StartR, EndR, UnityEngine.Color.green, 0);
            //var StartL = localToWorld.Position + 1 * localToWorld.Forward - 1 * localToWorld.Right;
            //var EndL = localToWorld.Position + 2.5f * localToWorld.Forward + speedFactor* localToWorld.Forward - 1 * localToWorld.Right;
            var raycastCollisionLeft = new RaycastInput
            {
                Start = StartL,
                End = EndL,
                Filter = CollisionFilter.Default
            };
            UnityEngine.Debug.DrawLine(StartL, EndL, UnityEngine.Color.green, 0);
            var rightCollision = new NativeList<RaycastHit>(20, Allocator.TempJob);
            var leftCollision = new NativeList<RaycastHit>(20, Allocator.TempJob);
            bool isCollisionFound = false; // flag that tells whether at least one admissible hit has been found
            RaycastHit coll = default;
            /* Assume that there exists only one admissible hit in the world with the given id*/
            if ((physicsWorld.CastRay(raycastCollisionLeft, ref leftCollision) && leftCollision.Length >= 1) || (physicsWorld.CastRay(raycastCollisionRight, ref rightCollision) && rightCollision.Length >= 1))
            {
                foreach (var i in leftCollision)
                {
                    if (!getTrackComponentDataFromEntity.HasComponent(i.Entity) && !isCollisionFound && i.Entity.Index != carEntity.Index && getCarComponentDataFromEntity[i.Entity].TrackId == getCarComponentDataFromEntity[carEntity].TrackId)
                    {
                        coll = i;
                        isCollisionFound = true;
                        // hit found, no need to proceed
                        break;
                    }
                }
                if(!isCollisionFound)
                    foreach (var j in rightCollision)
                    {
                        if (!getTrackComponentDataFromEntity.HasComponent(j.Entity) && !isCollisionFound && j.Entity.Index != carEntity.Index && getCarComponentDataFromEntity[j.Entity].TrackId == getCarComponentDataFromEntity[carEntity].TrackId)
                        {
                            coll = j;
                            isCollisionFound = true;
                            // hit found, no need to proceed
                            break;
                        }
                    }
            }

            if (isCollisionFound)     // Braking method in case of raycast collision with another car
            {
                if (carCompData.Speed < 10)
                    carCompData.Speed = 0;
                else
                    carCompData.Speed -= 0.01f * carComponentData.maxSpeed;        // that 0.10 is the braking factor. It reduces the car speed of 10% of the initial speed (it is just an example, we may change it to a proper value)

                // SISTEMA LAMPEGGIO - Michele
                var otherCarCompData = getCarComponentDataFromEntity[coll.Entity];

                if (carCompData.maxSpeed > otherCarCompData.maxSpeed)
                    if(carCompData.Speed > otherCarCompData.Speed -2 && carCompData.Speed < otherCarCompData.Speed + 2) // myCar has more maxSpeed, but is capped by otherCar in lane
                    {
                        if ((carCompData.lastTimeTried == -1 || math.abs(carCompData.lastTimeTried - elapsedTime) > 10) && otherCarCompData.Speed == 0f) // Avoid spam-trying
                        {
                            LogError("Asked for overtake");
                            carCompData.tryOvertake = true;
                            carCompData.rightOvertakeAllowed = true;
                        } else if ((carCompData.lastTimeTried == -1 || math.abs(carCompData.lastTimeTried - elapsedTime) > 10) && otherCarCompData.Speed != 0f)
                        {
                            LogError("Asked for overtake");
                            carCompData.tryOvertake = true;
                            carCompData.rightOvertakeAllowed = false;
                        }
                    }

            }
            else
            {
                if (carCompData.Speed > carComponentData.maxSpeed)
                    carCompData.Speed = carComponentData.maxSpeed;
                else
                    carCompData.Speed += 0.003f * carComponentData.maxSpeed;
            }
            leftCollision.Dispose();
            rightCollision.Dispose();

        }).Run();
    }
}
