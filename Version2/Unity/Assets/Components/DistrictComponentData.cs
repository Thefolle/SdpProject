using Unity.Entities;

[GenerateAuthoringComponent]
public struct DistrictComponentData : IComponentData
{
    /// <summary>
    /// <para>Component data should never be empty, although Unity optimizes the code for void components data. The community
    /// indeed signals some bugs that occur in that case.</para>
    /// </summary>
    private char Pad;
}