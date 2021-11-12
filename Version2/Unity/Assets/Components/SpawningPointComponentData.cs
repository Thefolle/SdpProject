using System.Collections;
using System.Collections.Generic;
using Unity.Entities;

[GenerateAuthoringComponent]
public struct SpawningPointComponentData : IComponentData
{
    public Entity CarPrefab;
    /// <summary>
    /// <para>This field tells to the spawning point which is the street it is attached to.</para>
    /// <para>It is a convenient variable to avoid raycasting and escalating the hierarchy. In future, this data may be
    /// obtained from the hierarchy, if spawning points become part of streets in the hierarchy.</para>
    /// </summary>
    public Entity OwnerStreet;

    public float SpawningPeriod;
    public float CurrentTimeOffset;
}
