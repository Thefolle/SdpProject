using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Physics;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics.Systems;
using static UnityEngine.Debug;
using Unity.Collections;

public class TrafficLightChangeColorSystem : SystemBase
{
    public UnityEngine.Material[] material;
    UnityEngine.Renderer rend;
    public EntityCommandBuffer commandBuffer;

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
        var getTrafficLightCrossComponentDataFromEntity = GetComponentDataFromEntity<TrafficLightCrossComponentData>();
        var PostUpdateCommands = new EntityCommandBuffer(Allocator.Temp);
        //var getComponentObject = entityManager.GetComponentObject<UnityEngine.Renderer>(new Entity());

        if (((elapsedTime / GlobalVariables.trafficLightTimeSwitch) % 1) < 0.1 || ((elapsedTime / GlobalVariables.trafficLightTimeSwitch) % 1) > 0.9)
            Entities.ForEach((Entity trafficLight, ref TrafficLightComponentData trafficLightComponentData) =>
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
                }
            }).Run();

        PostUpdateCommands.Playback(EntityManager);
        PostUpdateCommands.Dispose();
    }
}