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

    /// <summary>
    /// <para>Convenience flag that stores whether the car is currently on a cross or on a street.</para>
    /// </summary>
    public bool ImInCross;

    // Spawning system variables
    public bool HasJustSpawned;
}
