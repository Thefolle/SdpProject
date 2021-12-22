using System.Collections;
using System.Collections.Generic;
using Unity.Entities;

public struct SplineCarComponentData : IComponentData
{
    public float Speed;
//  public float AngularSpeed;
    public float maxSpeed;

    public bool emergencyBrakeActivated;

    public int TrackId;
    public Entity Track;
    public bool IsTracked;

    public bool isPathUpdated;

    // Spawning system variables
    public bool HasJustSpawned;
    public bool HasReachedDestination;

    public bool broken;
}
