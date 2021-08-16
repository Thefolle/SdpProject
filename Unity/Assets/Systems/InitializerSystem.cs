using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Physics;
using Domain;
using System;

public class InitializerSystem : SystemBase
{

    private EntityManager entityManager;
    private int[] arrayStreets;
    private int[] arrayIntersections;

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
        string cityString = ((UnityEngine.TextAsset)UnityEngine.Resources.Load("City", typeof(UnityEngine.TextAsset))).text;
        //UnityEngine.Debug.Log(cityString);

        // deserialize the string to City
        City city = City.FromJson(cityString);
        UnityEngine.Debug.Log(city.Streets.Count);
        UnityEngine.Debug.Log(city.Vehicles.Cars.Amount);

        // generate the city
        //CloneCar(carPrefab);
        //CloneLane(lanePrefab, 5);

        arrayStreets = new int[city.Streets.Count];
        for (int i = 0; i < city.Streets.Count; i++)
            arrayStreets[i] = 0;

        arrayIntersections = new int[city.Intersections.Count];
        for (int i = 0; i < city.Intersections.Count; i++)
            arrayIntersections[i] = 0;

        float3 initialRenderPoint = new float3(0, 0, 0);
        // choose a street as initial render prefab by convention

        for (int i = 0; i < city.Streets.Count; i++)
        {
            if (i == 0)
                if (arrayStreets[i] == 0)
                {
                    CreateStreet(lanePrefab, footpathPrefab, city.Streets[i], i);
                    if (city.Streets[i].StartingIntersectionId != -1)
                    {
                        if (arrayIntersections[(int)city.Streets[i].StartingIntersectionId] == 0)
                        {
                            CreateIntersection(crossingPrefab, city.Streets[i], (int)city.Streets[i].StartingIntersectionId);
                        }
                    }
                    if (city.Streets[i].EndingIntersectionId != -1)
                    {
                        if (arrayIntersections[(int)city.Streets[i].EndingIntersectionId] == 0)
                        {

                        }
                    }
                }
            //CreateStreet(lanePrefab, footpathPrefab, city.Streets[i]);
        }

        for (int i = 0; i < city.Vehicles.Cars.Amount; i++)
        {
            CloneCar(carPrefab, (float)city.Vehicles.Cars.Length);
        }

        UnityEngine.Debug.Log("arrayStreets: " + String.Join("",
             new List<int>(arrayStreets)
             .ConvertAll(i => i.ToString()).ToArray()));
        UnityEngine.Debug.Log("arrayIntersections: " + String.Join("",
             new List<int>(arrayIntersections)
             .ConvertAll(i => i.ToString()).ToArray()));
    }

    private void CreateIntersection(Entity crossingPrefab, Street street, int IntersectionId)
    {
        // Baso la grandezza dell'incrocio, sul numero di lanes
        // Ricorda che la lunghezza della strada � street.lenght * 100
        Entity currentIntersection = entityManager.Instantiate(crossingPrefab);
        entityManager.AddComponentData(currentIntersection, new IntersectionComponentData());
        entityManager.AddComponentData(currentIntersection, new NonUniformScale { Value = new float3((float)street.SemiCarriageways[0].LanesAmount * 1, 0.1f, (float)street.SemiCarriageways[0].LanesAmount * 1) });
        // La distanza dell'incrocio � data dalla lunghezza della strada moltiplicata per (1 + 0.2 * numero di lanes)
        if (street.StartingIntersectionId == IntersectionId)
            entityManager.AddComponentData(currentIntersection, new Translation { Value = new float3((float)street.Length * (1f + 0.2f * (float)street.SemiCarriageways[0].LanesAmount), 0, (float)street.SemiCarriageways[0].LanesAmount - 1f) }); // Perch� 1.6? (float)street.SemiCarriageways[0].LanesAmount
        if (street.EndingIntersectionId == IntersectionId)
            entityManager.AddComponentData(currentIntersection, new Translation { Value = new float3(-(float)street.Length * 1.6f, 0, 0) });
        entityManager.SetEnabled(currentIntersection, true);
    }

    private void CloneLane(Entity lanePrefab, float length, int pieceNumber)
    {
        Entity currentLane = entityManager.Instantiate(lanePrefab);
        entityManager.AddComponentData(currentLane, new LaneComponentData());
        entityManager.AddComponentData(currentLane, new NonUniformScale { Value = new float3(length * 1, 0.1f, 1) });

        // get prefab collider
        var getComponentDataFromEntity = GetComponentDataFromEntity<PhysicsCollider>();
        var initialPhysicsCollider = getComponentDataFromEntity[currentLane];
        quaternion orientation;
        float3 center;
        float3 size;
        float bevelRadius;
        unsafe
        {
            var collider = (BoxCollider*) initialPhysicsCollider.ColliderPtr;
            orientation = collider->Orientation;
            center = collider->Center;
            size = collider->Size;
            bevelRadius = collider->BevelRadius;
        }

        // modify collider
        size.x *= length;

        // set collider
        var physicsCollider = new PhysicsCollider
        {
            // Use a BoxCollider since its size can be modified at runtime, unlike the generic MeshCollider
            Value = BoxCollider.Create(
                new BoxGeometry
                {
                    Center = center,
                    BevelRadius = bevelRadius,
                    Size = size,
                    Orientation = orientation
                })
        };
        entityManager.SetComponentData(currentLane, physicsCollider);

        entityManager.AddComponentData(currentLane, new Translation { Value = new float3(0, 0, 2 * pieceNumber) });
        entityManager.SetEnabled(currentLane, true);
    }

    private void CloneFootpath(Entity Footpath, float length, int pieceNumber)
    {
        Entity currentFootpath = entityManager.Instantiate(Footpath);
        entityManager.AddComponentData(currentFootpath, new FootpathComponentData());
        entityManager.AddComponentData(currentFootpath, new NonUniformScale { Value = new float3(length * 1, 0.2f, 1) });
        if (pieceNumber == 0) entityManager.AddComponentData(currentFootpath, new Translation { Value = new float3(0, 0, -2) });
        else entityManager.AddComponentData(currentFootpath, new Translation { Value = new float3(0, 0, 2 * pieceNumber) });
        entityManager.SetEnabled(currentFootpath, true);
    }

    private void CreateStreet(Entity lanePrefab, Entity footpathPrefab, Street street, int streetN)
    {
        int i = 0;
        if (street.IsOneWay)
        {
            for (i = 0; i < street.SemiCarriageways[0].LanesAmount; i++)
            {
                if (i == 0) CloneFootpath(footpathPrefab, (float)street.Length, i);
                CloneLane(lanePrefab, (float)street.Length, i);
            }
            CloneFootpath(footpathPrefab, (float)street.Length, i);
        }
        arrayStreets[streetN] = 1;
    }

    private void CloneCar(Entity carPrefab, float length)
    {
        Entity currentCar = entityManager.Instantiate(carPrefab);
        entityManager.AddComponentData(currentCar, new CarComponentData());
        //entityManager.AddComponentData(currentCar, new NonUniformScale { Value = new float3(100, length * 80, 68) });
        //entityManager.SetComponentData(currentCar, new Rotation { Value = math.mul(quaternion.RotateY(math.radians(-90)), quaternion.RotateX(math.radians(-90))) });
        entityManager.SetComponentData(currentCar, new Translation { Value = new float3(0, 5, 1) });
        entityManager.SetEnabled(currentCar, true);
    }

    protected override void OnUpdate()
    {
        // MUST remain empty
    }

}
