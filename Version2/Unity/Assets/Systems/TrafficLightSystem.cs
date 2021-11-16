using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Physics;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics.Systems;
using static UnityEngine.Debug;
using Unity.Collections;

public class TrafficLightSystem : SystemBase
{
    protected override void OnUpdate()
    {
        float deltaTime = Time.DeltaTime;
        double elapsedTime = Time.ElapsedTime;
        float trafficLightTimeSwitch = 10f;


        PhysicsWorld physicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>().PhysicsWorld;
        EntityManager entityManager = World.EntityManager;
        var getTrackComponentDataFromEntity = GetComponentDataFromEntity<TrackComponentData>();
        var getCarComponentDataFromEntity = GetComponentDataFromEntity<CarComponentData>();
        var getParentComponentDataFromEntity = GetComponentDataFromEntity<Parent>();
        var getChildComponentDataFromEntity = GetBufferFromEntity<Child>();
        var getLaneComponentDataFromEntity = GetComponentDataFromEntity<LaneComponentData>();
        var getLocalToWorldComponentDataFromEntity = GetComponentDataFromEntity<LocalToWorld>();
        var getTrafficLightComponentDataFromEntity = GetComponentDataFromEntity<TrafficLightComponentData>();

        Entities.ForEach((Entity trafficLightCross, ref TrafficLightCrossComponentData TrafficLightCrossCompData) =>
        {
            var nOfTrafficLightsInCross = getChildComponentDataFromEntity[trafficLightCross].Length;
            var turn = (math.floor(elapsedTime / trafficLightTimeSwitch) % nOfTrafficLightsInCross);
            TrafficLightCrossCompData.greenTurn = turn;
            //LogError(TrafficLightCrossCompData.greenTurn + " is green");

            /*
            foreach (var trafficLight in getChildComponentDataFromEntity[trafficLightCross])
            {
                if (getTrafficLightComponentDataFromEntity.HasComponent(trafficLight.Value))
                {
                    var trafficLightNumber = entityManager.GetName(trafficLight.Value).Substring(entityManager.GetName(trafficLight.Value).LastIndexOf('-') + 1);
                    //LogError("trafficLightNumber: " + trafficLightNumber);
                    //var turn = (math.floor(elapsedTime / trafficLightTimeSwitch) % nOfTrafficLightsInCross);

                    var trafficLightComponentData = getTrafficLightComponentDataFromEntity[trafficLight.Value];
                    if (trafficLightNumber == turn.ToString())
                    {
                        //LogError(trafficLightNumber + " is green");
                        trafficLightComponentData.isGreen = true;
                    }
                    else
                    {
                        trafficLightComponentData.isGreen = false;
                    }
                }
            }*/
        }).Run();
    }
}
