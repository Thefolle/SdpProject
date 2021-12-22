using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;

[GenerateAuthoringComponent]
public struct SplineComponentData : IComponentData
{
    public int id;
    public bool isLast;
    public bool isOccupied;
    public Entity Track;
}
