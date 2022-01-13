using System.Collections;
using System.Collections.Generic;
using Unity.Entities;

[GenerateAuthoringComponent]
public struct TrafficLightCrossComponentData : IComponentData
{
    /// <summary>
    /// <para>The turn that indicates which semaphore is green in a given cross.</para>
    /// </summary>
    public int greenTurn;
}