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

    private const float LaneWidth = 2.5f * EditorFactor;

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
            bool isCollisionFound = false; // flag that tells whether at least one admissible hit has been found
            ColliderCastHit coll = default;

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

                sphereHits.Dispose();
            }
        }).Run();

    }
}

