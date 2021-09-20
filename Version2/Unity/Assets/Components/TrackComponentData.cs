using System.Collections;
using System.Collections.Generic;
using Unity.Entities;

[GenerateAuthoringComponent]
public struct TrackComponentData : IComponentData
{
    /// <summary>
    /// The id is local to the street
    /// </summary>
    public int id;
}
