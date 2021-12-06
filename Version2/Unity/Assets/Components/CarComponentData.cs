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

    public int TrackId;
    public Entity TrackParent;

    /// <summary>
    /// <para>Convenience variable that stores where the car is standing on.</para>
    /// </summary>
    public VehicleIsOn vehicleIsOn;
    /// <summary>
    /// <para>When a car is moving from a street to a cross or viceversa, its path need to be updated.
    /// Since this passage takes multiple frames, but the update is required once,
    /// this flag stores whether the update has been performed or not.</para>
    /// </summary>
    public bool isPathUpdated;

    // Spawning system variables
    public bool HasJustSpawned;
    public bool HasReachedDestination;

    public bool isOnStreet;
    public bool isOnCross;
    public bool isOnStreetAndCross;
}
