using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using static UnityEngine.Debug;
using System;

public class GraphGeneratorSystem : SystemBase
{
    /// <summary>
    /// <para>This data structure is global and helps in navigating the city.</para>
    /// </summary>
    public Graph District;
    

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

        var now = DateTime.Now;
        LogFormat("The current time is {0} ({1})", now, now.Millisecond);
        var district = new Graph((int)now.Millisecond);

        foreach (Entity street in streets)
        {
            var streetComponentData = entityManager.GetComponentData<StreetComponentData>(street);
            if (streetComponentData.IsBorder) continue; // TODO
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

        //Log(district.ToString());

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
    Dictionary<int, Dictionary<int, Edge>> Edges;

    private int Seed;

    public Graph(int seed)
    {
        Nodes = new Dictionary<int, Node>();
        Edges = new Dictionary<int, Dictionary<int, Edge>>();
        Seed = seed;
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
        return new Graph(17);
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

    /// <summary>
    /// <para>Compute a random path receiving as parameter an edge represented by its initial end ending node.</para>
    /// </summary>
    /// <param name="edgeInitialNode">The initial node from where the edge starts.</param>
    /// <param name="edgeEndingNode">The final node to where the edge ends.</param>
    /// <returns>The list of cross ids that the path traverses.</returns>
    public List<int> RandomPath(int edgeInitialNode, int edgeEndingNode)
    {
        var path = new List<int>();
        int pathLength = 3;
        var randomGenerator = new Random(Seed);

        int currentNode = edgeEndingNode;
        int i = 0;
        path.Add(currentNode);
        i++;
        for (; i < pathLength; i++)
        {
            var possibleNextCrossIds = Edges[currentNode].Keys;
            Log("possible next cross ids: " + possibleNextCrossIds.Count);
            int j = 0;
            int randomJ = randomGenerator.Next(0, possibleNextCrossIds.Count - 1);
            Log("randomJ: " + randomJ);
            foreach(var nextCrossId in possibleNextCrossIds)
            {
                if (j == randomJ && !path.Contains(nextCrossId))
                {
                    path.Add(nextCrossId);
                    currentNode = nextCrossId;
                    Log("Next cross selected: " + nextCrossId);
                    break;
                }
                j++;
            }
        }

        return path;
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