using Unity.Entities;

[GenerateAuthoringComponent]
public struct TrafficLightComponentData : IComponentData
{
    public bool isGreen;
    public Entity Spline1;
    public Entity Spline2;
    public Entity Spline3;
    public Entity Spline4;
    public Entity Spline5;
    public Entity Spline6;
    public Entity Spline7;
    public Entity Spline8;
}


