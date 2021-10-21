using System.Collections;
using System.Collections.Generic;
using Unity.Entities;

[GenerateAuthoringComponent]
public struct CrossComponentData : IComponentData
{
    /// <summary>
    /// <para>This parameter has the only purpose of filling this component data so that it is not empty anymore.
    /// The entity manager indeed may throw errors when try to get an empty component data from an entity.</para>
    /// </summary>
    private readonly int pad;
}
