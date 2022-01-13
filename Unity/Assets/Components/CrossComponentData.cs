using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using static UnityEngine.Debug;
using Unity.Collections;

[GenerateAuthoringComponent]
public struct CrossComponentData : IComponentData
{
    /// <summary>
    /// <para>This topological field links the current cross with the top street with respect to the local Z of the cross.</para>
    /// </summary>
    public Entity TopStreet;
    /// <summary>
    /// <para>This topological field links the current cross with the right street with respect to the local Z of the cross.</para>
    /// </summary>
    public Entity RightStreet;
    /// <summary>
    /// <para>This topological field links the current cross with the bottom street with respect to the local Z of the cross.</para>
    /// </summary>
    public Entity BottomStreet;
    /// <summary>
    /// <para>This topological field links the current cross with the left street with respect to the local Z of the cross.</para>
    /// </summary>
    public Entity LeftStreet;
    /// <summary>
    /// <para>This topological field links the current cross with the corner street with respect to the local Z of the cross.</para>
    /// </summary>
    public Entity CornerStreet;

}
