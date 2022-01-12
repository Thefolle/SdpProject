using Unity.Entities;

[GenerateAuthoringComponent]
public struct TrafficLightComponentData : IComponentData
{
    /// <summary>
    /// <para>True if the traffic light is green, false otherwise.</para>
    /// </summary>
    public bool isGreen;
    /// <summary>
    /// <para>The last node of the spline in the street's track facing the traffic light, belonging to the first lane.</para>
    /// </summary>
    public Entity Spline1;
    /// <summary>
    /// <para>The last node of the spline in the street's track facing the traffic light, belonging to the second lane.</para>
    /// </summary>
    public Entity Spline2;
}


