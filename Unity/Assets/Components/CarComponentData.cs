using System.Collections;
using System.Collections.Generic;
using Unity.Entities;

[GenerateAuthoringComponent]
public struct CarComponentData : IComponentData
{
    /// <summary>
    /// <para>The speed at which cars move.</para>
    /// </summary>
    public float maxSpeed;

    /// <summary>
    /// <para>The id of the node along <seealso cref="Track"/></para>
    /// </summary>
    public int SplineId;
    /// <summary>
    /// <para>The time instant at which the last node was reached.</para>
    /// </summary>
    public double splineReachedAtTime;

    /// <summary>
    /// <para>The track that the car is following.</para>
    /// </summary>
    public Entity Track;

    /// <summary>
    /// <para>When a car is moving from a street to a cross or viceversa, its path need to be updated.
    /// Since this passage takes multiple frames, but the update is required once,
    /// this flag stores whether the update has been performed or not.</para>
    /// </summary>
    public bool isPathUpdated;
    /// <summary>
    /// <para>A flag for internal usage. Coordinates the work of TrackAssignerSystem with SplineVehicleMovementSystem.</para>
    /// </summary>
    public bool needToUpdatedPath;
    /// <summary>
    /// <para>The initial node of the spline along which the car is moving.</para>
    /// </summary>
    public Entity splineStart;
    /// <summary>
    /// <para>The final node of the spline along which the car is moving.</para>
    /// </summary>
    public Entity splineEnd;

    /// <summary>
    /// <para>Flag that is true for the first frame after a car has been spawned.</para>
    /// </summary>
    public bool HasJustSpawned;
    /// <summary>
    /// <para>Flag that stores whether a car has reached its destination.</para>
    /// </summary>
    public bool HasReachedDestination;

    /// <summary>
    /// <para>Flag that is true when the car is currently on a street or on a cross.</para>
    /// </summary>
    public bool isOnStreet;

    public bool isOccupying;
}
