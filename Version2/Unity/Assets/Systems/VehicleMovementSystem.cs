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
    protected override void OnUpdate()
    {
        float deltaTime = Time.DeltaTime;
        double elapsedTime = Time.ElapsedTime;
        if (elapsedTime < 2) return;

        PhysicsWorld physicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>().PhysicsWorld;
        EntityManager entityManager = World.EntityManager;

        Entities.ForEach((CarComponentData carComponentData, LocalToWorld localToWorld, ref PhysicsVelocity physicsVelocity) =>
        {
            /* Initialize data */
            float factor;
            var raycastInputRight = new RaycastInput
            {
                Start = localToWorld.Position,
                End = localToWorld.Position + 10 * localToWorld.Right,
                Filter = CollisionFilter.Default
            };
            var raycastInputLeft = new RaycastInput
            {
                Start = localToWorld.Position,
                End = localToWorld.Position + 10 * -localToWorld.Right,
                Filter = CollisionFilter.Default
            };
            var rightHits = new NativeList<RaycastHit>(10, Allocator.TempJob);
            var leftHits = new NativeList<RaycastHit>(10, Allocator.TempJob);

            if (physicsWorld.CastRay(raycastInputRight, ref rightHits) && rightHits.Length > 1)
            {
                RaycastHit hit;
                if (entityManager.GetName(rightHits[0].Entity).Equals("StraightTrack"))
                {
                    hit = rightHits[0];
                } else
                {
                    hit = rightHits[1];
                }

                var distance = math.distance(localToWorld.Position, hit.Position);
                var dotProduct = math.dot(localToWorld.Forward, hit.SurfaceNormal);
                if (dotProduct > -0.34)
                {
                    // The car is going away, let's turn it left
                    factor = distance;
                } else
                {
                    // The car is approaching, let's turn it right
                    factor = -distance / 2;
                }
                Log(hit.SurfaceNormal);
                Log(math.dot(localToWorld.Forward, hit.SurfaceNormal));
            } else if (physicsWorld.CastRay(raycastInputLeft, ref leftHits) && leftHits.Length > 1)
            {
                RaycastHit hit;
                if (entityManager.GetName(leftHits[0].Entity).Equals("Track"))
                {
                    hit = leftHits[0];
                }
                else
                {
                    hit = leftHits[1];
                }

                var distance = math.distance(localToWorld.Position, hit.Position);
                if (math.dot(localToWorld.Forward, hit.SurfaceNormal) > -0.34)
                {
                    // The car is going away, let's turn it right
                    factor = -distance;
                }
                else
                {
                    // The car is approaching, let's turn it left
                    factor = distance / 2;
                }

                Log(hit.SurfaceNormal);
                Log(math.dot(localToWorld.Forward, hit.SurfaceNormal));
            } else
            {
                /* Fallback case */
                factor = 0;
            }
            rightHits.Dispose();
            leftHits.Dispose();

            physicsVelocity.Angular.y = carComponentData.angularSpeed * factor * deltaTime;

            var tmp = physicsVelocity.Linear.y;
            physicsVelocity.Linear = localToWorld.Forward * carComponentData.speed * deltaTime;
            // Neglect the y speed
            physicsVelocity.Linear.y = tmp;
        }).Run();
    }
}

