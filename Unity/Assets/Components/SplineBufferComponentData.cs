using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

[GenerateAuthoringComponent]
[InternalBufferCapacity(60)]
public struct SplineBufferComponentData : IBufferElementData
{
    public Entity spline;
}
