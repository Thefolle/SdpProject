using Unity.Entities;
using Unity.Physics;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics.Systems;
using static UnityEngine.Debug;
using Unity.Collections;

[GenerateAuthoringComponent]
public struct CrossComponentData : IComponentData
{
    /// <summary>
    /// <para>This parameter has the only purpose of filling this component data so that it is not empty anymore.
    /// The entity manager indeed may throw errors when try to get an empty component data from an entity.</para>
    /// </summary>
    private readonly int pad;

    /// <summary>
    /// <para>This representation of adjacent streets is not scalable, unlike a vector. However, a vector would lose
    /// topological associations.</para>
    /// </summary>
    public Entity TopStreet;
    public Entity RightStreet;
    public Entity BottomStreet;
    public Entity LeftStreet;
    public Entity CornerStreet;

}
