using System.Collections;
using System.Collections.Generic;
using Unity.Entities;

/// <summary>
/// <para>This system acts as a garbage collector for vehicles that reached their destination by destroying them.</para>
/// </summary>
public class DespawningSystem : SystemBase
{
    protected override void OnUpdate()
    {
        if (World.GetExistingSystem<GraphGeneratorSystem>().Enabled) return;

        var entityManager = World.EntityManager;

        Entities.ForEach((in Entity entity, in AskToDespawnComponentData askToDespawnComponentData) =>
        {
            if(askToDespawnComponentData.Asked)
                entityManager.DestroyEntity(entity);
        }).WithStructuralChanges().Run();
    }
}