using Unity.Entities;

[GenerateAuthoringComponent]
public struct TrafficLightComponentData : IComponentData
{
    public bool isGreen;
    public Entity Spline1;
    public Entity Spline2;
}


