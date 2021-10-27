using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Physics;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics.Systems;
using static UnityEngine.Debug;
using Unity.Collections;

public class VehicleChangeLaneSystem : SystemBase
{
    //private static Entity currentStreet = new Entity();
    private static bool newStreet = false; // dummy variable, if true change the lane

    protected override void OnUpdate()
    {
        float deltaTime = Time.DeltaTime;
        double elapsedTime = Time.ElapsedTime;
        //if (elapsedTime < 2) return;

        PhysicsWorld physicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>().PhysicsWorld;
        EntityManager entityManager = World.EntityManager;
        var getTrackComponentDataFromEntity = GetComponentDataFromEntity<TrackComponentData>();
        var getCarComponentDataFromEntity = GetComponentDataFromEntity<CarComponentData>();
        var getParentComponentDataFromEntity = GetComponentDataFromEntity<Parent>();
        var getChildComponentDataFromEntity = GetBufferFromEntity<Child>();
        var getLaneComponentDataFromEntity = GetComponentDataFromEntity<LaneComponentData>();
        //var currentStreetHere = currentStreet;


        Entities.ForEach((Entity carEntity, LocalToWorld localToWorld, ref PhysicsVelocity physicsVelocity, ref CarComponentData carComponentData1, in CarComponentData carComponentData) =>
        {
            var raycastInputLeft = new RaycastInput
            {
                Start = localToWorld.Position,
                End = localToWorld.Position + 20 * -localToWorld.Right,
                Filter = CollisionFilter.Default
            };
            var leftHits = new NativeList<RaycastHit>(20, Allocator.TempJob);

            /* Assume that there exists only one admissible hit in the world with the given id*/
            if (physicsWorld.CastRay(raycastInputLeft, ref leftHits) && leftHits.Length > 1)
            {
                foreach (var it in leftHits)
                {
                    if (getTrackComponentDataFromEntity.HasComponent(it.Entity))
                    {
                        var trackComponentData = getTrackComponentDataFromEntity[it.Entity];
                        if (it.Entity.Index == carComponentData.TrackId)
                        {
                            var parentEntity = getParentComponentDataFromEntity[it.Entity];
                            var laneName = entityManager.GetName(parentEntity.Value).ToString();
                            if (laneName.Contains("Lane"))
                            {
                                var laneDirection = laneName.Substring(0, laneName.IndexOf('-'));
                                var laneNumberInDirection = laneName.Substring(laneName.LastIndexOf('-') + 1);
                                var adjacentLaneNumbersInSameDirection = new int[2]; // vector[0] on the left, vector[1] on the right
                                var adjacentTrackIdInSameDirection = new int[2] { -1, -1 };

                                /*LogError("before change track: ");
                                foreach (var aaa in adjacentTrackIdInSameDirection)
                                    LogError(aaa);*/

                                //LogError("laneName: " + laneName + ", laneDirection: " + laneDirection + ", laneNumberInDirection: " + laneNumberInDirection);

                                if (newStreet && elapsedTime > 10) // Qui ci metteremo il trigger che decide di far cambiare corsia, per ora uso lui
                                {
                                    //changeLane(it.Entity, HasComponent<Parent>(parentEntity.Value), entityManager, getParentComponentDataFromEntity, carEntity, carComponentData1);
                                    //LogError("trigger");
                                    if (HasComponent<Parent>(parentEntity.Value))
                                    {
                                        var currentStreet = getParentComponentDataFromEntity[parentEntity.Value].Value;
                                        //LogError(getChildComponentDataFromEntity[currentStreet].ToString());
                                        /*foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(obj))
                                        {
                                            string name = descriptor.Name;
                                            object value = descriptor.GetValue(obj);
                                            Console.WriteLine("{0}={1}", name, value);
                                        }*/
                                        foreach (var lane in getChildComponentDataFromEntity[currentStreet])
                                        {
                                            var thisLaneName = entityManager.GetName(lane.Value);
                                            var thisLaneDirection = thisLaneName.Substring(0, laneName.IndexOf('-'));
                                            var thisLaneNumberInDirection = thisLaneName.Substring(laneName.LastIndexOf('-') + 1);
                                            if (thisLaneDirection.CompareTo(laneDirection) == 0 && thisLaneNumberInDirection != laneNumberInDirection) // Same direction, different lane
                                            {
                                                if (System.Int32.Parse(thisLaneNumberInDirection) - System.Int32.Parse(laneNumberInDirection) == -1) // Other lane on the left
                                                {
                                                    adjacentLaneNumbersInSameDirection[0] = System.Int32.Parse(thisLaneNumberInDirection);
                                                    var track = getChildComponentDataFromEntity[lane.Value];
                                                    if (getTrackComponentDataFromEntity.HasComponent(track[0].Value))
                                                    {
                                                        var thisTrackComponentData = getTrackComponentDataFromEntity[track[0].Value];
                                                        //adjacentTrackIdInSameDirection[0] = thisTrackComponentData.id;
                                                        adjacentTrackIdInSameDirection[0] = track[0].Value.Index;
                                                    }
                                                }
                                                else if (System.Int32.Parse(thisLaneNumberInDirection) - System.Int32.Parse(laneNumberInDirection) == 1) // Other lane on the right
                                                {
                                                    adjacentLaneNumbersInSameDirection[1] = System.Int32.Parse(thisLaneNumberInDirection);
                                                    var track = getChildComponentDataFromEntity[lane.Value];
                                                    if (getTrackComponentDataFromEntity.HasComponent(track[0].Value))
                                                    {
                                                        var thisTrackComponentData = getTrackComponentDataFromEntity[track[0].Value];
                                                        //adjacentTrackIdInSameDirection[1] = thisTrackComponentData.id;
                                                        adjacentTrackIdInSameDirection[1] = track[0].Value.Index;
                                                    }
                                                }
                                            }
                                            //LogError(entityManager.GetName(lane.Value));
                                        }
                                    }

                                    if (adjacentTrackIdInSameDirection[0] != -1) // Before check left
                                        carComponentData1.TrackId = adjacentTrackIdInSameDirection[0];
                                    else if (adjacentTrackIdInSameDirection[1] != -1) // If left not available, check right
                                        carComponentData1.TrackId = adjacentTrackIdInSameDirection[1];

                                    /*LogError("before change track: ");
                                    foreach (var aaa in adjacentTrackIdInSameDirection)
                                        LogError(aaa);
                                    LogError(carComponentData1.TrackId);*/
                                    newStreet = false;
                                }

                                /*
                                if (HasComponent<Parent>(parentEntity.Value) && entityManager.GetName(currentStreetHere).CompareTo(entityManager.GetName(getParentComponentDataFromEntity[parentEntity.Value].Value)) != 0)
                                {
                                    LogError("newStreet: " + entityManager.GetName(getParentComponentDataFromEntity[parentEntity.Value].Value) + " oldStreet: " + entityManager.GetName(currentStreetHere));
                                    currentStreetHere = getParentComponentDataFromEntity[parentEntity.Value].Value;
                                    currentStreet = currentStreetHere;
                                }
                                else
                                {
                                    LogError("oldStreet");
                                }*/
                            }
                            break;
                        }
                    }
                }
            }
            leftHits.Dispose();
        }).Run();
    }
}
