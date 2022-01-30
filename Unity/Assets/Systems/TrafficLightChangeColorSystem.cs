using Unity.Entities;
using Unity.Transforms;
using static UnityEngine.Debug;
using Unity.Collections;

public class TrafficLightChangeColorSystem : SystemBase
{
    public UnityEngine.Material[] material;
    UnityEngine.Renderer rend;
    public EntityCommandBuffer commandBuffer;
    double simStart = 0;
    protected override void OnUpdate()
    {
        double elapsedTime = Time.ElapsedTime;
        if (elapsedTime < 0.5 || World.GetExistingSystem<StreetSplinePlacerSystem>().Enabled || World.GetExistingSystem<GraphGeneratorSystem>().Enabled || World.GetExistingSystem<SemaphoreStateAssignerSystem>().Enabled)
        {
            simStart = elapsedTime;
            return;
        }

        float trafficLightTimeSwitch = 10f;

        EntityManager entityManager = World.EntityManager;
        var getParentComponentDataFromEntity = GetComponentDataFromEntity<Parent>();
        var getChildComponentDataFromEntity = GetBufferFromEntity<Child>();
        var getTrafficLightCrossComponentDataFromEntity = GetComponentDataFromEntity<TrafficLightCrossComponentData>();
        var getSplineComponentDataFromEntity = GetComponentDataFromEntity<SplineComponentData>();
        var getTrafficLightPanelComponentData = GetComponentDataFromEntity<TrafficLightPanelComponentData>();

        var PostUpdateCommands = new EntityCommandBuffer(Allocator.Temp);
        elapsedTime -= simStart;
        if (((elapsedTime / trafficLightTimeSwitch) % 1) < 0.1 || ((elapsedTime / trafficLightTimeSwitch) % 1) > 0.9)
            Entities.ForEach((ref TrafficLightComponentData trafficLightComponentData, in Entity trafficLight) =>
            {
                var trafficLightNumber = trafficLightComponentData.RelativeId;
                var trafficLightCross = getParentComponentDataFromEntity[trafficLight];
                if (getTrafficLightCrossComponentDataFromEntity.HasComponent(trafficLightCross.Value))
                {
                    var trafficLightCrossComponentData = getTrafficLightCrossComponentDataFromEntity[trafficLightCross.Value];
                    var wasGreen = trafficLightComponentData.isGreen;
                    if (getChildComponentDataFromEntity.HasComponent(trafficLight))
                    {
                        foreach (var trafficLightPanel in getChildComponentDataFromEntity[trafficLight])
                        {
                            if (getTrafficLightPanelComponentData.HasComponent(trafficLightPanel.Value)) // exclude the support
                            {
                                var trafficLightPanelComponentData = getTrafficLightPanelComponentData[trafficLightPanel.Value];
                                if (trafficLightNumber == trafficLightCrossComponentData.greenTurn)
                                {
                                    // GREEN color
                                    trafficLightComponentData.isGreen = true;
                                    if (trafficLightPanelComponentData.Color == TrafficLightColor.Green)
                                    {
                                        PostUpdateCommands.RemoveComponent<Disabled>(trafficLightPanel.Value);
                                    }
                                    else if (trafficLightPanelComponentData.Color == TrafficLightColor.Red)
                                    {
                                        PostUpdateCommands.AddComponent<Disabled>(trafficLightPanel.Value);
                                    }

                                }
                                else
                                {
                                    // RED color
                                    trafficLightComponentData.isGreen = false;

                                    if (trafficLightPanelComponentData.Color == TrafficLightColor.Green)
                                    {
                                        PostUpdateCommands.AddComponent<Disabled>(trafficLightPanel.Value);
                                    }
                                    else if (trafficLightPanelComponentData.Color == TrafficLightColor.Red)
                                    {
                                        PostUpdateCommands.RemoveComponent<Disabled>(trafficLightPanel.Value);
                                    }
                                }
                            }
                        }
                    }

                    // SPLINE
                    
                    if (trafficLightComponentData.Spline1 != Entity.Null && trafficLightComponentData.Spline2 != Entity.Null && wasGreen != trafficLightComponentData.isGreen)
                    {

                        PostUpdateCommands.SetComponent(trafficLightComponentData.Spline1, new SemaphoreStateComponentData { IsGreen = trafficLightComponentData.isGreen });
                        PostUpdateCommands.SetComponent(trafficLightComponentData.Spline2, new SemaphoreStateComponentData { IsGreen = trafficLightComponentData.isGreen });

                    }

                }
            }).Run();

        PostUpdateCommands.Playback(EntityManager);
        PostUpdateCommands.Dispose();
    }
}