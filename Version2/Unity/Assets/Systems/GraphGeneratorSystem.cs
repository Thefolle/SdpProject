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

public class Graph
{
    // use of a dictionary to access node in O(1) given the id
    Dictionary<int, Node> Nodes;

    Dictionary<int, List<int>> adjacentNodes;

    int idGenerator = 0;

    int GetNewId()
    {
        return idGenerator++;
    }
    
    public Graph Merge(Graph other)
    {
        return new Graph();
    }

    public void AddNode(Node node)
    {
        Nodes.Add(GetNewId(), node);
    }
}

public class Node
{
    List<int> adjacentNodes;
}
