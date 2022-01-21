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

    /// <summary>
    /// <para>Shortcut for cars that would like to change lane.</para>
    /// </summary>
    public Entity leftTrack;

    /// <summary>
    /// <para>The relative number that identifies the track within the context of a street.</para>
    /// <para>Given all the tracks of a street along a direction, this number by convention is 0 for the center track and increases by one for each track at its right.</para>
    /// </summary>
    public int relativeId;

    /// <summary>
    /// <para>Shortcut variables that tells whether the track has a street or a cross as parent.</para>
    /// </summary>
    public bool IsOnStreet;
    /// <summary>
    /// <para>Tells whether this track is along the forward direction or the backward direction.</para>
    /// <para>Note: this variable is meaningful only if <see cref="IsOnStreet"/> is true.</para>
    /// </summary>
    public bool IsForward;
    /// <summary>
    /// <para>Tells the source topological direction of a track.</para>
    /// <para>Note: this variable is meaningful only if <see cref="IsOnStreet"/> is false.</para>
    /// </summary>
    public Direction SourceDirection;
    /// <summary>
    /// <para>Tells the destination topological direction of a track.</para>
    /// <para>Note: this variable is meaningful only if <see cref="IsOnStreet"/> is false.</para>
    /// </summary>
    public Direction DestinationDirection;
}

public enum Direction
{
    Top,
    Right,
    Bottom,
    Left,
    Corner
}
