# Graph generation design

## How to represent a district ?

A node in the graph represents a street, while an edge subtends to a cross.

### Motivations underneath this choice

The alternative was that a node pointed to a cross, whereas an edge to a street.
There are advantages of the chosen approach w.r.t. the alternative one:

- A cross is naturally a more complex object than a street, so an edge fits better its behaviour: a car in a street can only go straight;
- In a real scenario, not all the streets linked to a street can lead to another street. In formal terms, given a cross *c* linked to a set *S* of streets and a street *s1* belonging to S, then it may exist a street *s2* belonging to S such that s2 cannot lead to s1. The implementation of this possibility is feasible with the chosen approach, while the alternative one would promote the data structure to a more general type of graph, which would also store the possibility of a car to proceed toward an edge from a node depending on from which edge the car has come; the Dijkstra algorithm for the computation of shortest paths may not work anymore with this kind of generalized graph.

The disadvantage of mirroring a street with a node and a cross with a set of edges is that it is not an intuitive model.
