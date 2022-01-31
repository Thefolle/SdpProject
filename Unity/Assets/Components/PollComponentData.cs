using System.Collections;
using System.Collections.Generic;
using Unity.Entities;

[GenerateAuthoringComponent]
public struct PollComponentData : IComponentData
{
    /// <summary>
    /// <para>When a vehicle V1 is stuck due to some other vehicle V2 in front of it, V1 checks the occupation of V2 at each frame.
    /// This counter enables vehicles to poll for the front position not on every frame, but only when <see cref="Poll"/> is 0.</para>
    /// <para>When V1 finds that the next node is occupied, this counter is incremented. The car, then, does not perform another check
    /// of the next node until <see cref="Poll"/> is reset again.</para>
    /// </summary>
    public int Poll;
}
