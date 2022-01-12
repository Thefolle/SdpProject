using System.Collections;
using System.Collections.Generic;
using Unity.Entities;

[GenerateAuthoringComponent]
public struct LaneComponentData : IComponentData
{
    /// <summary>
    /// <para>Component data should never be empty, although Unity optimizes the code for void components data. The community
    /// indeed signals some bugs that occur in that case.</para>
    /// </summary>
    private char Pad;
}
