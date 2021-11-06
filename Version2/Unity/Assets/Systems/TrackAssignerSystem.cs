using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using static UnityEngine.Debug;

public class TrackAssignerSystem : SystemBase
{
    protected override void OnUpdate()
    {
        
        var getBufferFromEntity = GetBufferFromEntity<PathComponentData>();
        var getCrossComponentData = GetComponentDataFromEntity<CrossComponentData>();
        var getStreetComponentData = GetComponentDataFromEntity<StreetComponentData>();
        var getChildComponentDataFromEntity = GetBufferFromEntity<Child>();
        EntityManager entityManager = World.EntityManager;
        PhysicsWorld physicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>().PhysicsWorld;

        var graph = World.GetExistingSystem<GraphGeneratorSystem>().District;

        Entities.ForEach((ref CarComponentData carComponentData, in Entity carEntity, in LocalToWorld localToWorld) =>
        {
            var bottom = -math.normalize(localToWorld.Up);
            var raycastInput = new RaycastInput
            {
                Start = localToWorld.Position,
                End = localToWorld.Position + 10 * bottom,
                Filter = CollisionFilter.Default
            };

            var hits = new NativeList<RaycastHit>(20, Allocator.TempJob);

            bool ImInCross = true;
            bool admissibleHitFound = false;

            if (physicsWorld.CastRay(raycastInput, ref hits) && hits.Length > 1)
            {
                foreach (var it in hits)
                {
                    if (entityManager.GetName(it.Entity).Contains("Lane"))
                    {
                        ImInCross = false;
                        admissibleHitFound = true;
                        break;
                    }
                    else if (entityManager.GetName(it.Entity).Equals("Base"))
                    {
                        ImInCross = true;
                        admissibleHitFound = true;
                        break;
                    }
                }
            }
            hits.Dispose();

            if (!admissibleHitFound)
            {
                LogErrorFormat("The car with id {0} doesn't know if it is on a street or on a cross", carEntity.Index);
                return;
            }
            else
            {
                if (ImInCross && !carComponentData.ImInCross) // the car is passing from a street to a cross
                {
                    var path = getBufferFromEntity[carEntity];
                    if (path.Length == 0 || path.Length == 2)
                    {
                        LogErrorFormat("The path of a car has length {0} which is inconsistent.", path.Length);
                        return;
                    }
                    else if (path.Length == 1)
                    {
                        LogFormat("The car with id {0} has reached its destination", carEntity.Index);
                        return;
                    }
                    var currentStreet = path.ElementAt(0).CrossOrStreet;
                    var currentCross = path.ElementAt(1).CrossOrStreet;
                    var nextStreet = path.ElementAt(2).CrossOrStreet;
                    path.RemoveAt(0);

                    /* Compute the track id to assign:
                        * First, infer which is the incoming street;
                        * Second, infer which is the outgoing street;
                        */
                    var crossComponentData = getCrossComponentData[currentCross];
                    string trackToAssignName = "";
                    if (crossComponentData.TopStreet != Entity.Null && crossComponentData.TopStreet == currentStreet)
                    {
                        trackToAssignName += "Top";
                    }
                    else if (crossComponentData.RightStreet != Entity.Null && crossComponentData.RightStreet == currentStreet)
                    {
                        trackToAssignName += "Right";
                    }
                    else if (crossComponentData.BottomStreet != Entity.Null && crossComponentData.BottomStreet == currentStreet)
                    {
                        trackToAssignName += "Bottom";
                    }
                    else if (crossComponentData.LeftStreet != Entity.Null && crossComponentData.LeftStreet == currentStreet)
                    {
                        trackToAssignName += "Left";
                    }
                    else if (crossComponentData.CornerStreet != Entity.Null && crossComponentData.CornerStreet == currentStreet)
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

                    var buffer = getChildComponentDataFromEntity[currentCross];
                    var trackToAssign = Entity.Null;
                    foreach (var trackChild in buffer)
                    {
                        if (entityManager.GetName(trackChild.Value).Contains(trackToAssignName))
                        {
                            trackToAssign = trackChild.Value;
                            break; // for now, neglect the relative track issue and select the first available track
                        }
                    }
                    if (trackToAssign == Entity.Null)
                    {
                        LogErrorFormat("The cross with id {0} doesn't contain a track with name {1}", currentCross.Index, trackToAssignName);
                    }

                    carComponentData.ImInCross = true;
                    carComponentData.TrackId = trackToAssign.Index;
                    LogFormat("I've assigned track {0} to car with id {1}", carComponentData.TrackId, carEntity.Index);
                }
                else if (!ImInCross && carComponentData.ImInCross) // The car is passing from a cross to a street
                {
                    var path = getBufferFromEntity[carEntity];

                    if (path.Length == 0)
                    {
                        LogErrorFormat("The cross where a car is passing through is not stored in its path to follow");
                        return;
                    }
                    else if (path.Length == 1)
                    {
                        /* Destination reached: indeed, it is not possible to infer which track to assign since the next cross
                            * is not in the path to follow
                            */
                        LogFormat("The car with id {0} has reached its destination.", carEntity.Index);
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

                    var lanes = getChildComponentDataFromEntity[street];
                    var trackToFollow = Entity.Null;
                    foreach (var laneChild in lanes)
                    {
                        if (entityManager.GetName(laneChild.Value).Contains(trackCandidatesName))
                        {
                            /* Just take the first admissible track for now */
                            var trackChild = getChildComponentDataFromEntity[laneChild.Value];
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
                        carComponentData.ImInCross = false;
                        carComponentData.TrackId = trackToFollow.Index;
                        LogFormat("I've assigned track {0} to car with id {1}", carComponentData.TrackId, carEntity.Index);
                    }
                }
                else
                {
                    /* The car is where it declares to be. Nothing to do */
                }
            }
            
        }).WithoutBurst().Run();
    
    }
}
