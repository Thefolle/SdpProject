using System.Collections;
using System.Collections.Generic;
using Unity.Entities;

[GenerateAuthoringComponent]
public struct TrafficLightCrossComponentData : IComponentData
{
    public double greenTurn;
}