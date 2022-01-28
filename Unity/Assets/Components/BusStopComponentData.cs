using System.Collections;
using System.Collections.Generic;
using Unity.Entities;

[GenerateAuthoringComponent]
public struct BusStopComponentData : IComponentData
{
    public DynamicBuffer<PathComponentData> ForwardPath;
    public DynamicBuffer<PathComponentData> BackwardPath;
}
