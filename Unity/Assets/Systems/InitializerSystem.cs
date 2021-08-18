using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Physics;
using Domain;
using System;

/// <summary>
/// Implementation details:
/// 
/// <list type="bullet">
/// <item>Instantiating entities: when Unity converts a gameobject with multiple materials to an Entity,
/// it adds a child for each material attached to the gameobject; however, the ECS system doesn't fully support entity hierarchy yet.
/// As a solution, we implemented the conversion of hierarchies manually.
/// In details, the parent can be distinguished from the other entities due to the presence of the PhysicsCollider; moreover,
/// the programmer has to add a buffer of Child to the parent entity and set the child's Parent to the actual one.
/// </item>
/// </list>
/// </summary>

public class InitializerSystem : SystemBase
{

    private EntityManager entityManager;
    private int[] arrayStreets;
    private int[] arrayIntersections;
    private City city;

    // The algorithm that arranges the components in the GUI uses this point to position them 
    private float3 pivot;
    private float3 direction;

    private Dictionary<string, Entity> prefabs;

    private float laneWidth = (float) 2; // La conversione (float) è necessaria
    private float footPathWidth = (float) 1;
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
        var prefabNames = new List<string>() { "Car", "Lane", "Footpath", "Semaphore", "Crossing"};

        prefabs = new Dictionary<string, Entity>();
        for (int i = 0; i < initialEntities.Length; i++)
        {
            if (prefabNames.Contains(entityManager.GetName(initialEntities[i])))
            {
                entityManager.SetEnabled(initialEntities[i], false);
                prefabs.Add(entityManager.GetName(initialEntities[i]) + initialEntities[i].Index, initialEntities[i]);
            }
        }


        // load the city as a json string
        string cityString = ((UnityEngine.TextAsset)UnityEngine.Resources.Load("City", typeof(UnityEngine.TextAsset))).text;
        // deserialize the string to City
        city = City.FromJson(cityString);


        // generate the city

        arrayStreets = new int[city.Streets.Count];
        for (int i = 0; i < city.Streets.Count; i++)
            arrayStreets[i] = 0;

        arrayIntersections = new int[city.Intersections.Count];
        for (int i = 0; i < city.Intersections.Count; i++)
            arrayIntersections[i] = 0;

        pivot = new float3(0, 0, 0);
        direction = new float3(0, 0, 1);
        // choose a street as initial render prefab by convention

        CreateStreet(city.Streets[0], 0);

        for (int i = 0; i < city.Vehicles.Cars.Amount; i++)
        {
            CloneCar((float)city.Vehicles.Cars.Length);
        }

        UnityEngine.Debug.Log("arrayStreets: " + String.Join("",
             new List<int>(arrayStreets)
             .ConvertAll(i => i.ToString()).ToArray()));
        UnityEngine.Debug.Log("arrayIntersections: " + String.Join("",
             new List<int>(arrayIntersections)
             .ConvertAll(i => i.ToString()).ToArray()));
    }

    private void CreateIntersection(Street street, int IntersectionId)
    {
        // Baso la grandezza dell'incrocio, sul numero di lanes
        // Ricorda che la lunghezza della strada � street.lenght * 100
        Entity currentIntersection = Entity.Null;
        foreach (var entry in prefabs)
        {
            if (entry.Key.StartsWith("Crossing"))
            {
                currentIntersection = entityManager.Instantiate(entry.Value);
            }
        }
       
        entityManager.AddComponentData(currentIntersection, new IntersectionComponentData());
        //entityManager.AddComponentData(currentIntersection, new NonUniformScale { Value = new float3((float)street.SemiCarriageways[0].LanesAmount * 1, 0.1f, (float)street.SemiCarriageways[0].LanesAmount * 1) });
        ///entityManager.AddComponentData(currentIntersection, new NonUniformScale { Value = new float3((float) street.SemiCarriageways[0].LanesAmount * 1 / 2, 0.1f, (float)street.SemiCarriageways[0].LanesAmount * 1 / 2) });
        entityManager.AddComponentData(currentIntersection, new NonUniformScale { Value = new float3((float)street.SemiCarriageways[0].LanesAmount * laneWidth, 0.1f, (float)street.SemiCarriageways[0].LanesAmount * laneWidth) });
        // La distanza dell'incrocio � data dalla lunghezza della strada moltiplicata per (1 + 0.2 * numero di lanes)
        entityManager.AddComponentData(currentIntersection, new Translation { Value = new float3(pivot.x, pivot.y, pivot.z) });
        
        entityManager.SetEnabled(currentIntersection, true);
        arrayIntersections[IntersectionId] = 1;
        /*for(int i = 0; i<city.Intersections[IntersectionId].streets.Count; i++)
        {

        }*/
    }

    private void CloneLane(float length, int pieceNumber)
    {
        // clone hierarchy
        List<Entity> prefabHierarchy = new List<Entity>(3);
        foreach (var entry in prefabs)
        {
            if (entry.Key.StartsWith("Lane"))
            {
                prefabHierarchy.Add(entry.Value);
            }
        }

        Entity parent = prefabHierarchy.Find(entity =>
        {
            return entityManager.HasComponent(entity, typeof(PhysicsCollider));
        });

        prefabHierarchy.RemoveAll(entity =>
        {
            return entity.Index == parent.Index;
        });

        Entity currentLane = entityManager.Instantiate(parent);
        var entityCommandBuffer = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>().CreateCommandBuffer();
        var children = entityCommandBuffer.AddBuffer<Child>(currentLane);
        var instantiatedPrefabHierarchy = prefabHierarchy.ConvertAll(source => { return entityManager.Instantiate(source); });
        foreach (var child in instantiatedPrefabHierarchy)
        {
            entityManager.SetComponentData(child, new Parent { Value = currentLane });
            children.Add(new Child { Value = child });
        }
        // end of clone hierarchy

        entityManager.AddComponentData(currentLane, new LaneComponentData());
        ///entityManager.AddComponentData(currentLane, new NonUniformScale { Value = new float3(length * 1, 0.1f, (float) 1 / 2) });
        entityManager.AddComponentData(currentLane, new NonUniformScale { Value = new float3(length * 1, 0.1f, laneWidth) });
        if (direction.x == 1 && direction.z == 0)
        {

        }
        else
        {
            entityManager.AddComponentData(currentLane, new Rotation { Value = quaternion.RotateY(math.radians(90)) });
        }
            

        // Unity doesn't support the direct scaling of colliders, so create it from scratch
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

        entityManager.AddComponentData(currentLane, new Translation { Value = new float3(pivot.x, pivot.y, pivot.z) });
        instantiatedPrefabHierarchy.ForEach(child => entityManager.SetEnabled(child, true));
        entityManager.SetEnabled(currentLane, true);
    }

    private void CloneFootpath(float length, int pieceNumber)
    {
        Entity currentFootpath = Entity.Null;
        foreach (var entry in prefabs)
        {
            if (entry.Key.StartsWith("Footpath"))
            {
                currentFootpath = entityManager.Instantiate(entry.Value);
            }
        }

        entityManager.AddComponentData(currentFootpath, new FootpathComponentData());
        ///entityManager.AddComponentData(currentFootpath, new NonUniformScale { Value = new float3(length * 1, 0.2f, (float) 1 / 2) });
        if(direction.x == 1 && direction.z == 0)
            entityManager.AddComponentData(currentFootpath, new NonUniformScale { Value = new float3(length * 1, 0.2f, footPathWidth) });
        else
            entityManager.AddComponentData(currentFootpath, new NonUniformScale { Value = new float3(footPathWidth, 0.2f, length * 1) });
        //if (pieceNumber == 0) entityManager.AddComponentData(currentFootpath, new Translation { Value = new float3(0, 0, -2) });
        entityManager.SetComponentData(currentFootpath, new Translation { Value = new float3(pivot.x, pivot.y, pivot.z) });
        entityManager.SetEnabled(currentFootpath, true);
    }

    private void CreateStreet(Street street, int streetN)
    {
        var pivotCopy = pivot;
        int i = 0;
        if (street.IsOneWay)
        {
            ///pivot += new float3(0, 0, ((float) street.SemiCarriageways[0].LanesAmount) / 2 * 1 + (float) 1 / 2);
            pivot = ourPivot("+", new float3(0, 0, ((float)street.SemiCarriageways[0].LanesAmount) * laneWidth * 1 + footPathWidth));
            ////pivot += new float3(0, 0, ((float)street.SemiCarriageways[0].LanesAmount) * laneWidth * 1 + footPathWidth);
            CloneFootpath((float)street.Length, 0);
            ///pivot -= new float3(0, 0, (float) 1 / 2 + (float) 1 / 2 );
            ////pivot -= new float3(0, 0, laneWidth + footPathWidth);
            pivot = ourPivot("-", new float3(0, 0, laneWidth + footPathWidth));
            for (i = 0; i < street.SemiCarriageways[0].LanesAmount; i++)
            {
                CloneLane((float)street.Length, i);
                ///pivot -= new float3(0, 0, 1);
                ////pivot -= new float3(0, 0, laneWidth * 2);
                pivot = ourPivot("-", new float3(0, 0, laneWidth * 2));
            }
            ///pivot += new float3(0, 0, (float) 1 / 2);
            ////pivot += new float3(0, 0, laneWidth);
            pivot = ourPivot("+", new float3(0, 0, laneWidth));
            ///pivot -= new float3(0, 0, (float) 1 / 2);
            ////pivot -= new float3(0, 0, footPathWidth);
            pivot = ourPivot("-", new float3(0, 0, footPathWidth));
            CloneFootpath((float)street.Length, i);
            // riporto il pivot.z al centro, letteralmente sommo cosa ho sottratto e sottraggo cosa ho sommato prima
            ///pivot += new float3(0, 0, -(((float)street.SemiCarriageways[0].LanesAmount) / 2 * 1 + (float)1 / 2) + 1 + (float) street.SemiCarriageways[0].LanesAmount * 1);
            ////pivot += new float3(0, 0, -(((float)street.SemiCarriageways[0].LanesAmount) * laneWidth * 1 + footPathWidth) + laneWidth + footPathWidth + (float)street.SemiCarriageways[0].LanesAmount * laneWidth * 2 - laneWidth + footPathWidth);
            pivot = ourPivot("+", new float3(0, 0, -(((float)street.SemiCarriageways[0].LanesAmount) * laneWidth * 1 + footPathWidth) + laneWidth + footPathWidth + (float)street.SemiCarriageways[0].LanesAmount * laneWidth * 2 - laneWidth + footPathWidth));
            // recur on the two intersections
            arrayStreets[streetN] = 1;
            if (street.StartingIntersectionId != null) // Starting, viene prima, quindi prima -=, dopo +=
            {
                ///pivot -= new float3((float)street.Length + (float) street.SemiCarriageways[0].LanesAmount * 1/2, 0, 0);
                ////pivot -= new float3((float)street.Length + (float) street.SemiCarriageways[0].LanesAmount * laneWidth, 0, 0);
                pivot = ourPivot("-", new float3((float)street.Length + (float)street.SemiCarriageways[0].LanesAmount * laneWidth, 0, 0));
                CreateIntersection(street, (int) street.StartingIntersectionId);
                ///pivot += new float3((float)street.Length + (float)street.SemiCarriageways[0].LanesAmount * 1 / 2, 0, 0);
                ////pivot += new float3((float)street.Length + (float)street.SemiCarriageways[0].LanesAmount * laneWidth, 0, 0);
                pivot = ourPivot("+", new float3((float)street.Length + (float)street.SemiCarriageways[0].LanesAmount * laneWidth, 0, 0));
            }
        }
        //arrayStreets[streetN] = 1;

        pivot = pivotCopy;
    }

    private float3 ourPivot(string v, float3 change)
    {
        if(direction.x == 1 && direction.z == 0)
        {
            if(v == "+")
                return new float3(pivot.x + change.x, pivot.y + change.y, pivot.z + change.z);
            return new float3(pivot.x - change.x, pivot.y - change.y, pivot.z - change.z);
        } else if (direction.x == 0 && direction.z == 1)
        {
            if(v == "+")
                return new float3(pivot.x + change.z, pivot.y + change.y, pivot.z + change.x);
            return new float3(pivot.x - change.z, pivot.y - change.y, pivot.z - change.x);
        } else
        {
            throw new NotImplementedException();
        }
    }

    private void CloneCar(float length)
    {
        Entity currentCar = Entity.Null;
        foreach (var entry in prefabs)
        {
            if (entry.Key.StartsWith("Car"))
            {
                currentCar = entityManager.Instantiate(entry.Value);
            }
        }

        entityManager.AddComponentData(currentCar, new CarComponentData { hasVehicleUpfront = false });
        //entityManager.AddComponentData(currentCar, new NonUniformScale { Value = new float3(100, length * 80, 68) });
        entityManager.SetComponentData(currentCar, new Rotation { Value = quaternion.RotateY(math.radians(90)) });
        entityManager.SetComponentData(currentCar, new Translation { Value = new float3(-2, 1, 0) });
        entityManager.SetEnabled(currentCar, true);
    }

    protected override void OnUpdate()
    {
        // MUST remain empty
    }

}
