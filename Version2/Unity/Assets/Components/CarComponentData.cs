using System.Collections;
using System.Collections.Generic;
using Unity.Entities;

[GenerateAuthoringComponent]
public struct CarComponentData : IComponentData
{
    public float maxSpeed;

    public int SplineId;
    //public bool isOccuping;
    public bool askToDespawn;
    public double splineReachedAtTime;

    public Entity Track;

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
}
