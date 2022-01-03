using Unity.Entities;

[GenerateAuthoringComponent]
public struct AskToDespawnComponentData : IComponentData
{
    public bool Asked;
}
