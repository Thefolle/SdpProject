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

    /// <summary>
    /// <para>This dictionary stores pairs (bus line number, path); for instance, it can be (10, {12, 14, 11, 23}).</para>
    /// <para>Notice that the path doesn't have to be a cycle or a simple path.</para>
    /// </summary>
    Dictionary<int, List<int>> BusRoute;

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

            for (int j = 0; j < possibleNextCrossIds.Count; j++, randomJ = (randomJ + 1) % possibleNextCrossIds.Count)
            {
                if (!pathInt[pathInt.Count - 2].Equals(possibleNextCrossIds[randomJ]))
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

            possibleNextCrossIds.Clear();
        }
        

        var path = new List<Node>();
        foreach (var nodeInt in pathInt)
        {
            path.Add(GetNode(nodeInt));
        }

        return path;
    }

    /// <summary>
    /// <para>Compute the minimum path between two edges.</para>
    /// <para>The complexity of this function is O(|V||E|).</para>
    /// </summary>
    /// <param name="startEdgeInitialNode"></param>
    /// <param name="startEdgeEndingNode"></param>
    /// <param name="finalEdgeInitialNode"></param>
    /// <param name="finalEdgeEndingNode"></param>
    /// <returns></returns>
    public List<Node> MinimumPath(int startEdgeInitialNode, int startEdgeEndingNode, int finalEdgeInitialNode, int finalEdgeEndingNode)
    {
        /* Compute the minimum path through the Bellman-Ford algorithm */
        var st = new List<int>();
        var d = new List<int>();
        var map = new List<int>(); // maps each index to the corresponding node id
        int i = 0;
        foreach(var nodeInt in Nodes.Keys)
        {
            map.Add(nodeInt);
            st.Add(-1);
            d.Add(int.MaxValue);
            i++;
        }
        d[map.IndexOf(startEdgeEndingNode)] = 0;
        st[map.IndexOf(startEdgeEndingNode)] = map.IndexOf(startEdgeEndingNode);
        var possibleNextNodeIds = new List<int>();

        for (int w = 0; w < Nodes.Count - 1; w++)
        {
            for (int v = 0; v < Nodes.Count; v++)
            {
                if (d[v] < int.MaxValue)
                {
                    possibleNextNodeIds.AddRange(Edges[map[v]].Keys);
                    foreach (var possibleNextCrossId in possibleNextNodeIds)
                    {
                        if (d[map.IndexOf(possibleNextCrossId)] > d[v] + 1)
                        {
                            d[map.IndexOf(possibleNextCrossId)] = d[v] + 1;
                            st[map.IndexOf(possibleNextCrossId)] = v;
                        }
                    }
                    possibleNextNodeIds.Clear();
                }
            }
        }

        var pathInt = new List<int>();
        //pathInt.Add(finalEdgeEndingNode);
        //pathInt.Add(finalEdgeInitialNode);
        var currentNode = map.IndexOf(finalEdgeInitialNode);
        while (currentNode != map.IndexOf(startEdgeEndingNode))
        {
            pathInt.Add(map[currentNode]);
            currentNode = st[currentNode];
        }
        //if (!pathInt.Contains(finalEdgeEndingNode)) pathInt.Add(finalEdgeEndingNode);

        pathInt.Add(startEdgeEndingNode);
        //pathInt.Add(startEdgeInitialNode);

        pathInt.Reverse(); // the st vector returns the minimum path in reverse order

        //if (!pathInt.Contains(startEdgeEndingNode)) pathInt.Add(startEdgeEndingNode);


        /* Translate from int to Node */
        var path = new List<Node>();
        foreach (var nodeInt in pathInt)
        {
            path.Add(GetNode(nodeInt));
        }

        return path;
    }

    /// <summary>
    /// <para>Extract an edge from the graph. If the edge is not directed (i.e. an edge exists for both directions), just one of them is extracted.</para>
    /// </summary>
    /// <param name="startingNodeId"></param>
    /// <param name="endingNodeId"></param>
    /// <returns></returns>
    public Edge ExtractEdge(int startingNodeId, int endingNodeId)
    {
        var edge = GetEdge(startingNodeId, endingNodeId);
        if (edge == null) return null;
        else
        {
            Edges[startingNodeId].Remove(endingNodeId);
            if (Edges[startingNodeId].Count == 0) Edges.Remove(startingNodeId);
            return edge;
        }
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