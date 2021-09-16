using System.Collections;
using System.Collections.Generic;
using Unity.Entities;

[GenerateAuthoringComponent]
public struct CarComponentData : IComponentData
{
    public float speed;
    public float angularSpeed;
}
