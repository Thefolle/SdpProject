using System.Collections;
using System.Collections.Generic;
using Unity.Entities;

[GenerateAuthoringComponent]
public struct StreetComponentData : IComponentData
{
    public bool IsOneWay;
}
