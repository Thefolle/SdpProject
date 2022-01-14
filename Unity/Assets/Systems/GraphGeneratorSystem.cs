using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using static UnityEngine.Debug;
using System;

[UpdateInGroup(typeof(InitializationSystemGroup))]
[UpdateAfter(typeof(ConvertToEntitySystem))]
public class GraphGeneratorSystem : SystemBase
{
    /// <summary>
    /// <para>This data structure is global and helps in navigating the city.</para>
    /// </summary>
    public Graph District;
    protected override void OnUpdate()
    {
        if (World.GetExistingSystem<DistrictPlacerSystem>().Enabled) return;

        EntityManager entityManager = World.EntityManager;

        var entities = entityManager.GetAllEntities();
        //Log(entities.Length);
        var streets = new List<Entity>();
        var crosses = new List<Entity>();
        foreach (Entity entity in entities)
        {
            if (entityManager.HasComponent<StreetComponentData>(entity))
            {
                streets.Add(entity);
            }
            else if (entityManager.HasComponent<CrossComponentData>(entity))
            {
                crosses.Add(entity);
            }
        }

        var now = DateTime.Now;

        var district = new Graph((int)now.Millisecond); // Don't set a static number here: streets and crosses have randomly-generated ids as well

        foreach (Entity street in streets)
        {
            var streetComponentData = entityManager.GetComponentData<StreetComponentData>(street);
            if (streetComponentData.IsBorder) continue;
            district.AddEdge(streetComponentData.startingCross.Index, streetComponentData.endingCross.Index, new Edge(street));
            if (!streetComponentData.IsOneWay)
            {
                district.AddEdge(streetComponentData.endingCross.Index, streetComponentData.startingCross.Index, new Edge(street));
            }
        }

        foreach (Entity cross in crosses)
        {
            var crossComponentData = entityManager.GetComponentData<CrossComponentData>(cross);
            if (crossComponentData.TopStreet == Entity.Null && crossComponentData.RightStreet == Entity.Null && crossComponentData.BottomStreet == Entity.Null && crossComponentData.LeftStreet == Entity.Null) continue;

            district.AddNode(cross.Index, new Node(cross));
        }

        District = district;

        entities.Dispose();

        this.Enabled = false;
    }
}

/// <summary>
/// <para>The Graph class implements a directed graph.</para>
/// <para>A node represents a street, whereas an edge stands for a bend in a cross.</para>
/// </summary>
public class Graph
{
    // use a dictionary to access node in O(1) given the id; moreover, treat nodes as integers now
    Dictionary<int, Node> Nodes;
    Dictionary<int, Dictionary<int, Edge>> Edges;

    private int Seed;
    private Random RandomGenerator;

    public Graph(int seed)
    {
        Nodes = new Dictionary<int, Node>();
        Edges = new Dictionary<int, Dictionary<int, Edge>>();
        Seed = seed;
        RandomGenerator = new Random(seed);
    }

    /// <summary>
    /// <para>Add the <paramref name="node"/> only if no other node with id <paramref name="nodeId"/> exists.</para>
    /// </summary>
    /// <param name="nodeId"></param>
    /// <param name="node"></param>
    public void AddNode(int nodeId, Node node)
    {
        if (!Nodes.ContainsKey(nodeId))
        {
            Nodes.Add(nodeId, node);
        }
    }

    /// <summary>
    /// <para>If the edge already exists, does nothing.</para>
    /// </summary>
    /// <param name="edgeId"></param>
    /// <param name="startingNode"></param>
    /// <param name="endingNode"></param>
    /// <param name="edge"></param>
    public void AddEdge(int startingNode, int endingNode, Edge edge)
    {
        if (Edges.ContainsKey(startingNode) && Edges[startingNode].ContainsKey(endingNode))
        {
            /* The edge already exists, so do nothing */
            return;
        }
        if (!Edges.ContainsKey(startingNode))
        {
            Edges.Add(startingNode, new Dictionary<int, Edge>());
        }
        if (!Edges[startingNode].ContainsKey(endingNode))
        {
            Edges[startingNode].Add(endingNode, edge);
        }
    }

    /// <summary>
    /// <para>Compute a random path receiving as parameter an edge represented by its initial end ending node.</para>
    /// </summary>
    /// <param name="edgeInitialNode">The initial node from where the edge starts.</param>
    /// <param name="edgeEndingNode">The final node to where the edge ends.</param>
    /// <returns>The list of nodes that the path traverses.</returns>
    public List<Node> RandomPath(int edgeInitialNode, int edgeEndingNode)
    {
        var pathInt = new List<int>();
        int pathLength = 5;

        int i = 0;
        pathInt.Add(edgeInitialNode);
        i++;
        pathInt.Add(edgeEndingNode);
        i++;
        int currentNode = edgeEndingNode;
        var possibleNextCrossIds = new List<int>();
        for (; i < pathLength; i++)
        {
            possibleNextCrossIds.AddRange(Edges[currentNode].Keys);
            int randomJ = RandomGenerator.Next(0, possibleNextCrossIds.Count);
            int nextCrossId = -1;
            //if (!pathInt.Contains(possibleNextCrossIds[randomJ]))
            //{
            //    nextCrossId = possibleNextCrossIds[randomJ];
            //}
            for (int j = 0; j < possibleNextCrossIds.Count; j++, randomJ = (randomJ + 1) % possibleNextCrossIds.Count)
            {
                if (!pathInt.Contains(possibleNextCrossIds[randomJ]))
                {
                    nextCrossId = possibleNextCrossIds[randomJ];
                    break;
                }
            }

            if (nextCrossId != -1)
            {
                pathInt.Add(nextCrossId);
                currentNode = nextCrossId;
            }

            //int j = 0;
            //foreach(var nextCrossId in possibleNextCrossIds)
            //{
            //    if (j == randomJ && !pathInt.Contains(nextCrossId))
            //    {
            //        pathInt.Add(nextCrossId);
            //        currentNode = nextCrossId;
            //        break;
            //    }
            //    j++;
            //}
            possibleNextCrossIds.Clear();
        }
        

        var path = new List<Node>();
        foreach (var nodeInt in pathInt)
        {
            path.Add(GetNode(nodeInt));
        }

        return path;
    }

    private int EdgeCount()
    {
        int count = 0;
        foreach (var adj in Edges)
        {
            count += adj.Value.Count;
        }

        return count;
    }

    public Node GetNode(int nodeId)
    {
        if (Nodes.ContainsKey(nodeId))
        {
            return Nodes[nodeId];
        } else
        {
            return null;
        }
        
    }

    public Edge GetEdge(int startingNodeId, int endingNodeId)
    {
        if (Edges.ContainsKey(startingNodeId) && Edges[startingNodeId].ContainsKey(endingNodeId))
        {
            return Edges[startingNodeId][endingNodeId];
        } else
        {
            return null;
        }
    }

    public override string ToString()
    {
        return "There are " + Nodes.Count + " nodes and " + EdgeCount() + " edges in the graph.";
    }
}

public class Node
{
    public Entity Cross;

    public Node(Entity cross)
    {
        Cross = cross;
    }
}

public class Edge
{
    public Entity Street;

    public Edge(Entity street)
    {
        Street = street;
    }
}