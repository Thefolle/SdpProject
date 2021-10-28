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
    protected override void OnUpdate()
    {
        float deltaTime = Time.DeltaTime;
        double elapsedTime = Time.ElapsedTime;
        //if (elapsedTime < 2) return;

        PhysicsWorld physicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>().PhysicsWorld;
        EntityManager entityManager = World.EntityManager;
        var getTrackComponentDataFromEntity = GetComponentDataFromEntity<TrackComponentData>();
        var getCarComponentDataFromEntity = GetComponentDataFromEntity<CarComponentData>();
        var getParentComponentDataFromEntity = GetComponentDataFromEntity<Parent>();
        var getChildComponentDataFromEntity = GetBufferFromEntity<Child>();
        var getLaneComponentDataFromEntity = GetComponentDataFromEntity<LaneComponentData>();
        //var currentStreetHere = currentStreet;


        Entities.ForEach((Entity obstacle, LocalToWorld localToWorld, ref Translation translation, in ObstaclesComponent obstaclesComponent) =>
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
