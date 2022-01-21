using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using static UnityEngine.Debug;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public class SplineTrackAssignerSystem : SystemBase
{
    protected override void OnUpdate()
    {
        if (World.Time.ElapsedTime < 4 || World.GetExistingSystem<StreetSplinePlacerSystem>().Enabled || World.GetExistingSystem<GraphGeneratorSystem>().Enabled) return;

        var graph = World.GetExistingSystem<GraphGeneratorSystem>().District;
        var getParentComponentData = GetComponentDataFromEntity<Parent>();
        var getStreetComponentData = GetComponentDataFromEntity<StreetComponentData>();
        var getBufferFromEntity = GetBufferFromEntity<PathComponentData>();
        var getCrossComponentData = GetComponentDataFromEntity<CrossComponentData>();
        var getChildComponentData = GetBufferFromEntity<Child>();
        var getTrackComponentData = GetComponentDataFromEntity<TrackComponentData>();
        var getLaneComponentData = GetComponentDataFromEntity<LaneComponentData>();
        EntityManager entityManager = World.EntityManager;

        Entities.ForEach((ref CarComponentData carComponentData, ref AskToDespawnComponentData askToDespawnComponentData, in Entity carEntity, in LocalToWorld localToWorld) =>
        {
            if (carComponentData.HasJustSpawned)
            {
                carComponentData.HasJustSpawned = false;

                /* Request a random path */
                var trackedLane = getParentComponentData[carComponentData.Track].Value;
                var trackComponentData = getTrackComponentData[carComponentData.Track];
                var street = getParentComponentData[trackedLane].Value;
                var streetComponentData = getStreetComponentData[street];
                if (streetComponentData.IsBorder)
                {
                    //LogFormat("Car spawned at the border street. Despawning...");
                    askToDespawnComponentData.Asked = true;
                    return;
                }
                int edgeInitialNode;
                int edgeEndingNode;
                /* Exploit the track name to infer which are the starting and the ending crosses. That's why 
                    * the algorithm must work with tracks rather than streets
                    */
                if (trackComponentData.IsForward)
                {
                    edgeInitialNode = streetComponentData.startingCross.Index;
                    edgeEndingNode = streetComponentData.endingCross.Index;
                }
                else
                {
                    edgeInitialNode = streetComponentData.endingCross.Index;
                    edgeEndingNode = streetComponentData.startingCross.Index;
                }

                var carPath = GetBufferFromEntity<PathComponentData>()[carEntity];
                var randomPath = graph.RandomPath(edgeInitialNode, edgeEndingNode);

                var isFirst = true;
                var lastStep = -1;

                foreach (var node in randomPath)
                {
                    if (isFirst)
                    {
                        //carPath.Add(new PathComponentData { CrossOrStreet = node.Cross }); //neglect the first node when a car is spawned in a street
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
            else if (!carComponentData.isOnStreet && !carComponentData.isPathUpdated) // the car is passing from a street to a cross
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
                //string trackToAssignName = "";
                Direction sourceDirection = Direction.Top;
                Direction destinationDirection = Direction.Top;
                if (crossComponentData.TopStreet != Entity.Null && crossComponentData.TopStreet == previousStreet)
                {
                    sourceDirection = Direction.Top;
                }
                else if (crossComponentData.RightStreet != Entity.Null && crossComponentData.RightStreet == previousStreet)
                {
                    sourceDirection = Direction.Right;
                }
                else if (crossComponentData.BottomStreet != Entity.Null && crossComponentData.BottomStreet == previousStreet)
                {
                    sourceDirection = Direction.Bottom;
                }
                else if (crossComponentData.LeftStreet != Entity.Null && crossComponentData.LeftStreet == previousStreet)
                {
                    sourceDirection = Direction.Left;
                }
                else if (crossComponentData.CornerStreet != Entity.Null && crossComponentData.CornerStreet == previousStreet)
                {
                    sourceDirection = Direction.Corner;
                }
                else
                {
                    LogErrorFormat("The cross with id {0} is not linked to the incoming street of a car. Check that its CrossComponentData is consistent.", currentCross.Index);
                }


                if (crossComponentData.TopStreet != Entity.Null && crossComponentData.TopStreet == nextStreet)
                {
                    destinationDirection = Direction.Top;
                }
                else if (crossComponentData.RightStreet != Entity.Null && crossComponentData.RightStreet == nextStreet)
                {
                    destinationDirection = Direction.Right;
                }
                else if (crossComponentData.BottomStreet != Entity.Null && crossComponentData.BottomStreet == nextStreet)
                {
                    destinationDirection = Direction.Bottom;
                }
                else if (crossComponentData.LeftStreet != Entity.Null && crossComponentData.LeftStreet == nextStreet)
                {
                    destinationDirection = Direction.Left;
                }
                else if (crossComponentData.CornerStreet != Entity.Null && crossComponentData.CornerStreet == nextStreet)
                {
                    destinationDirection = Direction.Corner;
                }
                else
                {
                    LogErrorFormat("Cannot find the outgoing street of a track to assign to a car.");
                }

                var streetTrackComponentData = getTrackComponentData[carComponentData.Track];
                var buffer = getChildComponentData[currentCross];
                var trackToAssign = Entity.Null;
                var minimumRelativeTrackDistance = int.MaxValue;
                int currentRelativeTrackDistance;
                foreach (var trackChild in buffer)
                {
                    if (getTrackComponentData.HasComponent(trackChild.Value))
                    {
                        var crossTrackComponentData = getTrackComponentData[trackChild.Value];
                        currentRelativeTrackDistance = math.abs(streetTrackComponentData.relativeId - crossTrackComponentData.relativeId);
                        if (crossTrackComponentData.SourceDirection == sourceDirection && crossTrackComponentData.DestinationDirection == destinationDirection && currentRelativeTrackDistance < minimumRelativeTrackDistance)
                        {
                            trackToAssign = trackChild.Value;
                            minimumRelativeTrackDistance = currentRelativeTrackDistance;

                            if (minimumRelativeTrackDistance == 0) break;
                        }
                    }

                }
                if (trackToAssign == Entity.Null)
                {
                    //LogErrorFormat("The cross with id {0} doesn't contain a track with source direction {1} and destination direction {2}.", currentCross.Index, sourceDirection, destinationDirection);
                    entityManager.Debug.LogEntityInfo(currentCross);
                }

                carComponentData.isPathUpdated = true;
                carComponentData.Track = trackToAssign;
            }
            else if (carComponentData.isOnStreet && !carComponentData.isPathUpdated) // The car is passing from a cross to a street
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
                    LogFormat("The car with id {0} has reached its destination.", carEntity.Index);
                    /* The despawn will be carried out by another system */
                    carComponentData.HasReachedDestination = true;
                    return;
                }
                var currentCross = path.ElementAt(0).CrossOrStreet;
                var nextStreet = path.ElementAt(1).CrossOrStreet;
                path.RemoveAt(0);

                var street = nextStreet;
                var streetComponentData = getStreetComponentData[street];
                //var trackCandidatesName = "";
                bool isForward = false;
                if (streetComponentData.startingCross == currentCross)
                {
                    isForward = true;
                }
                else if (streetComponentData.endingCross == currentCross)
                {
                    isForward = false;
                }
                else
                {
                    LogErrorFormat("The street with id {0} is not linked to the cross with id {1} due to some inconsistency.", street.Index, currentCross.Index);
                }

                var lanes = getChildComponentData[street];
                var trackToAssign = Entity.Null;
                var minimumRelativeTrackDistance = int.MaxValue;
                var crossTrack = carComponentData.Track;
                var crossTrackComponentData = getTrackComponentData[crossTrack];
                int currentRelativeTrackDistance;
                foreach (var lane in lanes)
                {
                    if (getLaneComponentData.HasComponent(lane.Value))
                    {
                        var streetTrack = getChildComponentData[lane.Value][0].Value;
                        var streetTrackComponentData = getTrackComponentData[streetTrack];
                        currentRelativeTrackDistance = math.abs(crossTrackComponentData.relativeId - streetTrackComponentData.relativeId);
                        if (streetTrackComponentData.IsForward == isForward && currentRelativeTrackDistance < minimumRelativeTrackDistance)
                        {
                            trackToAssign = getChildComponentData[lane.Value][0].Value;
                            minimumRelativeTrackDistance = currentRelativeTrackDistance;

                            if (minimumRelativeTrackDistance == 0) break;
                        }
                    }
                }

                if (trackToAssign == Entity.Null)
                {
                    LogErrorFormat("No admissible tracks in a street are available for a car.");
                }
                carComponentData.isPathUpdated = true;
                carComponentData.Track = trackToAssign;

            }

        }).WithStructuralChanges().Run();

    }
}
