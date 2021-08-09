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

        int carPrefabIndex = 0, lanePrefabIndex = 0;
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
        }

        Entity carPrefab = initialEntities[carPrefabIndex];
        entityManager.SetEnabled(carPrefab, false);
        Entity lanePrefab = initialEntities[lanePrefabIndex];
        entityManager.SetEnabled(lanePrefab, false);

        // load the city as a json string
        string cityString = ((UnityEngine.TextAsset) UnityEngine.Resources.Load("City", typeof(UnityEngine.TextAsset))).text;
        //UnityEngine.Debug.Log(cityString);

        // deserialize the string to City
        City city = City.FromJson(cityString);
        UnityEngine.Debug.Log(city.Streets.Count);
        UnityEngine.Debug.Log(city.Vehicles.Cars.Amount);

        // generate the city
        CloneCar(carPrefab);
        CloneLane(lanePrefab, 5);
        
    }

    private void CloneLane(Entity lanePrefab, float length)
    {
        Entity currentLane = entityManager.Instantiate(lanePrefab);
        entityManager.AddComponentData(currentLane, new LaneComponentData());
        entityManager.AddComponentData(currentLane, new NonUniformScale { Value = new float3(length * 100, 100, 10) });
        entityManager.SetEnabled(currentLane, true);
    }

    private void CreateStreet(/* Street street*/)
    {

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
