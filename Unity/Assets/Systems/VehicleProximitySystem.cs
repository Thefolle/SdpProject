using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Physics;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics.Systems;
using static UnityEngine.Debug;
using Unity.Collections;
using Unity.Jobs;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public class VehicleProximitySystem : SystemBase
{
    protected override void OnUpdate()
    {
        double elapsedTime = Time.ElapsedTime;
        if (elapsedTime < 2) return;

        var physicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>().PhysicsWorld;
        var getCarComponentDataFromEntity = GetComponentDataFromEntity<CarComponentData>();

        var sphereHits = new NativeList<ColliderCastHit>(20, Allocator.Temp);

        Entities.ForEach((ref PhysicsVelocity physicsVelocity, ref CarComponentData carComponentData, in Entity carEntity, in LocalToWorld localToWorld) =>
        {

            var radius = 1.75f;
            var direction = localToWorld.Forward;
            var maxDistance = 0.001f;

            var hittingAnotherCar = false;

            var StartR = localToWorld.Position + 8.4f * math.normalize(localToWorld.Forward) - 1.5f * math.normalize(localToWorld.Up);
            if (physicsWorld.SphereCastAll(StartR, radius, direction, maxDistance, ref sphereHits, CollisionFilter.Default) && sphereHits.Length >= 1)
            {
                /*var EndR = new float3();
                EndR = StartR + radius * math.normalize(localToWorld.Forward) + maxDistance * localToWorld.Forward;
                UnityEngine.Debug.DrawLine(StartR, EndR, UnityEngine.Color.white, 0);
                EndR = StartR + radius * math.normalize(-localToWorld.Forward);
                UnityEngine.Debug.DrawLine(StartR, EndR, UnityEngine.Color.white, 0);
                EndR = StartR + radius * math.normalize(localToWorld.Right);
                UnityEngine.Debug.DrawLine(StartR, EndR, UnityEngine.Color.white, 0);
                EndR = StartR + radius * math.normalize(-localToWorld.Right);
                UnityEngine.Debug.DrawLine(StartR, EndR, UnityEngine.Color.white, 0);*/

                foreach (var i in sphereHits)
                {
                    if (getCarComponentDataFromEntity.HasComponent(i.Entity) && carEntity.Index != i.Entity.Index)
                    {
                        hittingAnotherCar = true;
                        carComponentData.emergencyBrakeActivated = true;
                        break;
                    }
                }

                if(!hittingAnotherCar)
                    carComponentData.emergencyBrakeActivated = false;
            }
            sphereHits.Clear();
        })
        .WithNativeDisableContainerSafetyRestriction(getCarComponentDataFromEntity)
        .Run();
        sphereHits.Dispose();
    }
}
