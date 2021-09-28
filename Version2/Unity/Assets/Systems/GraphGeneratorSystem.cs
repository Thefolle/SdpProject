using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;

public class GraphGeneratorSystem : SystemBase
{
    protected override void OnStartRunning()
    {
        base.OnStartRunning();

        EntityManager entityManager = World.EntityManager;
        var getTrackComponentDataFromEntity = GetComponentDataFromEntity<TrackComponentData>();
        var getParentComponentDataFromEntity = GetComponentDataFromEntity<Parent>();
        var getStreetComponentDataFromEntity = GetComponentDataFromEntity<StreetComponentData>();
        var getCrossComponentDataFromEntity = GetComponentDataFromEntity<CrossComponentData>();

        var entities = entityManager.GetAllEntities();
        var tracks = new List<Entity>();
        foreach (Entity entity in entities)
        {
            if (getTrackComponentDataFromEntity.HasComponent(entity))
            {
                tracks.Add(entity);
            }
        }
        entities.Dispose();

        Graph district = new Graph();

        foreach (Entity track in tracks)
        {
            var startingEntityId = getTrackComponentDataFromEntity[track].StartingEntity.Index;
            var endingEntityId = getTrackComponentDataFromEntity[track].EndingEntity.Index;
            if (getParentComponentDataFromEntity.HasComponent(track))
            {
                var parent = getParentComponentDataFromEntity[track].Value;
                if (getStreetComponentDataFromEntity.HasComponent(parent))
                {
                    /* The track belongs to a street, which is translated to an edge */
                    district.AddEdge(parent.Index, startingEntityId, endingEntityId, new Edge(getStreetComponentDataFromEntity[parent]));
                } else if (getCrossComponentDataFromEntity.HasComponent(parent))
                {
                    /* The track belongs to a cross, which is translated to a node */
                    district.AddNode(parent.Index, new Node(getCrossComponentDataFromEntity[parent]));
                } else
                {
                    /* Inadvertitely the track has no parent */
                    UnityEngine.Debug.LogError("The track with id " + track.Index + " has neither a street nor a cross as parent.");
                }
            }
        }

        

    }

    protected override void OnUpdate()
    {
        // Must remain empty
    }
}

/// <summary>
/// <para>The Graph class represents a directed graph.</para>
/// <para>A Graph is directed so as to model one-way streets. Therefore, this library always treats edges as directed.</para>
/// </summary>
public class Graph
{
    // use a dictionary to access node in O(1) given the id; moreover, treat nodes as integers now
    Dictionary<int, Node> Nodes;
    Dictionary<int, Edge> Edges;

    // map each node to its neighbours
    Dictionary<int, List<int>> AdjacentNodes;
    
    public Graph Merge(Graph other)
    {
        return new Graph();
    }

    public void AddNode(int nodeId, Node node)
    {

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

    }
}

public class Node
{
    public CrossComponentData Cross;

    // map a trackId with the couple street-street it links; it may contain different tracks with the same couple.
    public Dictionary<int, DictionaryEntry> tracksLinkage;

    public Node(CrossComponentData cross)
    {
        Cross = cross;
    }
}

public class Edge
{
    public StreetComponentData Street;

    public Edge(StreetComponentData street)
    {
        Street = street;
    }
}
