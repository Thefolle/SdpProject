using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Physics;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics.Systems;
using static UnityEngine.Debug;
using Unity.Collections;

public class ObstacleMoveSystem : SystemBase
{
    protected override void OnCreate()
    {
        base.OnCreate();
        this.Enabled = false;
    }

    protected override void OnUpdate()
    {

        Entities.ForEach((ref Translation translation, in ObstaclesComponent obstaclesComponent, in Entity obstacle, in LocalToWorld localToWorld) =>
        {
            if(UnityEngine.Input.GetKey(UnityEngine.KeyCode.K))
            {
                translation.Value.y += 1;
            }
            if (UnityEngine.Input.GetKey(UnityEngine.KeyCode.M))
            {
                translation.Value.y -= 1;
            }
            if (UnityEngine.Input.GetKey(UnityEngine.KeyCode.W))
            {
                translation.Value.z += 1;
            }
            if (UnityEngine.Input.GetKey(UnityEngine.KeyCode.A))
            {
                translation.Value.x -= 1;
            }
            if (UnityEngine.Input.GetKey(UnityEngine.KeyCode.S))
            {
                translation.Value.z -= 1;
            }
            if (UnityEngine.Input.GetKey(UnityEngine.KeyCode.D))
            {
                translation.Value.x += 1;
            }


        }).Run();
    }
}
