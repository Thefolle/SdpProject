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
        if (World.Time.ElapsedTime < 4 || World.GetExistingSystem<StreetSplinePlacerSystem>().Enabled) return;

        var graph = World.GetExistingSystem<GraphGeneratorSystem>().District;
        var getParentComponentData = GetComponentDataFromEntity<Parent>();
        var getStreetComponentData = GetComponentDataFromEntity<StreetComponentData>();
        var getBufferFromEntity = GetBufferFromEntity<PathComponentData>();
        var getCrossComponentData = GetComponentDataFromEntity<CrossComponentData>();
        var getChildComponentData = GetBufferFromEntity<Child>();
        var getTrackComponentData = GetComponentDataFromEntity<TrackComponentData>();
        EntityManager entityManager = World.EntityManager;

        Entities.ForEach((ref CarComponentData carComponentData, in Entity carEntity, in LocalToWorld localToWorld) =>
        {
            if (carComponentData.HasJustSpawned)
            {

                carComponentData.HasJustSpawned = false;

                /* Request a random path */
                var trackedLane = getParentComponentData[carComponentData.Track].Value;
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
                //carComponentData.TrackId = trackToAssign.Index;
                carComponentData.Track = trackToAssign;

                //LogFormat("I've assigned track {0} to car with id {1}", carComponentData.TrackId, carEntity.Index);
            }

        }).WithStructuralChanges().Run();

    }
}
