using Unity.Entities;

/// <summary>
/// <para>This component data may be turned into a buffer for higher scalability.</para>
/// </summary>
[GenerateAuthoringComponent]
public struct PrefabComponentData : IComponentData
{
    /// <summary>
    /// <para>A district prefab, manually dragged and dropped from the Unity editor.</para>
    /// </summary>
    public Entity District;
    /// <summary>
    /// <para>A district prefab, manually dragged and dropped from the Unity editor.</para>
    /// </summary>
    public Entity District2;
    /// <summary>
    /// <para>A district prefab, manually dragged and dropped from the Unity editor.</para>
    /// </summary>
    public Entity District3;
}
