using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;

[GenerateAuthoringComponent]
public struct StartEndSplineComponentData : IComponentData
{
    public float speed;
    public bool Start;
    public Entity Track;
}
