using System.Collections;
using System.Collections.Generic;
using Unity.Entities;

[GenerateAuthoringComponent]
[InternalBufferCapacity(40)]
public struct BusStopLinesBuffer : IBufferElementData
{
    public int LineId;
}
