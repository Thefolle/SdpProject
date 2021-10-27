using System.Collections;
using System.Collections.Generic;
using Unity.Entities;

[GenerateAuthoringComponent]
public struct CarComponentData : IComponentData
{
    public float Speed;
    public float AngularSpeed;
    public float maxSpeed;
    public int TrackId;

    /// <summary>
    /// <para>This field is a queue that stores the adjacent streets and crosses to follow in the near future.</para>
    /// </summary>
}
