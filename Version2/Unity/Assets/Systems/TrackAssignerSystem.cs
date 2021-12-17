using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using static UnityEngine.Debug;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public class TrackAssignerSystem : SystemBase
{
    protected override void OnUpdate()
    {
        if (World.Time.ElapsedTime < 2) return;
        
        var getBufferFromEntity = GetBufferFromEntity<PathComponentData>();
        var getCrossComponentData = GetComponentDataFromEntity<CrossComponentData>();
        var getStreetComponentData = GetComponentDataFromEntity<StreetComponentData>();
        var getTrackComponentData = GetComponentDataFromEntity<TrackComponentData>();
        var getParentComponentData = GetComponentDataFromEntity<Parent>();
        var getChildComponentData = GetBufferFromEntity<Child>();
        EntityManager entityManager = World.EntityManager;
        PhysicsWorld physicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>().PhysicsWorld;

        var graph = World.GetExistingSystem<GraphGeneratorSystem>().District;

        Entities.ForEach((ref CarComponentData carComponentData, in Entity carEntity, in LocalToWorld localToWorld) =>
        {

            if (carComponentData.vehicleIsOn == VehicleIsOn.PassingFromStreetToCross && !carComponentData.isPathUpdated) // the car is passing from a street to a cross
            {
                var path = getBufferFromEntity[carEntity];
                if (path.Length == 0)
                {
                    LogErrorFormat("The path of the car with id {0} has length {1} which is inconsistent.", carEntity.Index, path.Length);
                    return;
                }
                else if (path.Length == 1 || path.Length == 2)
                {
                    /* Destination reached: indeed, the path only contains the previous street and the current cross
                        */
                    //LogFormat("The car with id {0} has reached its destination", carEntity.Index);
                    /* The despawn will be carried out by another system */
                    carComponentData.HasReachedDestination = true;
                    return;
                }
                var previousStreet = path.ElementAt(0).CrossOrStreet;
                var currentCross = path.ElementAt(1).CrossOrStreet;
                var nextStreet = path.ElementAt(2).CrossOrStreet;
                path.RemoveAt(0);

                /* Compute the track id to assign:
                    * First, infer which is the incoming street;
                    * Second, infer which is the outgoing street;
                    */
                var crossComponentData = getCrossComponentData[currentCross];
                string trackToAssignName = "";
                if (crossComponentData.TopStreet != Entity.Null && crossComponentData.TopStreet == previousStreet)
                {
                    trackToAssignName += "Top";
                }
                else if (crossComponentData.RightStreet != Entity.Null && crossComponentData.RightStreet == previousStreet)
                {
                    trackToAssignName += "Right";
                }
                else if (crossComponentData.BottomStreet != Entity.Null && crossComponentData.BottomStreet == previousStreet)
                {
                    trackToAssignName += "Bottom";
                }
                else if (crossComponentData.LeftStreet != Entity.Null && crossComponentData.LeftStreet == previousStreet)
                {
                    trackToAssignName += "Left";
                }
                else if (crossComponentData.CornerStreet != Entity.Null && crossComponentData.CornerStreet == previousStreet)
                {
                    trackToAssignName += "Corner";
                }
                else
                {
                    LogErrorFormat("The cross with id {0} is not linked to the incoming street of a car. Check that its CrossComponentData is consistent.", currentCross.Index);
                }

                trackToAssignName += "-";

                if (crossComponentData.TopStreet != Entity.Null && crossComponentData.TopStreet == nextStreet)
                {
                    trackToAssignName += "Top";
                }
                else if (crossComponentData.RightStreet != Entity.Null && crossComponentData.RightStreet == nextStreet)
                {
                    trackToAssignName += "Right";
                }
                else if (crossComponentData.BottomStreet != Entity.Null && crossComponentData.BottomStreet == nextStreet)
                {
                    trackToAssignName += "Bottom";
                }
                else if (crossComponentData.LeftStreet != Entity.Null && crossComponentData.LeftStreet == nextStreet)
                {
                    trackToAssignName += "Left";
                }
                else if (crossComponentData.CornerStreet != Entity.Null && crossComponentData.CornerStreet == nextStreet)
                {
                    trackToAssignName += "Corner";
                }
                else
                {
                    LogErrorFormat("Cannot find the outgoing street of a track to assign to a car.");
                }

                //LogFormat("The track name is: {0}", trackToAssignName);

                var buffer = getChildComponentData[currentCross];
                var trackToAssign = Entity.Null;
                var minimumRelativeTrackDistance = int.MaxValue;
                var currentLane = getParentComponentData[carComponentData.Track].Value;
                string currentTrackName;
                int currentRelativeTrackDistance;
                foreach (var trackChild in buffer)
                {
                    if (getTrackComponentData.HasComponent(trackChild.Value))
                    {
                        currentTrackName = entityManager.GetName(trackChild.Value);
                        currentRelativeTrackDistance = math.abs(int.Parse(currentTrackName.Split('-')[2]) - int.Parse(entityManager.GetName(currentLane).Split('-')[1]));
                        if (currentTrackName.Contains(trackToAssignName) && currentRelativeTrackDistance < minimumRelativeTrackDistance)
                        {
                            trackToAssign = trackChild.Value;
                            minimumRelativeTrackDistance = currentRelativeTrackDistance;
                        }
                    }
                    
                }
                if (trackToAssign == Entity.Null)
                {
                    LogErrorFormat("The cross with id {0} doesn't contain a track with name {1}", currentCross.Index, trackToAssignName);
                }

                //LogFormat("minimum distance: {0}", minimumRelativeTrackDistance);

                carComponentData.isPathUpdated = true;
                carComponentData.TrackId = trackToAssign.Index;
                carComponentData.Track = trackToAssign;

                //LogFormat("I've assigned track {0} to car with id {1}", carComponentData.TrackId, carEntity.Index);
            }
            else if (carComponentData.vehicleIsOn == VehicleIsOn.Street && !carComponentData.isPathUpdated) // The car is passing from a cross to a street
            {
                var path = getBufferFromEntity[carEntity];

                if (path.Length == 0)
                {
                    LogErrorFormat("The path of car with id {0} has length 0 which is inconsistent.", carEntity.Index);
                    return;
                }
                else if (path.Length == 1)
                {
                    /* Destination reached: indeed, it is not possible to infer which track to assign since the next cross
                        * is not in the path to follow
                        */
                    //LogFormat("The car with id {0} has reached its destination.", carEntity.Index);
                    /* The despawn will be carried out by another system */
                    carComponentData.HasReachedDestination = true;
                    return;
                }
                var currentCross = path.ElementAt(0).CrossOrStreet;
                var nextStreet = path.ElementAt(1).CrossOrStreet;
                path.RemoveAt(0);

                var street = nextStreet;
                var streetComponentData = getStreetComponentData[street];
                var trackCandidatesName = "";
                if (streetComponentData.startingCross == currentCross)
                {
                    trackCandidatesName += "ForwardLane";
                }
                else if (streetComponentData.endingCross == currentCross)
                {
                    trackCandidatesName += "BackwardLane";
                }
                else
                {
                    LogErrorFormat("The street with id {0} is not linked to the cross with id {1} due to some inconsistency.", street.Index, currentCross.Index);
                }

                var lanes = getChildComponentData[street];
                var trackToFollow = Entity.Null;
                foreach (var laneChild in lanes)
                {
                    if (entityManager.GetName(laneChild.Value).Contains(trackCandidatesName))
                    {
                        /* Just take the first admissible track for now */
                        var trackChild = getChildComponentData[laneChild.Value];
                        if (trackChild.Length > 1)
                        {
                            LogErrorFormat("A lane has multiple track children");
                            return;
                        }
                        trackToFollow = trackChild.ElementAt(0).Value;
                        break;
                    }
                }

                if (trackToFollow == Entity.Null)
                {
                    LogErrorFormat("No admissible tracks in a street are available for a car.");
                }
                else
                {
                    carComponentData.isPathUpdated = true;
                    carComponentData.TrackId = trackToFollow.Index;
                    carComponentData.Track = trackToFollow;

                    //LogFormat("I've assigned track {0} to car with id {1}", carComponentData.TrackId, carEntity.Index);
                }
            }
            else if (carComponentData.HasJustSpawned && /* other checks for robustness */ carComponentData.vehicleIsOn == VehicleIsOn.SpawningPoint /*&& vehicleIsOn == VehicleIsOn.SpawningPoint this check doesn't allow arbitrary spawning on the city*/)
            {
                /* TODO: postpone the track assignment if some car is approaching along the nearest track */

                carComponentData.HasJustSpawned = false;
                /* Search for the nearest admissible track */
                var trackFilter = new CollisionFilter
                {
                    BelongsTo = 1 << 0,
                    CollidesWith = 1 << 0,
                    GroupIndex = 0
                };
                var raycastInputRight = new RaycastInput
                {
                    Start = localToWorld.Position,
                    End = localToWorld.Position + 20 * localToWorld.Right,
                    Filter = trackFilter
                };
                var raycastInputLeft = new RaycastInput
                {
                    Start = localToWorld.Position,
                    End = localToWorld.Position + 20 * -localToWorld.Right,
                    Filter = trackFilter
                };
                var rightHits = new NativeList<RaycastHit>(20, Allocator.TempJob);
                var leftHits = new NativeList<RaycastHit>(20, Allocator.TempJob);

                bool isTrackHitFound = false; // flag that tells whether at least one admissible hit has been found
                RaycastHit hit = default;
                var forward = math.normalize(localToWorld.Forward);
                var minimumDistance = float.MaxValue;

                if (physicsWorld.CastRay(raycastInputLeft, ref leftHits))
                {
                    foreach (var it in leftHits)
                    {
                        if (getTrackComponentData.HasComponent(it.Entity))
                        {
                            var trackComponentData = getTrackComponentData[it.Entity];

                            /* Does the track it.Entity belong to the current street? */
                            if (getParentComponentData.HasComponent(it.Entity))
                            {
                                var parent = getParentComponentData[it.Entity].Value;
                                if (getParentComponentData.HasComponent(parent))
                                {
                                    var grandParent = getParentComponentData[parent].Value;
                                    if (getStreetComponentData.HasComponent(grandParent))
                                    {
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
                if (/*!isTrackHitFound && this check prevents car from being arbitrarily spawned between the center of the street
                        and the leftmost track of the right group of lanes*/ physicsWorld.CastRay(raycastInputRight, ref rightHits))
                {
                    foreach (var it in rightHits)
                    {
                        if (getTrackComponentData.HasComponent(it.Entity))
                        {
                            var trackComponentData = getTrackComponentData[it.Entity];

                            /* Does the track it.Entity belong to the current street? */
                            if (getParentComponentData.HasComponent(it.Entity))
                            {
                                var parent = getParentComponentData[it.Entity].Value;
                                if (getParentComponentData.HasComponent(parent))
                                {
                                    var grandParent = getParentComponentData[parent].Value;
                                    if (getStreetComponentData.HasComponent(grandParent))
                                    {
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
                    carComponentData.Track = hit.Entity;

                    //Log("I've assigned track " + carComponentData.TrackId + " to car with id " + carEntity.Index);

                    /* Request a random path */
                    var trackedLane = getParentComponentData[hit.Entity].Value;
                    var trackedLaneName = entityManager.GetName(trackedLane);
                    var street = getParentComponentData[trackedLane].Value;
                    var streetComponentData = getStreetComponentData[street];
                    int edgeInitialNode;
                    int edgeEndingNode;
                    /* Exploit the track name to infer which are the starting and the ending crosses. That's why 
                        * the algorithm must work with tracks rather than streets
                        */
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
                        }
                        else
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

public enum VehicleIsOn
{
    Street,
    Cross,
    SpawningPoint,
    PassingFromCrossToStreet,
    PassingFromStreetToCross
}