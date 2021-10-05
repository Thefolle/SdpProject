using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;

[GenerateAuthoringComponent]
public struct TrackComponentData : IComponentData
{
    /// <summary>
    /// <para></para>
    /// The id is local to the street
    /// </summary>
    [Obsolete("This parameter will be deleted in future snapshots, as the system will leverage the automatically-generated id of Unity")]
    public int id;

    /// <summary>
    /// <para>Why does this struct store entities rather than the corresponding ids? The purpose is leveraging
    /// the automatic creation of ids for Unity entities.</para>
    /// </summary>
    public Entity StartingEntity;
    public Entity EndingEntity;
}
