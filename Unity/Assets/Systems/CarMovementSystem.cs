using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Mathematics;
using Unity.Transforms;

public class CarMovementSystem : SystemBase
{
    private BuildPhysicsWorld buildPhysicsWorld;

    protected override void OnCreate()
    {
        base.OnCreate();

        buildPhysicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>();
    }

    protected override void OnUpdate()
    {

        var raycastHits = new NativeList<RaycastHit>(10, Allocator.TempJob);

        var deltaTime = Time.DeltaTime;

        PhysicsWorld physicsWorld = buildPhysicsWorld.PhysicsWorld;

        // movement job
        Dependency =
            Entities.ForEach((LocalToWorld localToWorld, Translation translation, ref PhysicsVelocity physicsVelocity, ref CarComponentData carComponentData) =>
            {
                if (carComponentData.hasVehicleUpfront)
                {
                    physicsVelocity.Linear = float3.zero;
                }
                else
                {
                    var tmp = physicsVelocity.Linear.y;
                    physicsVelocity.Linear = localToWorld.Forward * deltaTime * 100;
                    physicsVelocity.Linear.y = tmp;
                }
            }).Schedule(Dependency);

        // anti-rear-end collision job
        Dependency =
            Entities.ForEach((LocalToWorld localToWorld, Translation translation, ref PhysicsVelocity physicsVelocity, ref CarComponentData carComponentData) =>
            {
                var raycastInputForward = new RaycastInput
                {
                    Start = translation.Value,
                    End = translation.Value + 3 * localToWorld.Forward,
                    Filter = CollisionFilter.Default
                };

                bool isHitForward = physicsWorld.CastRay(raycastInputForward, ref raycastHits);

                // The raycast hits twice the car itself
                if (isHitForward && raycastHits.Length > 2)
                {
                    carComponentData.hasVehicleUpfront = true;
                }
                else
                {
                    carComponentData.hasVehicleUpfront = false;
                }
            }).Schedule(Dependency);

        raycastHits.Dispose(Dependency);

    }

}
