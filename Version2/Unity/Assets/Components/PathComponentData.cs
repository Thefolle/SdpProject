using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

[InternalBufferCapacity(40)]
[GenerateAuthoringComponent]
public struct PathComponentData : IBufferElementData
{
    /// <summary>
    /// <para>Stores the i-th cross/street along the path assigned to cars.</para>
    /// </summary>
    public Entity CrossOrStreet;
}
