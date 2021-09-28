using System.Collections;
using System.Collections.Generic;
using Unity.Entities;

[GenerateAuthoringComponent]
public struct CarComponentData : IComponentData
{
    public float Speed;
    public float AngularSpeed;

    public int TrackId;
    /// <summary>
    /// This parameter is relative to the TrackId
    /// </summary>
    public int LaneId;
}
