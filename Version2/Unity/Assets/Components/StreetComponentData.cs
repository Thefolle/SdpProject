using System.Collections;
using System.Collections.Generic;
using Unity.Entities;

[GenerateAuthoringComponent]
public struct StreetComponentData : IComponentData
{
    public bool IsOneWay;

    public Entity startingCross;
    public Entity endingCross;

    public bool allSplinesPlaced;
    public Entity splineEntity;

    /// <summary>
    /// <para>This field is a topological hint that allows dynamic linking of districts at runtime.</para>
    /// <para> exitNumber goes from 1 to 12 since we have 12 exits for each district:
    /// <list type="bullet">
    /// <item> 1 - 3: TOP respect to the global positive Z</item>
    /// <item> 4 - 6: RIGHT respect to the global positive Z</item>
    /// <item> 7 - 9: BOTTOM respect to the global positive Z</item>
    /// <item> 10 - 12: LEFT respect to the global positive Z</item>
    /// </list>
    /// </para>
    /// </summary>
    public int exitNumber;
    public bool IsBorder;
}
