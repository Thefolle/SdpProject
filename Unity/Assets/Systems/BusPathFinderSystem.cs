using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Collections;
using static UnityEngine.Debug;

/// <summary>
/// <para>This system computes paths that buses follow. The system is run at initialization phase and immediately disabled.</para>
/// </summary>
[AlwaysUpdateSystem] // update even if there are no entities to process
public class BusPathFinderSystem : SystemBase
{
    protected override void OnUpdate()
    {
        if (World.GetExistingSystem<GraphGeneratorSystem>().Enabled) return;

        var entityManager = World.EntityManager;
        var graph = World.GetExistingSystem<GraphGeneratorSystem>().District;
        var getStreetComponentData = GetComponentDataFromEntity<StreetComponentData>();
        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        var lastStreet = Entity.Null; // the preceding bus stop of the bus route
        var firstStreet = Entity.Null; // the initial bus stop of the bus route

        

        Entities.ForEach((in BusStopComponentData busStopComponentData, in Entity street, in StreetComponentData streetComponentData) =>
        {
            if (lastStreet == Entity.Null)
            {
                lastStreet = street;
                firstStreet = street;
            }
            else
            {
                var lastStreetComponentData = getStreetComponentData[lastStreet];
                var firstStreetComponentData = getStreetComponentData[firstStreet];

                /* Compute the shortest path between the current street and the last-considered one */
                var startingStreetEdge = graph.ExtractEdge(streetComponentData.endingCross.Index, streetComponentData.startingCross.Index); // temporarily remove the backward edge to avoid a path that requires a U-shaped inversion of the bus
                var endingStreetEdge = graph.ExtractEdge(lastStreetComponentData.endingCross.Index, lastStreetComponentData.startingCross.Index); // temporarily remove the backward edge of the destination street so that the bus always reaches the destination street in the forward direction of the street
                SetMinimumPath(street, streetComponentData, graph, ecb, lastStreetComponentData);
                graph.AddEdge(streetComponentData.endingCross.Index, streetComponentData.startingCross.Index, startingStreetEdge);
                graph.AddEdge(lastStreetComponentData.endingCross.Index, lastStreetComponentData.startingCross.Index, endingStreetEdge);

                /* Compute the shortest path between the very first-considered street and the current one
                 * This evaluation is performed for each bus stop, although it is not necessary. The reason
                 * is that it is not possible to infer whether the current street is the last one to consider
                 * or not. The path assigned to the first street is therefore overwritten for each street.
                 */
                startingStreetEdge = graph.ExtractEdge(streetComponentData.endingCross.Index, streetComponentData.startingCross.Index); // temporarily remove the backward edge to avoid a path that requires a U-shaped inversion of the bus
                var firstStreetEdge = graph.ExtractEdge(firstStreetComponentData.endingCross.Index, firstStreetComponentData.startingCross.Index);  // temporarily remove the backward edge of the destination street so that the bus always reaches the destination street in the forward direction of the street
                SetMinimumPath(firstStreet, firstStreetComponentData, graph, ecb, streetComponentData); // overwrite the first street path for each iteration, since cannot infer whether this is the last street of Entities.ForEach
                graph.AddEdge(streetComponentData.endingCross.Index, streetComponentData.startingCross.Index, startingStreetEdge);
                graph.AddEdge(firstStreetComponentData.endingCross.Index, firstStreetComponentData.startingCross.Index, firstStreetEdge);

                lastStreet = street;
            }

            Globals.numberOfBusStops++;

        }).WithStructuralChanges().Run();

        ecb.Playback(entityManager);
        ecb.Dispose();

        if (Globals.numberOfBusStops < 2)
            Log("There is only one bus stop in this simulation. A bus needs at least two stops in order to spawn. No bus will be generated.");

        this.Enabled = false;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="street">The source street.</param>
    /// <param name="streetComponentData"></param>
    /// <param name="graph"></param>
    /// <param name="ecb"></param>
    /// <param name="lastStreetComponentData">The destination street component data.</param>
    private static void SetMinimumPath(Entity street, StreetComponentData streetComponentData, Graph graph, EntityCommandBuffer ecb, StreetComponentData lastStreetComponentData)
    {
        var minimumPath = graph.MinimumPath(streetComponentData.startingCross.Index, streetComponentData.endingCross.Index, lastStreetComponentData.startingCross.Index, lastStreetComponentData.endingCross.Index);

        var streetComponentDataStartingCrossIndex = streetComponentData.startingCross.Index;
        var streetComponentDataEndingCrossIndex = streetComponentData.endingCross.Index;
        var busPath = ecb.AddBuffer<PathComponentData>(street);
        //if (!minimumPath.Exists(node => node.Cross.Index == streetComponentDataStartingCrossIndex)) // if the minimum path doesn't already traverse the initial node, include it
        //{
            busPath.Add(new PathComponentData { CrossOrStreet = street });
        //}
        var lastStep = -1;
        bool isFirst = true;
        foreach (var node in minimumPath)
        {
            if (isFirst)
            {
                //if (!minimumPath.Exists(node => node.Cross.Index == streetComponentDataStartingCrossIndex)) // if the minimum path doesn't already traverse the initial node, include it
                //{
                    busPath.Add(new PathComponentData { CrossOrStreet = node.Cross });
                //}

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

    }
}