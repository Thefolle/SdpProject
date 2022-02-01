using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using static UnityEngine.Debug;

/// <summary>
/// <para>This system acts as a garbage collector for vehicles that reached their destination by destroying them.</para>
/// <para>In more detail, this system destroys the whole entity hierarchy given a root entity. The whole operation is performed in one frame, so as to keep the simulator in a consistent state.</para>
/// </summary>
public class DespawningSystem : SystemBase
{
    protected override void OnUpdate()
    {
        if (World.GetExistingSystem<EndInitializationBarrierSystem>().Enabled) return;

        var entityManager = World.EntityManager;

        Entities.ForEach((in AskToDespawnComponentData askToDespawnComponentData, in Entity entity) =>
        {
            if(askToDespawnComponentData.Asked)
            {
                if(entityManager.HasComponent<CarComponentData>(entity))
                {
                    //World.GetExistingSystem<SplineVehicleSpawnerSystem>().currentVehicleNumber--;
                    Globals.currentVehicleNumber--;
                    Globals.numberDespawnedVehicles++;
                    Globals.numberOfVehicleDespawnedInLastSecond++;
                    if(entityManager.GetComponentData<CarComponentData>(entity).IsBus)
                        Globals.currentBusNumber--;
                }

                var ecb = new EntityCommandBuffer(Allocator.TempJob); 

                DestroyHierarchy(ecb, entityManager, entity);
                

                ecb.Playback(entityManager);
                ecb.Dispose();

            }
                
        }).WithStructuralChanges().Run();
    }

    static void DestroyHierarchy(EntityCommandBuffer ecb, EntityManager entityManager, Entity entity)
    {
        if (entityManager.HasComponent<Child>(entity))
        {
            var children = entityManager.GetBuffer<Child>(entity);
            foreach (var child in children)
            {
                DestroyHierarchy(ecb, entityManager, child.Value);
            }
        }
        ecb.DestroyEntity(entity);
    }
}
