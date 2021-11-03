using System.Collections;
using System.Collections.Generic;
using Unity.Entities;

public class TrackAssignerSystem : SystemBase
{
    protected override void OnUpdate()
    {
        /*
        var getBufferFromEntity = GetBufferFromEntity<PathComponentData>();
        var getCrossComponentData = GetComponentDataFromEntity<CrossComponentData>();
        var getStreetComponentData = GetComponentDataFromEntity<StreetComponentData>();

        var graph = World.GetExistingSystem<GraphGeneratorSystem>().District;

        Entities.ForEach((Entity carEntity, ref CarComponentData carComponentData) =>
        {
            if (carComponentData.EndOfTrackReached)
            {
                // TODO: probe if the car is on a cross or a street through a downward raycast

                carComponentData.EndOfTrackReached = false;

                var path = getBufferFromEntity[carEntity];
                var currentCrossId = path.ElementAt(0).crossId;
                var nextCrossId = path.ElementAt(1).crossId;
                path.RemoveAt(0);

                var currentCross = graph.GetNode(currentCrossId).Cross;

                /* Compute the track id to assign */
                
    /*
                var crossComponentData = getCrossComponentData[currentCross];
                if (crossComponentData.TopStreet != Entity.Null)
                {
                    var topStreetComponentData = getStreetComponentData[crossComponentData.TopStreet];
                    var otherNode = -1;
                    if (topStreetComponentData.startingCross.Index == approachingCross.Index)
                    {
                        otherNode = topStreetComponentData.endingCross.Index;
                    }
                    else
                    {
                        otherNode = topStreetComponentData.startingCross.Index;
                    }

                    if (otherNode == nextCrossId)
                    {
                        direction = "top";
                    }
                }

            }
        }).Run();
    */
    }
}
