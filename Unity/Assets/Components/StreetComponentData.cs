using System.Collections;
using System.Collections.Generic;
using Unity.Entities;

[GenerateAuthoringComponent]
public struct StreetComponentData : IComponentData
{
    /// <summary>
    /// <para>Flag that tells whether the street is one-way. Currently it is not used.</para>
    /// </summary>
    public bool IsOneWay;

    /// <summary>
    /// <para>The cross from which the street begins, keeping in mind that the car forward sense is its local Z axis.</para>
    /// </summary>
    public Entity startingCross;
    /// <summary>
    /// <para>The cross where the street ends, keeping in mind that the car forward sense is its local Z axis.</para>
    /// </summary>
    public Entity endingCross;

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
    /// <summary>
    /// <para>Flag that is true for border streets, i.e. when the street is intended to be linked with other districts.</para>
    /// </summary>
    public bool IsBorder;
}
