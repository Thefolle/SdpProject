using System.Collections;
using System.Collections.Generic;
using Unity.Entities;

[GenerateAuthoringComponent]
public struct StreetComponentData : IComponentData
{
    public bool IsOneWay;

    /// <summary>
    /// <para>This field is a topological hint that allows dynamic linking of districts at runtime.</para>
    /// </summary>
    public bool IsBorder;
    public Side Side;
}

public enum Side {
    LEFT,
    TOP,
    RIGHT,
    BOTTOM
}
