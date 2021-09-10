using System.Collections;
using System.Collections.Generic;
using Unity.Entities;

[GenerateAuthoringComponent]
public struct CarComponentData : IComponentData
{
    public bool hasVehicleUpfront;
    public int currentLane;
}
