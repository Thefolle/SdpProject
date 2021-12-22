using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;

[GenerateAuthoringComponent]
public struct MidSplineComponentData : IComponentData
{
    public int id;
    public bool isOccupied;
    public Entity Track;
}
