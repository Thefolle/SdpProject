using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;

public class CarMovementSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var deltaTime = Time.DeltaTime;

        Entities.ForEach((CarComponentData carComponentData, LocalToWorld localToWorld, ref PhysicsVelocity physicsVelocity) =>
        {
            var tmp = physicsVelocity.Linear.y;
            physicsVelocity.Linear += localToWorld.Up * deltaTime / 10;
            physicsVelocity.Linear.y = tmp;
        })
            .Run();
    }
}
