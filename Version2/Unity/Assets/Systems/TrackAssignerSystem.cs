using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
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

        var graph = World.GetExistingSystem<GraphGeneratorSystem>().District;

        Entities.ForEach((Entity carEntity, ref CarComponentData carComponentData) =>
        {
            if (carComponentData.EndOfTrackReached)
            {
                carComponentData.EndOfTrackReached = false;
                // TODO: probe if the car is on a cross or a street through a downward raycast
                if (!carComponentData.ImInCross)
                {
                    var path = getBufferFromEntity[carEntity];
                    var currentCrossId = path.ElementAt(0).crossId;
                    var nextCrossId = path.ElementAt(1).crossId;

                    var currentCross = graph.GetNode(currentCrossId).Cross;

                    /* Compute the track id to assign:
                     * First, infer which is the incoming street;
                     * Second, infer which is the outgoing street;
                     */
                    var crossComponentData = getCrossComponentData[currentCross];
                    string trackToAssignName = "";
                    if (crossComponentData.TopStreet != Entity.Null && crossComponentData.TopStreet.Index == carComponentData.CrossOrStreet.Index)
                    {
                        trackToAssignName += "Top";
                    }
                    else if (crossComponentData.RightStreet != Entity.Null && crossComponentData.RightStreet.Index == carComponentData.CrossOrStreet.Index)
                    {
                        trackToAssignName += "Right";
                    }
                    else if (crossComponentData.BottomStreet != Entity.Null && crossComponentData.BottomStreet.Index == carComponentData.CrossOrStreet.Index)
                    {
                        trackToAssignName += "Bottom";
                    }
                    else if (crossComponentData.LeftStreet != Entity.Null && crossComponentData.LeftStreet.Index == carComponentData.CrossOrStreet.Index)
                    {
                        trackToAssignName += "Left";
                    }
                    else if (crossComponentData.CornerStreet != Entity.Null && crossComponentData.CornerStreet.Index == carComponentData.CrossOrStreet.Index)
                    {
                        trackToAssignName += "Corner";
                    }
                    else
                    {
                        LogErrorFormat("The cross with id {0} is not linked to the incoming street of a car. Check that its CrossComponentData is consistent.", currentCrossId);
                    }

                    trackToAssignName += "-";

                    if (crossComponentData.TopStreet != Entity.Null)
                    {
                        var topStreetComponentData = getStreetComponentData[crossComponentData.TopStreet];
                        var otherNodeId = -1;
                        if (topStreetComponentData.startingCross.Index == currentCrossId)
                        {
                            otherNodeId = topStreetComponentData.endingCross.Index;
                        }
                        else
                        {
                            otherNodeId = topStreetComponentData.startingCross.Index;
                        }

                        if (otherNodeId == nextCrossId)
                        {
                            trackToAssignName += "Top";
                        }
                    }
                    else if (crossComponentData.RightStreet != Entity.Null)
                    {
                        var rightStreetComponentData = getStreetComponentData[crossComponentData.RightStreet];
                        var otherNodeId = -1;
                        if (rightStreetComponentData.startingCross.Index == currentCrossId)
                        {
                            otherNodeId = rightStreetComponentData.endingCross.Index;
                        }
                        else
                        {
                            otherNodeId = rightStreetComponentData.startingCross.Index;
                        }

                        if (otherNodeId == nextCrossId)
                        {
                            trackToAssignName += "Right";
                        }
                    }
                    else if (crossComponentData.BottomStreet != Entity.Null)
                    {
                        var bottomStreetComponentData = getStreetComponentData[crossComponentData.BottomStreet];
                        var otherNodeId = -1;
                        if (bottomStreetComponentData.startingCross.Index == currentCrossId)
                        {
                            otherNodeId = bottomStreetComponentData.endingCross.Index;
                        }
                        else
                        {
                            otherNodeId = bottomStreetComponentData.startingCross.Index;
                        }

                        if (otherNodeId == nextCrossId)
                        {
                            trackToAssignName += "Bottom";
                        }
                    }
                    else if (crossComponentData.LeftStreet != Entity.Null)
                    {
                        var leftStreetComponentData = getStreetComponentData[crossComponentData.LeftStreet];
                        var otherNodeId = -1;
                        if (leftStreetComponentData.startingCross.Index == currentCrossId)
                        {
                            otherNodeId = leftStreetComponentData.endingCross.Index;
                        }
                        else
                        {
                            otherNodeId = leftStreetComponentData.startingCross.Index;
                        }

                        if (otherNodeId == nextCrossId)
                        {
                            trackToAssignName += "Left";
                        }
                    }
                    else if (crossComponentData.CornerStreet != Entity.Null)
                    {
                        var cornerStreetComponentData = getStreetComponentData[crossComponentData.CornerStreet];
                        var otherNodeId = -1;
                        if (cornerStreetComponentData.startingCross.Index == currentCrossId)
                        {
                            otherNodeId = cornerStreetComponentData.endingCross.Index;
                        }
                        else
                        {
                            otherNodeId = cornerStreetComponentData.startingCross.Index;
                        }

                        if (otherNodeId == nextCrossId)
                        {
                            trackToAssignName += "Corner";
                        }
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
                        LogErrorFormat("The cross with id {0} doesn't contain a track with name {1}", currentCrossId, trackToAssignName);
                    }

                    carComponentData.ImInCross = true;
                    carComponentData.CrossOrStreet = currentCross;
                    carComponentData.TrackId = trackToAssign.Index;
                    LogFormat("I've assigned track {0} to car with id {1}", carComponentData.TrackId, carEntity.Index);
                }
                else // The car is in a street now
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
                        LogFormat("The car has reached the destination.");
                        return;
                    }
                    var currentCrossId = path.ElementAt(0).crossId;
                    var nextCrossId = path.ElementAt(1).crossId;
                    path.RemoveAt(0);

                    var street = graph.GetEdge(currentCrossId, nextCrossId).Street;
                    var streetComponentData = getStreetComponentData[street];
                    var trackCandidatesName = "";
                    if (streetComponentData.startingCross.Index == currentCrossId)
                    {
                        trackCandidatesName += "ForwardLane";
                    }
                    else
                    {
                        trackCandidatesName += "BackwardLane";
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
                    } else
                    {
                        carComponentData.ImInCross = false;
                        carComponentData.CrossOrStreet = street;
                        carComponentData.TrackId = trackToFollow.Index;
                        LogFormat("I've assigned track {0} to car with id {1}", carComponentData.TrackId, carEntity.Index);
                    }
                }
            }
        }).WithoutBurst().Run();
    
    }
}
