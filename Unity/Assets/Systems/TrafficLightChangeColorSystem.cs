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
        if (elapsedTime < 0.5 || World.GetExistingSystem<StreetSplinePlacerSystem>().Enabled || World.GetExistingSystem<GraphGeneratorSystem>().Enabled)
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
                    else
                    {
                        //LogErrorFormat("{0} has no child component", trafficLight.Index);
                    }

                    // SPLINE
                    if (trafficLightComponentData.Spline1 != Entity.Null)
                    {
                        var spline1 = getSplineComponentDataFromEntity[trafficLightComponentData.Spline1];
                        entityManager.SetComponentData(trafficLightComponentData.Spline1, new SplineComponentData { id = spline1.id, isLast = spline1.isLast, Track = spline1.Track, isSpawner = spline1.isSpawner, carEntity = spline1.carEntity, lastSpawnedCar = spline1.lastSpawnedCar, isForward = spline1.isForward, isOccupied = !trafficLightComponentData.isGreen });
                    }

                    if (trafficLightComponentData.Spline2 != Entity.Null)
                    {
                        var spline2 = getSplineComponentDataFromEntity[trafficLightComponentData.Spline2];
                        entityManager.SetComponentData(trafficLightComponentData.Spline2, new SplineComponentData { id = spline2.id, isLast = spline2.isLast, Track = spline2.Track, isSpawner = spline2.isSpawner, carEntity = spline2.carEntity, lastSpawnedCar = spline2.lastSpawnedCar, isForward = spline2.isForward, isOccupied = !trafficLightComponentData.isGreen });
                    }
                }
            }).Run();

        PostUpdateCommands.Playback(EntityManager);
        PostUpdateCommands.Dispose();
    }
}