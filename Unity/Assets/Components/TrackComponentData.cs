using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;

[GenerateAuthoringComponent]
public struct TrackComponentData : IComponentData
{
    /// <summary>
    /// <para>True when all nodes belonging to the track has been assigned, at bootstrap.</para>
    /// </summary>
    public bool allSplinesPlaced;
    /// <summary>
    /// <para>The node prefab of splines.</para>
    /// </summary>
    public Entity splineEntity;
    /// <summary>
    /// <para>The car prefab used by node of splines that behave also as car spawners.</para>
    /// </summary>
    public Entity carEntity;

    public Entity leftTrack;
}
