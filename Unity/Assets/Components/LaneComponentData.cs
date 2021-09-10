using Unity.Entities;

[GenerateAuthoringComponent]
public struct LaneComponentData : IComponentData
{
    public int localToStreetId;
}
