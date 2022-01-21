using System.Collections;
using System.Collections.Generic;
using Unity.Entities;

[GenerateAuthoringComponent]
public struct TrafficLightPanelComponentData : IComponentData
{
    public TrafficLightColor Color;
}

public enum TrafficLightColor
{
    Red,
    Green
}