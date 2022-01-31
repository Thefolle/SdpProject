using System.Collections;
using System.Collections.Generic;
using Unity.Entities;

[GenerateAuthoringComponent]
[InternalBufferCapacity(40)]
public struct EntityIndexBuffer : IBufferElementData
{
    public Entity Track;
}
