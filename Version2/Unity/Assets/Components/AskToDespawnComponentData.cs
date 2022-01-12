using Unity.Entities;

[GenerateAuthoringComponent]
public struct AskToDespawnComponentData : IComponentData
{
    /// <summary>
    /// <para>Whether the simulator has requested the attached entity to be destroyed.</para>
    /// </summary>
    public bool Asked;
}
