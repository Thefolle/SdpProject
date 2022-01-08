using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using static UnityEngine.Debug;

public class TrafficLightSystem : SystemBase
{
    double simStart = 0;
    protected override void OnUpdate()
    {
        double elapsedTime = Time.ElapsedTime;
        if (elapsedTime < 0.5 || World.GetExistingSystem<StreetSplinePlacerSystem>().Enabled || World.GetExistingSystem<GraphGeneratorSystem>().Enabled) {
            simStart = elapsedTime;    
            return; 
        }
        
        float trafficLightTimeSwitch = 10f;

        var getChildComponentDataFromEntity = GetBufferFromEntity<Child>();

        /* Ottimizzazione: chiamo la funzione solo quando serve aggiornare il "turn", quindi attorno ad un secondo 
         * prima fino ad un secondo dopo lo switch (2 secondi di delta per sicurezza)
         */
        elapsedTime -= simStart;
        if (((elapsedTime / trafficLightTimeSwitch) % 1) < 0.1 || ((elapsedTime / trafficLightTimeSwitch) % 1) > 0.9)
            Entities.ForEach((ref TrafficLightCrossComponentData TrafficLightCrossCompData, in Entity trafficLightCross) =>
            {
                if (getChildComponentDataFromEntity.HasComponent(trafficLightCross))
                {
                    var nOfTrafficLightsInCross = getChildComponentDataFromEntity[trafficLightCross].Length;
                    var turn = (math.floor(elapsedTime / trafficLightTimeSwitch) % nOfTrafficLightsInCross);
                    TrafficLightCrossCompData.greenTurn = turn;
                }
                else
                {
                    LogErrorFormat("{0} has no child component", trafficLightCross.Index);
                }
            }).Run();
    }
}