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
    private static bool newStreet = true; // dummy variable, if true change the lane
    private static float laneWidth = 2.5f;

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
        var getLocalToWorldComponentDataFromEntity = GetComponentDataFromEntity<LocalToWorld>();
        //var currentStreetHere = currentStreet;


        Entities.ForEach((Entity carEntity, LocalToWorld localToWorld, ref PhysicsVelocity physicsVelocity, ref CarComponentData carComponentData) =>
        {
            if (carComponentData.tryOvertake)
            {
                bool isCollisionFound = false;
                RaycastHit coll = default;

                var occupiedTrackIds = new ArrayList();
                var raycastInputLeft = new RaycastInput
                {
                    Start = localToWorld.Position,
                    End = localToWorld.Position + 20 * -localToWorld.Right,
                    Filter = CollisionFilter.Default
                };
                var leftHits = new NativeList<RaycastHit>(20, Allocator.TempJob);
                var raycastInputRight = new RaycastInput
                {
                    Start = localToWorld.Position,
                    End = localToWorld.Position + 20 * localToWorld.Right,
                    Filter = CollisionFilter.Default
                };
                var rightHits = new NativeList<RaycastHit>(20, Allocator.TempJob);

                var sphereHits = new NativeList<ColliderCastHit>(20, Allocator.TempJob);
                var radius = laneWidth * 3f;
                var direction = localToWorld.Forward;
                var maxDistance = 5f;

                if (carComponentData.Speed <= 20)
                {
                    direction = -localToWorld.Forward;
                    maxDistance = 13f; // max value
                }
                else if (carComponentData.Speed >= 180)
                {
                    direction = -localToWorld.Forward;
                    maxDistance = 0.1f; //min value
                }
                else
                {
                    direction = -localToWorld.Forward;
                    maxDistance = -13f / 160f * (carComponentData.Speed - 180f);
                }


                if (physicsWorld.SphereCastAll(localToWorld.Position, radius, direction, maxDistance, ref sphereHits, CollisionFilter.Default) && sphereHits.Length > 1)
                //if (physicsWorld.SphereCastAll(localToWorld.Position, radius, -localToWorld.Forward, 5f, ref sphereHits, CollisionFilter.Default) && sphereHits.Length > 1)
                {
                    var StartR = localToWorld.Position;
                    //var EndR = localToWorld.Position + new float3(0, 0, radius);
                    var EndR = new float3();
                    if (math.all(direction == localToWorld.Forward))
                        EndR = localToWorld.Position + radius * math.normalize(localToWorld.Forward) + maxDistance * localToWorld.Forward;
                    else
                        EndR = localToWorld.Position + radius * math.normalize(localToWorld.Forward);
                    UnityEngine.Debug.DrawLine(StartR, EndR, UnityEngine.Color.red, 0);

                    //EndR = localToWorld.Position + new float3(0, 0, -radius);
                    if (math.all(direction == -localToWorld.Forward))
                        EndR = localToWorld.Position + radius * math.normalize(-localToWorld.Forward) + maxDistance * -localToWorld.Forward;
                    else
                        EndR = localToWorld.Position + radius * math.normalize(-localToWorld.Forward);
                    UnityEngine.Debug.DrawLine(StartR, EndR, UnityEngine.Color.red, 0);

                    //EndR = localToWorld.Position + new float3(radius, 0, 0);
                    EndR = localToWorld.Position + radius * math.normalize(localToWorld.Right);
                    UnityEngine.Debug.DrawLine(StartR, EndR, UnityEngine.Color.red, 0);
                    //EndR = localToWorld.Position + new float3(-radius, 0, 0);
                    EndR = localToWorld.Position + radius * math.normalize(-localToWorld.Right);
                    UnityEngine.Debug.DrawLine(StartR, EndR, UnityEngine.Color.red, 0);
                    foreach (var sphIt in sphereHits)
                    {
                        //LogError(sphIt.Entity.ToString());
                        //if (getCarComponentDataFromEntity.HasComponent(sphIt.Entity) && getCarComponentDataFromEntity[carEntity].testID == 0 && getCarComponentDataFromEntity[carEntity].testID != getCarComponentDataFromEntity[sphIt.Entity].testID)
                        if (getCarComponentDataFromEntity.HasComponent(sphIt.Entity) && carEntity.Index != sphIt.Entity.Index)
                        {
                            var localToWorldSphIt = getLocalToWorldComponentDataFromEntity[sphIt.Entity];
                            // Compute distance
                            float deltaX = localToWorld.Position.x - localToWorldSphIt.Position.x;
                            float deltaY = localToWorld.Position.y - localToWorldSphIt.Position.y;
                            float deltaZ = localToWorld.Position.z - localToWorldSphIt.Position.z;

                            float distance = (float)System.Math.Sqrt(deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ);

                            //LogError("I can see another car with testId = " + getCarComponentDataFromEntity[sphIt.Entity].testID + " , at distance: " + distance);
                            //LogError("myLocal: " + math.normalize(localToWorld.Forward) + ", other: " + math.normalize(localToWorldSphIt.Forward));

                            EndR = localToWorldSphIt.Position;
                            UnityEngine.Debug.DrawLine(StartR, EndR, UnityEngine.Color.blue, 0);
                            if (!occupiedTrackIds.Contains(getCarComponentDataFromEntity[sphIt.Entity].TrackId) && getCarComponentDataFromEntity[sphIt.Entity].TrackId != carComponentData.TrackId)
                                occupiedTrackIds.Add(getCarComponentDataFromEntity[sphIt.Entity].TrackId);
                        }
                    }
                }



                /* Assume that there exists only one admissible hit in the world with the given id*/
                if ((physicsWorld.CastRay(raycastInputLeft, ref leftHits) && leftHits.Length > 1) || (physicsWorld.CastRay(raycastInputRight, ref rightHits) && rightHits.Length > 1))
                {
                    foreach (var it in leftHits)
                    {
                        if (getTrackComponentDataFromEntity.HasComponent(it.Entity))
                        {
                            var trackComponentData = getTrackComponentDataFromEntity[it.Entity];
                            if (it.Entity.Index == carComponentData.TrackId)
                            {
                                isCollisionFound = true;
                                coll = it;
                                break;
                            }
                        }
                    }
                    if (!isCollisionFound)
                    {
                        foreach (var it in rightHits)
                        {
                            if (getTrackComponentDataFromEntity.HasComponent(it.Entity))
                            {
                                var trackComponentData = getTrackComponentDataFromEntity[it.Entity];
                                if (it.Entity.Index == carComponentData.TrackId)
                                {
                                    isCollisionFound = true;
                                    coll = it;
                                    break;
                                }
                            }
                        }
                    }

                    if(isCollisionFound)
                    {
                        var parentEntity = getParentComponentDataFromEntity[coll.Entity];
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

                            //if (newStreet && elapsedTime > 10) // Qui ci metteremo il trigger che decide di far cambiare corsia, per ora uso lui



                            //changeLane(it.Entity, HasComponent<Parent>(parentEntity.Value), entityManager, getParentComponentDataFromEntity, carEntity, carComponentData1);
                            LogError("trigger");


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

                            if (adjacentTrackIdInSameDirection[0] != -1 && !occupiedTrackIds.Contains(adjacentTrackIdInSameDirection[0])) // Before check left
                            {
                                carComponentData.TrackId = adjacentTrackIdInSameDirection[0];
                            }
                            else if (adjacentTrackIdInSameDirection[1] != -1 && !occupiedTrackIds.Contains(adjacentTrackIdInSameDirection[1]) && carComponentData.rightOvertakeAllowed) // If left not available, check right
                            {
                                carComponentData.TrackId = adjacentTrackIdInSameDirection[1];
                            }

                            carComponentData.tryOvertake = false;
                            carComponentData.lastTimeTried = elapsedTime;
                            carComponentData.rightOvertakeAllowed = false;

                            //newStreet = false

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
                    }
                }

                /*var trackComponentData = getTrackComponentDataFromEntity[it.Entity];
                if (it.Entity.Index == carComponentData.TrackId)
                {
                    hit = it;

                    isRightHit = false;
                    // Don't compute the distance with math.distance, since only the projection along the surface normal is relevant
                    distance = math.abs(math.dot(localToWorld.Position, hit.SurfaceNormal) - math.dot(hit.Position, hit.SurfaceNormal));
                    isTrackHitFound = true;

                    // hit found, no need to proceed
                    break;
                }*/
                sphereHits.Dispose();
                leftHits.Dispose();
                rightHits.Dispose();
            }
        }).Run();
    }
    /*
    [UnityEditor.DrawGizmo(UnityEditor.GizmoType.Selected | UnityEditor.GizmoType.Active)]
    static void DrawGizmoForMyScript(float3 position, UnityEditor.GizmoType gizmoType)
    {
        UnityEngine.Gizmos.color = UnityEngine.Color.green;
        UnityEngine.Gizmos.DrawSphere(position, 3f);
    }*/
}

/*
public class GizmoPlayer : UnityEngine.MonoBehaviour
{
    private MyGizmo _holder;

    public void Init(MyGizmo g)
    {
        _holder = g;
    }

    private void OnDrawGizmos(float3 position)
    {
        while (_holder.OnGizmo.Count > 0)
        {
            System.Action a;
            _holder.OnGizmo.TryDequeue(out a);
            a.Invoke();
        }
    }
}

public class MyGizmo
{
    internal System.Collections.Concurrent.ConcurrentQueue<System.Action> OnGizmo = new System.Collections.Concurrent.ConcurrentQueue<System.Action>();

    public void GizmoOneFrame(System.Action oneTimeAction)
    {
        OnGizmo.Enqueue(oneTimeAction);
    }
}*/
