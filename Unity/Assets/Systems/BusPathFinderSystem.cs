using System.Collections;
using System.Collections.Generic;
using Unity.Entities;

public class BusPathFinderSystem : SystemBase
{
    protected override void OnUpdate()
    {
        if (World.GetExistingSystem<GraphGeneratorSystem>().Enabled) return;

        var entityManager = World.EntityManager;
        var graph = World.GetExistingSystem<GraphGeneratorSystem>().District;
        var getStreetComponentData = GetComponentDataFromEntity<StreetComponentData>();
        var lastStreet = Entity.Null;

        Entities.ForEach((in DynamicBuffer<BusStopLinesBuffer> busStopLinesBuffer, in Entity street, in StreetComponentData streetComponentData) =>
        {
            if (lastStreet == Entity.Null)
            {
                lastStreet = street;
            }
            else
            {
                var lastStreetComponentData = getStreetComponentData[lastStreet];
                var minimumPath = graph.MinimumPath(streetComponentData.startingCross.Index, streetComponentData.endingCross.Index, lastStreetComponentData.startingCross.Index, lastStreetComponentData.endingCross.Index); // pass just the ending cross of the initial street and the starting cross of the final street

                var streetComponentDataStartingCrossIndex = streetComponentData.startingCross.Index;
                var streetComponentDataEndingCrossIndex = streetComponentData.endingCross.Index;
                var busPath = entityManager.AddBuffer<PathComponentData>(street);
                if (!minimumPath.Exists(node => node.Cross.Index == streetComponentDataStartingCrossIndex)) // if the minimum path doesn't already traverse the initial node, include it
                {
                    busPath.Add(new PathComponentData { CrossOrStreet = graph.GetEdge(streetComponentData.startingCross.Index, streetComponentData.endingCross.Index).Street });
                }
                var lastStep = -1;
                bool isFirst = true;
                foreach (var node in minimumPath)
                {
                    if (isFirst)
                    {
                        if (!minimumPath.Exists(node => node.Cross.Index == streetComponentDataStartingCrossIndex)) // if the minimum path doesn't already traverse the initial node, include it
                        {
                            busPath.Add(new PathComponentData { CrossOrStreet = node.Cross });
                        }
                        
                        lastStep = node.Cross.Index;
                        isFirst = false;
                    }
                    else
                    {
                        busPath.Add(new PathComponentData { CrossOrStreet = graph.GetEdge(lastStep, node.Cross.Index).Street });
                        busPath.Add(new PathComponentData { CrossOrStreet = node.Cross });
                        lastStep = node.Cross.Index;
                    }
                }
                var lastStreetComponentDataStartingCrossIndex = lastStreetComponentData.startingCross.Index;
                var lastStreetComponentDataEndingCrossIndex = lastStreetComponentData.endingCross.Index;
                if (!minimumPath.Exists(node => node.Cross.Index == lastStreetComponentDataEndingCrossIndex)) // if the minimum path doesn't already traverse the initial node, include it
                {
                    busPath.Add(new PathComponentData { CrossOrStreet = graph.GetEdge(lastStreetComponentData.startingCross.Index, lastStreetComponentData.endingCross.Index).Street });
                    busPath.Add(new PathComponentData { CrossOrStreet = lastStreetComponentData.endingCross });
                }

                //entityManager.Debug.LogEntityInfo(street); // the starting street
                //entityManager.Debug.LogEntityInfo(lastStreet); // the ending street

                foreach (var step in busPath)
                {
                    entityManager.Debug.LogEntityInfo(step.CrossOrStreet);
                }
            }
        }).WithStructuralChanges().Run();

        this.Enabled = false;
    }
}