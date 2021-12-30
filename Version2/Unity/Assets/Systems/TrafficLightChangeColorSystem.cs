using Unity.Entities;
using Unity.Transforms;
using static UnityEngine.Debug;
using Unity.Collections;

public class TrafficLightChangeColorSystem : SystemBase
{
    public UnityEngine.Material[] material;
    UnityEngine.Renderer rend;
    public EntityCommandBuffer commandBuffer;

    protected override void OnUpdate()
    {
        double elapsedTime = Time.ElapsedTime;
        if (elapsedTime < 0.5) return;

        float trafficLightTimeSwitch = 10f;

        EntityManager entityManager = World.EntityManager;
        var getParentComponentDataFromEntity = GetComponentDataFromEntity<Parent>();
        var getChildComponentDataFromEntity = GetBufferFromEntity<Child>();
        var getTrafficLightCrossComponentDataFromEntity = GetComponentDataFromEntity<TrafficLightCrossComponentData>();
        var getSplineComponentDataFromEntity = GetComponentDataFromEntity<SplineComponentData>();
        var PostUpdateCommands = new EntityCommandBuffer(Allocator.Temp);

        if (((elapsedTime / trafficLightTimeSwitch) % 1) < 0.1 || ((elapsedTime / trafficLightTimeSwitch) % 1) > 0.9)
            Entities.ForEach((ref TrafficLightComponentData trafficLightComponentData, in Entity trafficLight) =>
            {
                //rend = entityManager.GetComponentObject<UnityEngine.MeshRenderer>(trafficLight);
                //rend.enabled = true;

                var trafficLightNumber = entityManager.GetName(trafficLight).Substring(entityManager.GetName(trafficLight).LastIndexOf('-') + 1);
                var trafficLightCross = getParentComponentDataFromEntity[trafficLight];
                if (getTrafficLightCrossComponentDataFromEntity.HasComponent(trafficLightCross.Value))
                {
                    var trafficLightCrossComponentData = getTrafficLightCrossComponentDataFromEntity[trafficLightCross.Value];
                    if (getChildComponentDataFromEntity.HasComponent(trafficLight))
                    {
                        foreach (var trafficLightPanel in getChildComponentDataFromEntity[trafficLight])
                        {
                            var thisTrafficLightPanelName = entityManager.GetName(trafficLightPanel.Value);
                            if (trafficLightNumber == trafficLightCrossComponentData.greenTurn.ToString())
                            {
                                // GREEN color
                                trafficLightComponentData.isGreen = true;
                                //rend.sharedMaterial.color = UnityEngine.Color.green;
                                if (thisTrafficLightPanelName.Contains("Green"))
                                {
                                    //entityManager.RemoveComponent(trafficLightPanel.Value, typeof(Disabled));
                                    PostUpdateCommands.RemoveComponent<Disabled>(trafficLightPanel.Value);
                                }
                                else if (thisTrafficLightPanelName.Contains("Red"))
                                {
                                    //entityManager.AddComponent(trafficLightPanel.Value, typeof(Disabled));
                                    PostUpdateCommands.AddComponent<Disabled>(trafficLightPanel.Value);
                                }

                            }
                            else
                            {
                                // RED color
                                trafficLightComponentData.isGreen = false;
                                //rend.sharedMaterial.color = UnityEngine.Color.red;

                                if (thisTrafficLightPanelName.Contains("Green"))
                                {
                                    //entityManager.AddComponent(trafficLightPanel.Value, typeof(Disabled));
                                    PostUpdateCommands.AddComponent<Disabled>(trafficLightPanel.Value);
                                }
                                else if (thisTrafficLightPanelName.Contains("Red"))
                                {
                                    //entityManager.RemoveComponent(trafficLightPanel.Value, typeof(Disabled));
                                    PostUpdateCommands.RemoveComponent<Disabled>(trafficLightPanel.Value);
                                }
                            }
                        }
                    }
                    else
                    {
                        LogError(trafficLight.Index + " has no child component");
                        //entityManager.SetName(trafficLight, "ECCOMISONOIOQUELLOCHECERCAVI" + trafficLight.Index);
                    }

                    // SPLINE
                    if (trafficLightComponentData.Spline1 != Entity.Null)
                    {
                        var spline1 = getSplineComponentDataFromEntity[trafficLightComponentData.Spline1];
                        entityManager.SetComponentData(trafficLightComponentData.Spline1, new SplineComponentData { id = spline1.id, isLast = spline1.isLast, Track = spline1.Track, isSpawner = spline1.isSpawner, carEntity = spline1.carEntity, lastSpawnedCar = spline1.lastSpawnedCar, lastTimeSpawned = spline1.lastTimeSpawned, isForward = spline1.isForward, isOccupied = !trafficLightComponentData.isGreen });
                    }

                    if (trafficLightComponentData.Spline2 != Entity.Null)
                    {
                        var spline2 = getSplineComponentDataFromEntity[trafficLightComponentData.Spline2];
                        entityManager.SetComponentData(trafficLightComponentData.Spline2, new SplineComponentData { id = spline2.id, isLast = spline2.isLast, Track = spline2.Track, isSpawner = spline2.isSpawner, carEntity = spline2.carEntity, lastSpawnedCar = spline2.lastSpawnedCar, lastTimeSpawned = spline2.lastTimeSpawned, isForward = spline2.isForward, isOccupied = !trafficLightComponentData.isGreen });
                    }
                    if (trafficLightComponentData.Spline3 != Entity.Null)
                    {
                        var spline3 = getSplineComponentDataFromEntity[trafficLightComponentData.Spline3];
                        entityManager.SetComponentData(trafficLightComponentData.Spline3, new SplineComponentData { id = spline3.id, isLast = spline3.isLast, Track = spline3.Track, isSpawner = spline3.isSpawner, carEntity = spline3.carEntity, lastSpawnedCar = spline3.lastSpawnedCar, lastTimeSpawned = spline3.lastTimeSpawned, isForward = spline3.isForward, isOccupied = !trafficLightComponentData.isGreen });
                    }
                    if (trafficLightComponentData.Spline4 != Entity.Null)
                    {
                        var spline4 = getSplineComponentDataFromEntity[trafficLightComponentData.Spline4];
                        entityManager.SetComponentData(trafficLightComponentData.Spline4, new SplineComponentData { id = spline4.id, isLast = spline4.isLast, Track = spline4.Track, isSpawner = spline4.isSpawner, carEntity = spline4.carEntity, lastSpawnedCar = spline4.lastSpawnedCar, lastTimeSpawned = spline4.lastTimeSpawned, isForward = spline4.isForward, isOccupied = !trafficLightComponentData.isGreen });
                    }
                    if (trafficLightComponentData.Spline5 != Entity.Null)
                    {
                        var spline5 = getSplineComponentDataFromEntity[trafficLightComponentData.Spline5];
                        entityManager.SetComponentData(trafficLightComponentData.Spline5, new SplineComponentData { id = spline5.id, isLast = spline5.isLast, Track = spline5.Track, isSpawner = spline5.isSpawner, carEntity = spline5.carEntity, lastSpawnedCar = spline5.lastSpawnedCar, lastTimeSpawned = spline5.lastTimeSpawned, isForward = spline5.isForward, isOccupied = !trafficLightComponentData.isGreen });
                    }
                    if (trafficLightComponentData.Spline6 != Entity.Null)
                    {
                        var spline6 = getSplineComponentDataFromEntity[trafficLightComponentData.Spline6];
                        entityManager.SetComponentData(trafficLightComponentData.Spline6, new SplineComponentData { id = spline6.id, isLast = spline6.isLast, Track = spline6.Track, isSpawner = spline6.isSpawner, carEntity = spline6.carEntity, lastSpawnedCar = spline6.lastSpawnedCar, lastTimeSpawned = spline6.lastTimeSpawned, isForward = spline6.isForward, isOccupied = !trafficLightComponentData.isGreen });
                    }
                    if (trafficLightComponentData.Spline7 != Entity.Null)
                    {
                        var spline7 = getSplineComponentDataFromEntity[trafficLightComponentData.Spline7];
                        entityManager.SetComponentData(trafficLightComponentData.Spline7, new SplineComponentData { id = spline7.id, isLast = spline7.isLast, Track = spline7.Track, isSpawner = spline7.isSpawner, carEntity = spline7.carEntity, lastSpawnedCar = spline7.lastSpawnedCar, lastTimeSpawned = spline7.lastTimeSpawned, isForward = spline7.isForward, isOccupied = !trafficLightComponentData.isGreen });

                    }
                    if (trafficLightComponentData.Spline8 != Entity.Null)
                    {
                        var spline8 = getSplineComponentDataFromEntity[trafficLightComponentData.Spline8];
                        entityManager.SetComponentData(trafficLightComponentData.Spline8, new SplineComponentData { id = spline8.id, isLast = spline8.isLast, Track = spline8.Track, isSpawner = spline8.isSpawner, carEntity = spline8.carEntity, lastSpawnedCar = spline8.lastSpawnedCar, lastTimeSpawned = spline8.lastTimeSpawned, isForward = spline8.isForward, isOccupied = !trafficLightComponentData.isGreen });
                    }
                }
            }).Run();

        PostUpdateCommands.Playback(EntityManager);
        PostUpdateCommands.Dispose();
    }
}