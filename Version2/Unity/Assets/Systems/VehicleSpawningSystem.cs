using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Physics;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics.Systems;
using static UnityEngine.Debug;
using Unity.Collections;

public class VehicleSpawningSystem : SystemBase
{

    protected override void OnUpdate()
    {
        PhysicsWorld physicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>().PhysicsWorld;
        EntityManager entityManager = World.EntityManager;
        var getTrackComponentDataFromEntity = GetComponentDataFromEntity<TrackComponentData>();
        var getParentComponentDataFromEntity = GetComponentDataFromEntity<Parent>();
        var getStreetComponentDataFromEntity = GetComponentDataFromEntity<StreetComponentData>();

        Entities.ForEach((ref CarComponentData carComponentData, in LocalToWorld localToWorld, in Entity carEntity) =>
        {
            if (carComponentData.HasJustSpawned)
            {
                carComponentData.HasJustSpawned = false;

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
                    var carPath = GetBufferFromEntity<PathComponentData>()[carEntity];
                    var randomPath = graph.RandomPath(edgeInitialNode, edgeEndingNode);

                    var isFirst = true;
                    var lastStep = -1;
                    foreach (var node in randomPath)
                    {
                        if (isFirst)
                        {
                            // carPath.Add(new PathComponentData { CrossOrStreet = node.Cross }); //neglect the first node when a car is spawned in a street
                            lastStep = node.Cross.Index;
                            isFirst = false;
                        } else
                        {
                            carPath.Add(new PathComponentData { CrossOrStreet = graph.GetEdge(lastStep, node.Cross.Index).Street });
                            carPath.Add(new PathComponentData { CrossOrStreet = node.Cross });
                            lastStep = node.Cross.Index;
                        }
                    }
                }
            }
        }).WithoutBurst().Run();
    }

}
