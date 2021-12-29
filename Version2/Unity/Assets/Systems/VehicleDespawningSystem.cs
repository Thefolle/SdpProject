using System.Collections;
using System.Collections.Generic;
using Unity.Entities;

/// <summary>
/// <para>This system acts as a garbage collector for vehicles that reached their destination by destroying them.</para>
/// </summary>
public class VehicleDespawningSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var entityManager = World.EntityManager;

        Entities.ForEach((in Entity carEntity, in CarComponentData carComponentData) =>
        {
            if (carComponentData.HasReachedDestination)
            {
                entityManager.DestroyEntity(carEntity);
            }
        }).WithStructuralChanges().Run();
    }
}
