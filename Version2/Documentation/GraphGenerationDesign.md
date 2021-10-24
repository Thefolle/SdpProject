# Graph generation design

A node in the graph represents a street, while an edge subtends to a cross. The alternative was that a node points to a cross, whereas an edge to a street.
The are advantages and disadvantages of the former approach w.r.t. the latter:

- A cross is naturally a more complex object than a street, so an edge fits better its behaviour;
- A cross may not permit all the incoming streets to continue in another street; in this scenario, the former model is simpler;
- Intuitively, a street should be mirrored by an edge, as well as a cross by a node.
