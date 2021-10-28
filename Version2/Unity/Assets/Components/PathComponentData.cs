using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

[InternalBufferCapacity(40)]
[GenerateAuthoringComponent]
public struct PathComponentData : IBufferElementData
{
    public int crossId;
}
