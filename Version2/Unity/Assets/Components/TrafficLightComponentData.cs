using Unity.Entities;

[GenerateAuthoringComponent]
public struct TrafficLightComponentData : IComponentData
{
    public bool isGreen;
    public Entity endSpline1;
    public Entity endSpline2;
}


