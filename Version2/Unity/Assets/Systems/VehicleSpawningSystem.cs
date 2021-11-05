using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Physics;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics.Systems;
using static UnityEngine.Debug;
using Unity.Collections;

/// <summary>
/// <para>This system assigns to a car the initial id to track when:
/// <list type="bullet">
/// <item>The car has spawned;</item>
/// <item>The car has physically passed from a street (cross) to a cross (street).</item>
/// </list></para>
/// </summary>
public class VehicleSpawningSystem : SystemBase
{
    /// <summary>
    /// <para>For now, model this as a global variable. In future, when the spawning system will be created,
    /// you may want to improve this approach.</para>
    /// </summary>
    bool hasSpawned;

    protected override void OnCreate()
    {
        base.OnCreate();

        hasSpawned = true;
    }

    protected override void OnUpdate()
    {
        // if (condition 1 is true)
        // assign track
        // else if (condition 2 is true)
        // assign track (improvement: follow the same relative track as before)

        PhysicsWorld physicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>().PhysicsWorld;
        EntityManager entityManager = World.EntityManager;
        var getTrackComponentDataFromEntity = GetComponentDataFromEntity<TrackComponentData>();
        var getParentComponentDataFromEntity = GetComponentDataFromEntity<Parent>();
        var getStreetComponentDataFromEntity = GetComponentDataFromEntity<StreetComponentData>();

        var isSpawned = hasSpawned;
        hasSpawned = false;

        Entities.ForEach((ref CarComponentData carComponentData, in LocalToWorld localToWorld, in Entity carEntity) =>
        {
            if (isSpawned)
            {
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

                bool isTrackHitFound = false; // flag that tells whether at least one admissible hit has been found
                RaycastHit hit = default;
                var forward = math.normalize(localToWorld.Forward);
                var minimumDistance = float.MaxValue;

                if (physicsWorld.CastRay(raycastInputLeft, ref leftHits) && leftHits.Length > 1)
                {
                    foreach (var it in leftHits)
                    {
                        if (getTrackComponentDataFromEntity.HasComponent(it.Entity))
                        {
                            var trackComponentData = getTrackComponentDataFromEntity[it.Entity];

                            /* Does the track it.Entity belong to the current street? */
                            if (getParentComponentDataFromEntity.HasComponent(it.Entity))
                            {
                                var parent = getParentComponentDataFromEntity[it.Entity].Value;
                                if (getParentComponentDataFromEntity.HasComponent(parent))
                                {
                                    var grandParent = getParentComponentDataFromEntity[parent].Value;
                                    if (getStreetComponentDataFromEntity.HasComponent(grandParent))
                                    {
                                        // TODO: check if the street of the it.Entity track equals the assigned street of the car
                                        var carDistanceFromTrack = math.distance(localToWorld.Position, it.Position);
                                        if (carDistanceFromTrack < minimumDistance)
                                        {
                                            minimumDistance = carDistanceFromTrack;
                                            hit = it;

                                            isTrackHitFound = true;
                                        }
                                    }
                                }
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

                            /* Does the track it.Entity belong to the current street? */
                            if (getParentComponentDataFromEntity.HasComponent(it.Entity))
                            {
                                var parent = getParentComponentDataFromEntity[it.Entity].Value;
                                if (getParentComponentDataFromEntity.HasComponent(parent))
                                {
                                    var grandParent = getParentComponentDataFromEntity[parent].Value;
                                    if (getStreetComponentDataFromEntity.HasComponent(grandParent))
                                    {
                                        // TODO: check if the street of the it.Entity track equals the assigned street of the car
                                        var carDistanceFromTrack = math.distance(localToWorld.Position, it.Position);
                                        if (carDistanceFromTrack < minimumDistance)
                                        {
                                            minimumDistance = carDistanceFromTrack;
                                            hit = it;

                                            isTrackHitFound = true;
                                        }
                                    }
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
                    LogError("The car with id " + carEntity.Index + " cannot find any track to follow.");
                }
                else
                {
                    carComponentData.TrackId = hit.Entity.Index;
                    Log("I've assigned track " + carComponentData.TrackId + " to car with id " + carEntity.Index);

                    /* Request a random path */
                    var trackedLane = getParentComponentDataFromEntity[hit.Entity].Value;
                    var trackedLaneName = entityManager.GetName(trackedLane);
                    var street = getParentComponentDataFromEntity[trackedLane].Value;
                    carComponentData.CrossOrStreet = street;
                    carComponentData.ImInCross = false;
                    var streetComponentData = getStreetComponentDataFromEntity[street];
                    int edgeInitialNode;
                    int edgeEndingNode;
                    if (trackedLaneName.Contains("Forward"))
                    {
                        edgeInitialNode = streetComponentData.startingCross.Index;
                        edgeEndingNode = streetComponentData.endingCross.Index;
                    }
                    else if (trackedLaneName.Contains("Backward"))
                    {
                        edgeInitialNode = streetComponentData.endingCross.Index;
                        edgeEndingNode = streetComponentData.startingCross.Index;
                    }
                    else
                    {
                        LogErrorFormat("%s", "The trackedLane name is malformed: it doesn't contain neither \"Forward\" nor \"Backward\"");
                        /* Cannot recover from this error */
                        edgeInitialNode = 0;
                        edgeEndingNode = 0;
                    }


                    var graph = World.GetExistingSystem<GraphGeneratorSystem>().District;
                    var randomPath = graph.RandomPath(edgeInitialNode, edgeEndingNode);
                    var carPath = GetBufferFromEntity<PathComponentData>()[carEntity];
                    foreach (var crossId in randomPath)
                    {
                        Log(crossId);
                        var step = new PathComponentData
                        {
                            crossId = crossId
                        };
                        carPath.Add(step);
                    }

                }

            }
        }).WithoutBurst().Run();

    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="approachingCross">The first cross the car is going to meet.</param>
    /// <param name="nextCrossId">The cross after the <paramref name="approachingCross"/> that the car pass through.</param>
    /// <param name="relativeTrack">The relative track id the car is following, so that the cross tries to return
    /// the track id with the same relative track id.</param>
    /// <returns>The track id the requesting car should follow.</returns>
    public int GetTrackId(Entity approachingCross, int nextCrossId, int relativeTrack)
    {
        var getCrossComponentData = GetComponentDataFromEntity<CrossComponentData>();
        var getStreetComponentData = GetComponentDataFromEntity<StreetComponentData>();
        var crossComponentData = getCrossComponentData[approachingCross];
        var direction = "";

        // evaluate which is the direction that leads to the next cross
        if (crossComponentData.TopStreet != Entity.Null)
        {
            var topStreetComponentData = getStreetComponentData[crossComponentData.TopStreet];
            var otherNode = -1;
            if (topStreetComponentData.startingCross.Index == approachingCross.Index)
            {
                otherNode = topStreetComponentData.endingCross.Index;
            } else
            {
                otherNode = topStreetComponentData.startingCross.Index;
            }

            if (otherNode == nextCrossId)
            {
                direction = "top";
            }
        }

        return 0;
    }

}
