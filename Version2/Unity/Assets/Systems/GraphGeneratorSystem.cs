using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using static UnityEngine.Debug;
using System;

public class GraphGeneratorSystem : SystemBase
{
    protected override void OnStartRunning()
    {
        base.OnStartRunning();

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

        Graph district = new Graph();

        foreach (Entity street in streets)
        {
            district.AddNode(street.Index, new Node(entityManager.GetComponentData<StreetComponentData>(street)));
        }

        foreach (Entity cross in crosses)
        {
            /* 
             * should distinguish whether a certain track is actually present;
             * for now, assume no absent tracks.
             */
            var crossComponentData = entityManager.GetComponentData<CrossComponentData>(cross);
            if (crossComponentData.TopStreet == Entity.Null && crossComponentData.RightStreet == Entity.Null && crossComponentData.BottomStreet == Entity.Null && crossComponentData.LeftStreet == Entity.Null) continue;
            var topStreetIndex = crossComponentData.TopStreet.Index;
            var rightStreetIndex = crossComponentData.RightStreet.Index;
            var bottomStreetIndex = crossComponentData.BottomStreet.Index;
            var leftStreetIndex = crossComponentData.LeftStreet.Index;
            var cornerStreetIndex = crossComponentData.CornerStreet.Index;

            var adjacentStreetIndexes = new List<int>();
            /* Distinguish the varios types of crosses according to their adjacent streets */
            if (topStreetIndex != Entity.Null.Index) adjacentStreetIndexes.Add(topStreetIndex);
            if (rightStreetIndex != Entity.Null.Index) adjacentStreetIndexes.Add(rightStreetIndex);
            if (bottomStreetIndex != Entity.Null.Index) adjacentStreetIndexes.Add(bottomStreetIndex);
            if (leftStreetIndex != Entity.Null.Index) adjacentStreetIndexes.Add(leftStreetIndex);
            if (cornerStreetIndex != Entity.Null.Index) adjacentStreetIndexes.Add(cornerStreetIndex);

            foreach (var streetIndexOuter in adjacentStreetIndexes)
            {
                foreach (var streetIndexInner in adjacentStreetIndexes)
                {
                    if (streetIndexInner != streetIndexOuter)
                    {
                        district.AddEdge(streetIndexOuter, streetIndexInner, new Edge(crossComponentData));
                    }
                }
            }
            
        }

        //        var cross = GetCrossFromTrack(track);
        //        if (cross != Entity.Null)
        //        {
        //            /* The track belongs to a cross, which is translated to an edge */
        //            if (entityManager.GetComponentData<TrackComponentData>(track).StartingEntity == Entity.Null) continue;
        //            if (entityManager.GetComponentData<TrackComponentData>(track).EndingEntity == Entity.Null) continue;
        //            var startingStreet = GetStreetFromTrack(entityManager.GetComponentData<TrackComponentData>(track).StartingEntity);
        //            var endingStreet = GetStreetFromTrack(entityManager.GetComponentData<TrackComponentData>(track).EndingEntity);
        //            //var startingEntityId = entityManager.GetComponentData<TrackComponentData>(track).StartingEntity.Index;
        //            //var endingEntityId = entityManager.GetComponentData<TrackComponentData>(track).EndingEntity.Index;
        //            district.AddEdge(
        //                track.Index,
        //                startingStreet.Index,
        //                endingStreet.Index,
        //                new Edge(entityManager.GetComponentData<CrossComponentData>(cross)) // added multiple times; not needed so far
        //                );

        //            // I may create something to add a cross once
        //        }
        //    }

        //    /* Inadvertitely the track has no parent */
        //    Log("The track with id " + track.Index + " has neither a street nor a cross as parent.");
        //}

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
    Dictionary<int, Dictionary<int, Edge>> Edges;
  

    public Graph()
    {
        Nodes = new Dictionary<int, Node>();
        Edges = new Dictionary<int, Dictionary<int, Edge>>();
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

    public List<int> MinimumPath(int startingNode, int endingNode)
    {
        return new List<int>();
    }

    public bool IsOneWay(int startingNode, int endingNode)
    {
        //if (AdjacentNodes.ContainsKey(endingNode))
        //{
        //    if (AdjacentNodes[endingNode].Contains(startingNode))
        //    {
        //        return false;
        //    }
        //}
        //return true;
        return false;
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

    public override string ToString()
    {
        return "There are " + Nodes.Count + " nodes and " + EdgeCount() + " edges in the graph.";
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


  
class GFG
{
    // A utility function to find the
    // vertex with minimum distance
    // value, from the set of vertices
    // not yet included in shortest
    // path tree
    static int V = 9;
    int minDistance(int[] dist,
                    bool[] sptSet)
    {
        // Initialize min value
        int min = int.MaxValue, min_index = -1;

        for (int v = 0; v < V; v++)
            if (sptSet[v] == false && dist[v] <= min)
            {
                min = dist[v];
                min_index = v;
            }

        return min_index;
    }

    // A utility function to print
    // the constructed distance array
    void printSolution(int[] dist, int n)
    {
        Console.Write("Vertex     Distance "
                      + "from Source\n");
        for (int i = 0; i < V; i++)
            Console.Write(i + " \t\t " + dist[i] + "\n");
    }

    // Function that implements Dijkstra's
    // single source shortest path algorithm
    // for a graph represented using adjacency
    // matrix representation
    void dijkstra(int[,] graph, int src)
    {
        int[] dist = new int[V]; // The output array. dist[i]
                                 // will hold the shortest
                                 // distance from src to i

        // sptSet[i] will true if vertex
        // i is included in shortest path
        // tree or shortest distance from
        // src to i is finalized
        bool[] sptSet = new bool[V];

        // Initialize all distances as
        // INFINITE and stpSet[] as false
        for (int i = 0; i < V; i++)
        {
            dist[i] = int.MaxValue;
            sptSet[i] = false;
        }

        // Distance of source vertex
        // from itself is always 0
        dist[src] = 0;

        // Find shortest path for all vertices
        for (int count = 0; count < V - 1; count++)
        {
            // Pick the minimum distance vertex
            // from the set of vertices not yet
            // processed. u is always equal to
            // src in first iteration.
            int u = minDistance(dist, sptSet);

            // Mark the picked vertex as processed
            sptSet[u] = true;

            // Update dist value of the adjacent
            // vertices of the picked vertex.
            for (int v = 0; v < V; v++)

                // Update dist[v] only if is not in
                // sptSet, there is an edge from u
                // to v, and total weight of path
                // from src to v through u is smaller
                // than current value of dist[v]
                if (!sptSet[v] && graph[u, v] != 0 &&
                     dist[u] != int.MaxValue && dist[u] + graph[u, v] < dist[v])
                    dist[v] = dist[u] + graph[u, v];
        }

        // print the constructed distance array
        printSolution(dist, V);
    }

    // Driver Code
    public static void Main()
    {
        /* Let us create the example 
graph discussed above */
        int[,] graph = new int[,] { { 0, 4, 0, 0, 0, 0, 0, 8, 0 },
                                      { 4, 0, 8, 0, 0, 0, 0, 11, 0 },
                                      { 0, 8, 0, 7, 0, 4, 0, 0, 2 },
                                      { 0, 0, 7, 0, 9, 14, 0, 0, 0 },
                                      { 0, 0, 0, 9, 0, 10, 0, 0, 0 },
                                      { 0, 0, 4, 14, 10, 0, 2, 0, 0 },
                                      { 0, 0, 0, 0, 0, 2, 0, 1, 6 },
                                      { 8, 11, 0, 0, 0, 0, 1, 0, 7 },
                                      { 0, 0, 2, 0, 0, 0, 6, 7, 0 } };
        GFG t = new GFG();
        t.dijkstra(graph, 0);
    }
}