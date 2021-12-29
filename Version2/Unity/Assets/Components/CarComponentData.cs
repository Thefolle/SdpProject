using System.Collections;
using System.Collections.Generic;
using Unity.Entities;

[GenerateAuthoringComponent]
public struct CarComponentData : IComponentData
{
    public float Speed;
    public float AngularSpeed;
    public float maxSpeed;

    // Fields for lane change system
    public bool tryOvertake;
    public double lastTimeTried;
    public bool rightOvertakeAllowed;

    public bool emergencyBrakeActivated;

    public int SplineId;
    public double splineReachedAtTime;
    public int elapsedFramesMovement;

    public int TrackId;
    public Entity Track;
    public bool IsTracked;

    /// <summary>
    /// <para>When a car is moving from a street to a cross or viceversa, its path need to be updated.
    /// Since this passage takes multiple frames, but the update is required once,
    /// this flag stores whether the update has been performed or not.</para>
    /// </summary>
    public bool isPathUpdated;
    public bool needToUpdatedPath;
    public Entity splineStart;

    // Spawning system variables
    public bool HasJustSpawned;
    public bool HasReachedDestination;

    public bool isOnStreet;

    public bool broken;
}
