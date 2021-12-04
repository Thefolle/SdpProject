using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Physics;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics.Systems;
using static UnityEngine.Debug;
using Unity.Collections;

public static class GlobalVariables
{
    public static float trafficLightTimeSwitch = 10f;
}

public class TrafficLightSystem : SystemBase
{
    protected override void OnUpdate()
    {
        float deltaTime = Time.DeltaTime;
        double elapsedTime = Time.ElapsedTime;
        if (elapsedTime < 2) return;


        PhysicsWorld physicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>().PhysicsWorld;
        EntityManager entityManager = World.EntityManager;
        var getTrackComponentDataFromEntity = GetComponentDataFromEntity<TrackComponentData>();
        var getCarComponentDataFromEntity = GetComponentDataFromEntity<CarComponentData>();
        var getParentComponentDataFromEntity = GetComponentDataFromEntity<Parent>();
        var getChildComponentDataFromEntity = GetBufferFromEntity<Child>();
        var getLaneComponentDataFromEntity = GetComponentDataFromEntity<LaneComponentData>();
        var getLocalToWorldComponentDataFromEntity = GetComponentDataFromEntity<LocalToWorld>();
        var getTrafficLightComponentDataFromEntity = GetComponentDataFromEntity<TrafficLightComponentData>();

        /* Ottimizzazione: chiamo la funzione solo quando serve aggiornare il "turn", quindi attorno ad un secondo 
         * prima fino ad un secondo dopo lo switch (2 secondi di delta per sicurezza)
         */

        if (((elapsedTime/GlobalVariables.trafficLightTimeSwitch) % 1) < 0.1 || ((elapsedTime / GlobalVariables.trafficLightTimeSwitch) % 1) > 0.9)
            Entities.ForEach((ref TrafficLightCrossComponentData TrafficLightCrossCompData, in Entity trafficLightCross) =>
            {
                if (getChildComponentDataFromEntity.HasComponent(trafficLightCross))
                {
                    var nOfTrafficLightsInCross = getChildComponentDataFromEntity[trafficLightCross].Length;
                    var turn = (math.floor(elapsedTime / GlobalVariables.trafficLightTimeSwitch) % nOfTrafficLightsInCross);
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
                }
                else
                {
                    LogError(trafficLightCross.Index + " has no child component");
                    //entityManager.SetName(trafficLightCross, "ECCOMISONOIOQUELLOCHECERCAVI" + trafficLightCross.Index);
                }
            }).Run();
    }
}
