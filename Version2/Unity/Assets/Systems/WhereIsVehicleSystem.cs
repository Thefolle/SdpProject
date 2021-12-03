using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Physics;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics.Systems;
using static UnityEngine.Debug;
using Unity.Collections;

public class WhereIsVehicleSystem : SystemBase
{

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
        var getCrossComponentDataFromEntity = GetComponentDataFromEntity<CrossComponentData>();
        var getObstaclesComponentDataFromEntity = GetComponentDataFromEntity<ObstaclesComponent>();
        var getCarComponentDataFromEntity = GetComponentDataFromEntity<CarComponentData>();
        var getTrafficLightComponentDataFromEntity = GetComponentDataFromEntity<TrafficLightComponentData>();
        var getTrafficLightCrossComponentDataFromEntity = GetComponentDataFromEntity<TrafficLightCrossComponentData>();
        var getStreetComponentDataFromEntity = GetComponentDataFromEntity<StreetComponentData>();
        var getBaseCrossComponentDataFromEntity = GetComponentDataFromEntity<BaseCrossComponentData>();

        Entities.ForEach((Entity carEntity, LocalToWorld localToWorld, ref PhysicsVelocity physicsVelocity, ref CarComponentData carComponentData) =>
        {
            var sphereHits = new NativeList<ColliderCastHit>(20, Allocator.TempJob);

            var radius = 0.5f;
            var direction = localToWorld.Forward;
            var maxDistance = 3.5f;

            var isOnStreet = false;
            var isOnCross = false;

            var StartR = localToWorld.Position - 5f * math.normalize(localToWorld.Forward) - 1.5f * math.normalize(localToWorld.Up);
            if (physicsWorld.SphereCastAll(StartR, radius, direction, maxDistance, ref sphereHits, CollisionFilter.Default) && sphereHits.Length >= 1)
            {
                var EndR = new float3();
                EndR = StartR + radius * math.normalize(localToWorld.Forward) + maxDistance * localToWorld.Forward;
                UnityEngine.Debug.DrawLine(StartR, EndR, UnityEngine.Color.green, 0);
                EndR = StartR + radius * math.normalize(-localToWorld.Forward);
                UnityEngine.Debug.DrawLine(StartR, EndR, UnityEngine.Color.green, 0);
                EndR = StartR + radius * math.normalize(localToWorld.Right);
                UnityEngine.Debug.DrawLine(StartR, EndR, UnityEngine.Color.green, 0);
                EndR = StartR + radius * math.normalize(-localToWorld.Right);
                UnityEngine.Debug.DrawLine(StartR, EndR, UnityEngine.Color.green, 0);

                foreach (var i in sphereHits)
                {
                    if(getLaneComponentDataFromEntity.HasComponent(i.Entity))
                    {
                        isOnStreet = true;
                    }
                    else if(getBaseCrossComponentDataFromEntity.HasComponent(i.Entity))
                    {
                        isOnCross = true;
                    }

                    if (isOnStreet && isOnCross)
                        break;

                }

                if (isOnStreet && isOnCross && carComponentData.vehicleIsOn == VehicleIsOn.Cross)
                {
                    carComponentData.vehicleIsOn = VehicleIsOn.PassingFromCrossToStreet;
                    carComponentData.isPathUpdated = false;
                }
                else if (isOnStreet && isOnCross && carComponentData.vehicleIsOn == VehicleIsOn.Street)
                {
                    carComponentData.vehicleIsOn = VehicleIsOn.PassingFromStreetToCross;
                    carComponentData.isPathUpdated = false;
                }
                else if (isOnStreet && !isOnCross && carComponentData.vehicleIsOn == VehicleIsOn.PassingFromStreetToCross) // some bends are so short that the car never hits a cross only
                {
                    carComponentData.vehicleIsOn = VehicleIsOn.PassingFromCrossToStreet;
                    carComponentData.isPathUpdated = false;
                }
                else if (isOnStreet && !isOnCross)
                {
                    carComponentData.vehicleIsOn = VehicleIsOn.Street;
                }
                else if (!isOnStreet && isOnCross)
                {
                    carComponentData.vehicleIsOn = VehicleIsOn.Cross;
                } else if (isOnStreet && isOnCross && carComponentData.vehicleIsOn != VehicleIsOn.Cross && carComponentData.vehicleIsOn != VehicleIsOn.Street)
                {
                    /* Do nothing. Just to distinguish that this case is admissible */
                } else if (isOnStreet && isOnCross && carComponentData.vehicleIsOn == VehicleIsOn.Cross)
                {
                    LogErrorFormat("Unforseen car state: isOnStreet = {0}, isOnCross = {1}", isOnStreet, isOnCross);
                }

                if (isOnStreet && isOnCross)
                {
                    carComponentData.isOnStreetAndCross = true;
                    carComponentData.isOnStreet = false;
                    carComponentData.isOnCross = false;
                }
                else if(isOnStreet)
                {
                    carComponentData.isOnStreetAndCross = false;
                    carComponentData.isOnStreet = true;
                    carComponentData.isOnCross = false;
                }
                else if(isOnCross)
                {
                    carComponentData.isOnStreetAndCross = false;
                    carComponentData.isOnStreet = false;
                    carComponentData.isOnCross = true;
                }

                
            }

            sphereHits.Dispose();
        }).Run();

    }
}

