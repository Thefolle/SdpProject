using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using static UnityEngine.Debug;

public class GraphGeneratorSystem : SystemBase
{
    protected override void OnStartRunning()
    {
        base.OnStartRunning();

        EntityManager entityManager = World.EntityManager;

        var entities = entityManager.GetAllEntities();
        //Log(entities.Length);
        var tracks = new List<Entity>();
        foreach (Entity entity in entities)
        {
            if (entityManager.HasComponent<TrackComponentData>(entity))
            {
                tracks.Add(entity);
            }
        }

        Graph district = new Graph();

        foreach (Entity track in tracks)
        {
            if (entityManager.HasComponent<Parent>(track))
            {
                var parent = entityManager.GetComponentData<Parent>(track).Value;
                if (entityManager.HasComponent<StreetComponentData>(parent))
                {
                    //Log("I'm a street");
                    /* The track belongs to a street, which is translated to a node */
                    district.AddNode(parent.Index, new Node(entityManager.GetComponentData<StreetComponentData>(parent)));
                } else if (entityManager.HasComponent<CrossComponentData>(parent))
                {
                    /* The track belongs to a cross, which is translated to an edge */
                    var startingEntityId = entityManager.GetComponentData<TrackComponentData>(track).StartingEntity.Index;
                    var endingEntityId = entityManager.GetComponentData<TrackComponentData>(track).EndingEntity.Index;
                    district.AddEdge(parent.Index, startingEntityId, endingEntityId, new Edge(entityManager.GetComponentData<CrossComponentData>(parent)));
                } else
                {
                    /* Inadvertitely the track has no parent */
                    LogError("The track with id " + track.Index + " has neither a street nor a cross as parent.");
                }
            }
        }

        UnityEngine.Debug.Log(district.ToString());

        entities.Dispose();
    }

    protected override void OnUpdate()
    {
        // Must remain empty
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
    Dictionary<int, Edge> Edges;

    // map each node to its neighbours
    Dictionary<int, List<int>> AdjacentNodes;

    public Graph()
    {
        Nodes = new Dictionary<int, Node>();
        Edges = new Dictionary<int, Edge>();
        AdjacentNodes = new Dictionary<int, List<int>>();
    }
    
    public Graph Merge(Graph other)
    {
        return new Graph();
    }

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
    public void AddEdge(int edgeId, int startingNode, int endingNode, Edge edge)
    {
        if (AdjacentNodes.ContainsKey(startingNode) && AdjacentNodes[startingNode].Contains(endingNode))
        {
            /* Since there are two physical tracks per bend, neglect one of them */
            return;
        }
        Edges.Add(edgeId, edge);
        if (!AdjacentNodes.ContainsKey(startingNode))
        {
            AdjacentNodes.Add(startingNode, new List<int>());
        }
        if (!AdjacentNodes[startingNode].Contains(endingNode))
        {
            AdjacentNodes[startingNode].Add(endingNode);
        }
    }

    public override string ToString()
    {
        return "There are " + Nodes.Count + " nodes and " + Edges.Count + " edges in the graph.";
    }
}

public class Node
{
    public StreetComponentData Cross;

    // map a trackId with the couple street-street it links; it may contain different tracks with the same couple.
    public Dictionary<int, DictionaryEntry> tracksLinkage;

    public Node(StreetComponentData cross)
    {
        Cross = cross;
    }
}

public class Edge
{
    public CrossComponentData Street;

    public Edge(CrossComponentData street)
    {
        Street = street;
    }
}
