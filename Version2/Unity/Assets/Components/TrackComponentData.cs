using System.Collections;
using System.Collections.Generic;
using Unity.Entities;

[GenerateAuthoringComponent]
public struct TrackComponentData : IComponentData
{
    /// <summary>
    /// The id is local to the street
    /// </summary>
    public int id;

    /// <summary>
    /// <para>Why does this struct store entities rather than the corresponding ids? The purpose is leveraging
    /// the automatic creation of ids for Unity entities.</para>
    /// </summary>
    public Entity StartingEntity;
    public Entity EndingEntity;
}
