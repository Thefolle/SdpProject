using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;

[GenerateAuthoringComponent]
public struct SplineComponentData : IComponentData
{
    /// <summary>
    /// <para>The id that uniquely identifies a node within a track. They are assigned increasingly as the streets proceeds down the street, starting from 0.</para>
    /// </summary>
    public int id;
    /// <summary>
    /// <para>Flag that tells whether the attached node is the last one of the track.</para>
    /// </summary>
    public bool isLast;
    /// <summary>
    /// <para>Flag that tells whether the current node is occupied by a certain car.</para>
    /// </summary>
    public bool isOccupied;


    /// <summary>
    /// <para>Flag that stores whether the pertinent lane is forward or backward.</para>
    /// </summary>
    public bool isForward;

    /// <summary>
    /// <para>The track to which the current node is attached to.</para>
    /// </summary>
    public Entity Track;
    /// <summary>
    /// <para>The car prefab, useful to spawn new cars when <see cref="isSpawner"/> is set.</para>
    /// </summary>
    public Entity carEntity;
    /// <summary>
    /// <para>The last car that have been spawned by the underlying node. Useful to configure the spawn strategy. Meaningful if <see cref="isSpawner"/> is set.</para>
    /// </summary>
    public Entity lastSpawnedCar;
    /// <summary>
    /// <para>The time instant at which the last car has been spawned.</para>
    /// </summary>
    public double lastTimeTriedToSpawn;

    /// <summary>
    /// <para>Flag that tells whether the node can spawn cars.</para>
    /// </summary>
    public bool isSpawner;
}
