using System.Collections;
using System.Collections.Generic;
using Unity.Entities;

public class GraphGeneratorSystem : SystemBase
{
    protected override void OnCreate()
    {
        base.OnCreate();

        Graph district = new Graph();

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

    // map each node to its neighbours with the relative edges
    Dictionary<int, Dictionary<int, Edge>> adjacentNodes;
    
    public Graph Merge(Graph other)
    {
        return new Graph();
    }

    public void AddNode(Node node)
    {
        Nodes.Add(node.CrossId, node);
        adjacentNodes.Add(node.CrossId, new Dictionary<int, Edge>());
    }

    public void AddEdge(Edge edge)
    {

    }
}

public class Node
{
    public int CrossId { get; }

    // map a trackId with the couple street-street it links; it may contain different tracks with the same couple.
    public Dictionary<int, DictionaryEntry> tracksLinkage;

    public Node(int CrossId)
    {
        this.CrossId = CrossId;
    }
}

public class Edge
{
    public int StreetId { get; }

    public Edge(int StreetId)
    {
        this.StreetId = StreetId;
    }

    float Weight;
}
