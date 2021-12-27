using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;

[GenerateAuthoringComponent]
public struct TrackComponentData : IComponentData
{
    public bool allSplinesPlaced;
    public Entity splineEntity;
    public Entity carEntity;
}
