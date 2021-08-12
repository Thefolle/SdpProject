using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Physics;
using Domain;

public class InitializerSystem : SystemBase
{

    private EntityManager entityManager;

    /// <summary>
    /// Don't use OnCreate because at that time gameobjects are not converted to entities yet;
    /// if this approach doesn't work anymore, use a MonoBehaviour and inject prefabs there;
    /// </summary>
    protected override void OnStartRunning()
    {
        base.OnStartRunning();

        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        // the initial entities comprise the prefabs, along with other junk
        NativeArray<Entity> initialEntities = entityManager.GetAllEntities();

        int carPrefabIndex = 0, lanePrefabIndex = 0, footpathPrefabIndex = 0, semaphorePrefabIndex = 0, crossingPrefabIndex = 0;
        for (int i = 0; i < initialEntities.Length; i++)
        {
            if (entityManager.GetName(initialEntities[i]).Equals("Car"))
            {
                carPrefabIndex = i;
            }
            else if (entityManager.GetName(initialEntities[i]).Equals("Lane"))
            {
                lanePrefabIndex = i;
            }
            else if (entityManager.GetName(initialEntities[i]).Equals("Footpath"))
            {
                footpathPrefabIndex = i;
            }
            else if (entityManager.GetName(initialEntities[i]).Equals("Semaphore"))
            {
                semaphorePrefabIndex = i;
            }
            else if (entityManager.GetName(initialEntities[i]).Equals("Crossing"))
            {
                crossingPrefabIndex = i;
            }
        }

        Entity carPrefab = initialEntities[carPrefabIndex];
        entityManager.SetEnabled(carPrefab, false);
        Entity lanePrefab = initialEntities[lanePrefabIndex];
        entityManager.SetEnabled(lanePrefab, false);
        Entity footpathPrefab = initialEntities[footpathPrefabIndex];
        entityManager.SetEnabled(footpathPrefab, false);
        Entity semaphorePrefab = initialEntities[semaphorePrefabIndex];
        entityManager.SetEnabled(semaphorePrefab, false);
        Entity crossingPrefab = initialEntities[crossingPrefabIndex];
        entityManager.SetEnabled(crossingPrefab, false);

        // load the city as a json string
        string cityString = ((UnityEngine.TextAsset) UnityEngine.Resources.Load("City", typeof(UnityEngine.TextAsset))).text;
        //UnityEngine.Debug.Log(cityString);

        // deserialize the string to City
        City city = City.FromJson(cityString);
        UnityEngine.Debug.Log(city.Streets.Count);
        UnityEngine.Debug.Log(city.Vehicles.Cars.Amount);

        // generate the city
        //CloneCar(carPrefab);
        //CloneLane(lanePrefab, 5);
        float3 initialRenderPoint = new float3(0, 0, 0);
        // choose a street as initial render prefab by convention
        CreateStreet(lanePrefab, city.Streets[0]);

        
        
    }

    private void CloneLane(Entity lanePrefab, float length)
    {
        Entity currentLane = entityManager.Instantiate(lanePrefab);
        entityManager.AddComponentData(currentLane, new LaneComponentData());
        entityManager.AddComponentData(currentLane, new NonUniformScale { Value = new float3(length * 100, 100, 10) });
        entityManager.SetEnabled(currentLane, true);
    }

    private void CreateStreet(Entity lanePrefab, Street street)
    {
        if (street.IsOneWay)
        {
            
        }
    }

    private void CloneCar(Entity carPrefab)
    {
        Entity currentCar = entityManager.Instantiate(carPrefab);
        entityManager.AddComponentData(currentCar, new CarComponentData());
        entityManager.SetEnabled(currentCar, true);
    }

    protected override void OnUpdate()
    {
        // MUST remain empty
    }

}
