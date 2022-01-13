using System.Collections;
using System.Collections.Generic;
using Unity.Entities;

[GenerateAuthoringComponent]
public struct SpawningPointComponentData : IComponentData
{
    public Entity CarPrefab;

    public Entity LastSpawnedCar;
}
