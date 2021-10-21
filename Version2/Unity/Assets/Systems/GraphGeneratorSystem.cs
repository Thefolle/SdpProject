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
            var street = GetStreetFromTrack(track);
            if (street != Entity.Null)
            {
                district.AddNode(street.Index, new Node(entityManager.GetComponentData<StreetComponentData>(street)));
            } else
            {
                var cross = GetCrossFromTrack(track);
                if (cross != Entity.Null)
                {
                    /* The track belongs to a cross, which is translated to an edge */
                    if (entityManager.GetComponentData<TrackComponentData>(track).StartingEntity == Entity.Null) continue;
                    if (entityManager.GetComponentData<TrackComponentData>(track).EndingEntity == Entity.Null) continue;
                    var startingEntityId = entityManager.GetComponentData<TrackComponentData>(track).StartingEntity.Index;
                    var endingEntityId = entityManager.GetComponentData<TrackComponentData>(track).EndingEntity.Index;
                    district.AddEdge(
                        track.Index,
                        startingEntityId,
                        endingEntityId,
                        new Edge(entityManager.GetComponentData<CrossComponentData>(cross)) // added multiple times; not needed so far
                        );

                    // I may create something to add a cross once
                }
            }

            /* Inadvertitely the track has no parent */
            Log("The track with id " + track.Index + " has neither a street nor a cross as parent.");
        }

        Log(district.ToString());

        entities.Dispose();
    }

    private Entity GetStreetFromTrack(Entity track)
    {
        EntityManager entityManager = World.EntityManager;
        if (entityManager.HasComponent<Parent>(track))
        {
            var parent = entityManager.GetComponentData<Parent>(track).Value;
            if (entityManager.HasComponent<Parent>(parent))
            {
                var grandParent = entityManager.GetComponentData<Parent>(parent).Value;
                if (entityManager.HasComponent<StreetComponentData>(grandParent))
                {
                    /* The track belongs to a street, which is translated to a node */
                    return grandParent;
                }
            }
        }

        return Entity.Null;
    }

    private bool IsStreet(Entity track)
    {
        return GetStreetFromTrack(track) != Entity.Null;
    }

    private Entity GetCrossFromTrack(Entity track)
    {
        EntityManager entityManager = World.EntityManager;
        if (entityManager.HasComponent<Parent>(track))
        {
            var parent = entityManager.GetComponentData<Parent>(track).Value;
            if (entityManager.HasComponent<CrossComponentData>(parent))
            {
                return parent;
            }
        }

        return Entity.Null;
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
    
    /// <summary>
    /// <para>Merge two graphs.</para>
    /// </summary>
    /// <param name="other"></param>
    /// <returns><paramref name="this"/> graph modified in place.</returns>
    public Graph Merge(Graph other)
    {
        // get the two nodes to merge
        // create an edge between them through AddEdge
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
            /* Since there are multiple tracks per bend, pick only one of them */
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

    public bool IsOneWay(int startingNode, int endingNode)
    {
        if (AdjacentNodes.ContainsKey(endingNode))
        {
            if (AdjacentNodes[endingNode].Contains(startingNode))
            {
                return false;
            }
        }
        return true;
    }

    public override string ToString()
    {
        return "There are " + Nodes.Count + " nodes and " + Edges.Count + " edges in the graph.";
    }
}

public class Node
{
    public StreetComponentData Cross;

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
