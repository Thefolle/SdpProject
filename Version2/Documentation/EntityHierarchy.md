# Entity hierarchy

- Cross
  - Tracks
    - Track

- Street
  - ForwardLanes
    - TrackedLane
      - Track
  - BackwardLanes: if the street is one-way, this field is absent
    - TrackedLane
      - Track

- Car
