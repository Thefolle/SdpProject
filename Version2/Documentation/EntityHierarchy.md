# Entity hierarchy

- Cross
  - Bends (svolte): each bend has two entities, that is the incoming and the outgoing streets

- Street : each street has two entities, that is the incoming and the outgoing cross; the border streets have only one entity
  - ForwardLanes
  - BackwardLanes: if the street is one-way, this fields is absent
  - ForwardTrack
  - BackwardTrack: if the street is one-way, this fields is absent

- Car
