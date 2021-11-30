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

    // Spawning system variables
    public bool HasJustSpawned;
    public bool HasReachedDestination;

    // Cross system
    public bool myTrafficLight;
    public double lastTimeMyTrafficLight;

    public bool isOnStreet;
    public bool isOnCross;
    public bool isOnStreetAndCross;
}
