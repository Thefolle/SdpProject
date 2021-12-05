using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Physics;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics.Systems;
using static UnityEngine.Debug;
using Unity.Collections;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public class AntiCollisionSystem : SystemBase
{
    private const int EditorFactor = 2;

    private const float LaneWidth = 2.5f * EditorFactor;

    protected override void OnUpdate()
    {
        float deltaTime = Time.DeltaTime;
        double elapsedTime = Time.ElapsedTime;
        if (elapsedTime < 2) return;

        PhysicsWorld physicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>().PhysicsWorld;
        EntityManager entityManager = World.EntityManager;
        var getTrackComponentDataFromEntity = GetComponentDataFromEntity<TrackComponentData>();
        var getParentComponentDataFromEntity = GetComponentDataFromEntity<Parent>();
        var getLaneComponentDataFromEntity = GetComponentDataFromEntity<LaneComponentData>();
        var getCrossComponentDataFromEntity = GetComponentDataFromEntity<CrossComponentData>();
        var getObstaclesComponentDataFromEntity = GetComponentDataFromEntity<ObstaclesComponent>();
        var getCarComponentDataFromEntity = GetComponentDataFromEntity<CarComponentData>();
        var getTrafficLightComponentDataFromEntity = GetComponentDataFromEntity<TrafficLightComponentData>();
        var getTrafficLightCrossComponentDataFromEntity = GetComponentDataFromEntity<TrafficLightCrossComponentData>();
        var getStreetComponentDataFromEntity = GetComponentDataFromEntity<StreetComponentData>();

        Entities.ForEach((ref PhysicsVelocity physicsVelocity, ref CarComponentData carComponentData, in Entity carEntity, in LocalToWorld localToWorld) =>
        {
            // Anti-collision Raycasts
            /*
            float speedFactor;                   // This factor is for regulating the raycasts wrt the car velocity
            if (carComponentData.Speed >= 20)
            {
                if(carComponentData.Speed > 800)
                    speedFactor = 0.01f * 800; // Max cap
                else
                    speedFactor = 0.01f * carComponentData.Speed;
            }
            else
            {
                speedFactor = 0;
            }
            //speedFactor = 0.03f * carComponentData.Speed;

            var StartR = new float3();
            var EndR = new float3();
            var StartL = new float3();
            var EndL = new float3();

            if (carComponentData.tryOvertake == false)
            {
                StartR = localToWorld.Position + 1.66f * localToWorld.Forward + 1 * localToWorld.Right;
                EndR = localToWorld.Position + 2.5f * localToWorld.Forward + speedFactor * localToWorld.Forward + 1 * localToWorld.Right;
                StartL = localToWorld.Position + 1.66f * localToWorld.Forward - 1 * localToWorld.Right;
                EndL = localToWorld.Position + 2.5f * localToWorld.Forward + speedFactor * localToWorld.Forward - 1 * localToWorld.Right;
            } else // is overtaking, less starting value of anti-collision raycast 
            {
                StartR = localToWorld.Position + 1.66f * localToWorld.Forward + 1 * localToWorld.Right;
                EndR = localToWorld.Position + 2f * localToWorld.Forward + speedFactor * localToWorld.Forward + 1 * localToWorld.Right;
                StartL = localToWorld.Position + 1.66f * localToWorld.Forward - 1 * localToWorld.Right;
                EndL = localToWorld.Position + 2f * localToWorld.Forward + speedFactor * localToWorld.Forward - 1 * localToWorld.Right;
            }

            //var StartR = localToWorld.Position + 1 * localToWorld.Forward + 1 * localToWorld.Right;
            //var EndR = localToWorld.Position + 2.5f * localToWorld.Forward + speedFactor * localToWorld.Forward + 1 * localToWorld.Right;
            var raycastCollisionRight = new RaycastInput
            {
                // Start = localToWorld.Position + x0 * localToWorld.Forward + y0 * localToWorld.Right,    // Assign the value x0 and y0 in order to be positioned at the front extreme right side of the car.
                // End = localToWorld.Position + x1 * localToWorld.Forward + speedFactor * localToWorld.Forward + y0 * localToWorld.Right,      // x1 must be fixed, speedfactor is variable wrt the car velocity
                Start = StartR,
                End = EndR,
                Filter = CollisionFilter.Default
            };
            UnityEngine.Debug.DrawLine(StartR, EndR, UnityEngine.Color.green, 0);
            //var StartL = localToWorld.Position + 1 * localToWorld.Forward - 1 * localToWorld.Right;
            //var EndL = localToWorld.Position + 2.5f * localToWorld.Forward + speedFactor* localToWorld.Forward - 1 * localToWorld.Right;
            var raycastCollisionLeft = new RaycastInput
            {
                Start = StartL,
                End = EndL,
                Filter = CollisionFilter.Default
            };
            UnityEngine.Debug.DrawLine(StartL, EndL, UnityEngine.Color.green, 0);
            var rightCollision = new NativeList<RaycastHit>(20, Allocator.TempJob);
            var leftCollision = new NativeList<RaycastHit>(20, Allocator.TempJob);
            bool isCollisionFound = false; // flag that tells whether at least one admissible hit has been found
            RaycastHit coll = default;
            // Assume that there exists only one admissible hit in the world with the given id
            if ((physicsWorld.CastRay(raycastCollisionLeft, ref leftCollision) && leftCollision.Length >= 1) || (physicsWorld.CastRay(raycastCollisionRight, ref rightCollision) && rightCollision.Length >= 1))
            {
                foreach (var i in leftCollision)
                {
                    if (!getTrackComponentDataFromEntity.HasComponent(i.Entity) && !isCollisionFound && i.Entity.Index != carEntity.Index)
                    {
                        if (getCarComponentDataFromEntity.HasComponent(i.Entity))
                        {
                            if (getCarComponentDataFromEntity[i.Entity].TrackId == getCarComponentDataFromEntity[carEntity].TrackId)
                            {
                                coll = i;
                                isCollisionFound = true;
                                break;
                            }
                        }
                        else
                        {
                            coll = i;
                            isCollisionFound = true;
                            break;
                        }
                    }
                }
                if (!isCollisionFound)
                    foreach (var j in rightCollision)
                    {
                        if (!getTrackComponentDataFromEntity.HasComponent(j.Entity) && !isCollisionFound && j.Entity.Index != carEntity.Index)
                        {
                            if (getCarComponentDataFromEntity.HasComponent(j.Entity))
                            {
                                if (getCarComponentDataFromEntity[j.Entity].TrackId == getCarComponentDataFromEntity[carEntity].TrackId)
                                {
                                    coll = j;
                                    isCollisionFound = true;
                                    break;
                                }
                            }
                            else
                            {
                                coll = j;
                                isCollisionFound = true;
                                break;
                            }
                        }
                    }
            }

            var slowDownTo0 = false;
            if (isCollisionFound)     // Braking method in case of raycast collision with another car
            {

                // SISTEMA LAMPEGGIO - Michele
                if (getCarComponentDataFromEntity.HasComponent(coll.Entity))
                {
                    slowDownTo0 = true;
                    var othercarComponentData = getCarComponentDataFromEntity[coll.Entity];

                    if (carComponentData.maxSpeed > othercarComponentData.maxSpeed)
                    {
                        if (carComponentData.Speed > othercarComponentData.Speed - 2 && carComponentData.Speed < othercarComponentData.Speed + 2) // myCar has more maxSpeed, but is capped by otherCar in lane
                        {
                            if ((carComponentData.lastTimeTried == -1 || math.abs(carComponentData.lastTimeTried - elapsedTime) > 10) && othercarComponentData.Speed == 0f) // Avoid spam-trying
                            {
                                LogError("Asked for overtake");
                                carComponentData.tryOvertake = true;
                                carComponentData.rightOvertakeAllowed = true;
                            } else if ((carComponentData.lastTimeTried == -1 || math.abs(carComponentData.lastTimeTried - elapsedTime) > 10) && othercarComponentData.Speed != 0f)
                            {
                                LogError("Asked for overtake");
                                carComponentData.tryOvertake = true;
                                carComponentData.rightOvertakeAllowed = false;
                            }
                        }
                        if (carComponentData.Speed > 20 && othercarComponentData.Speed < 10)
                        {
                            if ((carComponentData.lastTimeTried == -1 || math.abs(carComponentData.lastTimeTried - elapsedTime) > 10))
                            {
                                LogError("Asked for overtake");
                                carComponentData.tryOvertake = true;
                                carComponentData.rightOvertakeAllowed = true;
                            }
                        }
                    }
                }
                else if(getTrafficLightComponentDataFromEntity.HasComponent(coll.Entity))
                {
                    var trafficLight = getTrafficLightComponentDataFromEntity[coll.Entity];
                    if (carComponentData.vehicleIsOn == VehicleIsOn.Street) // If you are already on the cross: free the cross
                    {
                        /*var trafficLightCross = getParentComponentDataFromEntity[coll.Entity];
                        if (getTrafficLightCrossComponentDataFromEntity.HasComponent(trafficLightCross.Value))
                        {
                            var trafficLightNumber = entityManager.GetName(coll.Entity).Substring(entityManager.GetName(coll.Entity).LastIndexOf('-') + 1);
                            var trafficLightCrossComponentData = getTrafficLightCrossComponentDataFromEntity[trafficLightCross.Value];

                            //LogError("trafficLightNumber: " + trafficLightNumber + ", isTurnOf: " + trafficLightCrossComponentData.greenTurn);
                            if (trafficLightNumber != trafficLightCrossComponentData.greenTurn.ToString())
                            {
                                slowDownTo0 = true;
                            }
                            else
                            {
                                slowDownTo0 = false;
                            }
                        }*/
            /*
                        if(trafficLight.isGreen)
                            slowDownTo0 = false;
                        else
                            slowDownTo0 = true;
                    }
                    //LogError("Traffic Light is green: " + trafficLight.isGreen);
                }

                if(slowDownTo0)
                    if (carComponentData.Speed < 10)
                        carComponentData.Speed = 0;
                    else
                        carComponentData.Speed -= 0.01f * carComponentData.maxSpeed;        // that 0.10 is the braking factor. It reduces the car speed of 10% of the initial speed (it is just an example, we may change it to a proper value)
            }
            if(!isCollisionFound || slowDownTo0 == false)
            {
                if (carComponentData.Speed > carComponentData.maxSpeed)
                    carComponentData.Speed = carComponentData.maxSpeed;
                else
                    carComponentData.Speed += 0.003f * carComponentData.maxSpeed;
            }
            leftCollision.Dispose();
            rightCollision.Dispose();
            */

            //V2
            float speedFactor;                   // This factor is for regulating the spherecast wrt the car velocity

            var radius = LaneWidth * 0.5f;
            if (carComponentData.Speed >= 100)
            {
                if (carComponentData.Speed > 800)
                    speedFactor = 0.2f + 0.01f * 800; // Max cap
                else
                    speedFactor = 0.2f + 0.01f * carComponentData.Speed;
            }
            else
            {
                speedFactor = 0.2f;
            }

            /*if (carComponentData.myTrafficLight == true && carComponentData.lastTimeMyTrafficLight != -1 && (elapsedTime - carComponentData.lastTimeMyTrafficLight) > 3f)
            {
                carComponentData.myTrafficLight = false;
            }*/


            //var radius = LaneWidth * 1f;
            /*var radius = new float();
            if (carComponentData.Speed >= 200)
            {
                radius = LaneWidth * 1f;
            }
            else
            {
                radius = LaneWidth * 0.5f;
            }*/


            var sphereHits = new NativeList<ColliderCastHit>(20, Allocator.TempJob);
            bool isCollisionFound = false; // flag that tells whether at least one admissible hit has been found
            ColliderCastHit coll = default;
            
            var direction = localToWorld.Forward;
            var maxDistance = speedFactor;

            var StartR = localToWorld.Position + 8f * math.normalize(localToWorld.Forward);
            if (physicsWorld.SphereCastAll(StartR, radius, direction, maxDistance, ref sphereHits, CollisionFilter.Default) && sphereHits.Length >= 1)
            {
                var EndR = new float3();
                EndR = StartR + radius * math.normalize(localToWorld.Forward) + maxDistance * localToWorld.Forward;
                UnityEngine.Debug.DrawLine(StartR, EndR, UnityEngine.Color.blue, 0);
                EndR = StartR + radius * math.normalize(-localToWorld.Forward);
                UnityEngine.Debug.DrawLine(StartR, EndR, UnityEngine.Color.blue, 0);
                EndR = StartR + radius * math.normalize(localToWorld.Right);
                UnityEngine.Debug.DrawLine(StartR, EndR, UnityEngine.Color.blue, 0);
                EndR = StartR + radius * math.normalize(-localToWorld.Right);
                UnityEngine.Debug.DrawLine(StartR, EndR, UnityEngine.Color.blue, 0);
                foreach (var i in sphereHits)
                {
                    if (!isCollisionFound && i.Entity.Index != carEntity.Index)
                    {
                        if (getCarComponentDataFromEntity.HasComponent(i.Entity))
                        {
                            if (getCarComponentDataFromEntity[i.Entity].TrackId == getCarComponentDataFromEntity[carEntity].TrackId)
                            {
                                coll = i;
                                isCollisionFound = true;
                                //Log("Car with id" + carEntity.Index + "hit car with id" + i.Entity.Index);
                                break;
                            }
                        }
                        //else if (getTrafficLightComponentDataFromEntity.HasComponent(i.Entity) && !carComponentData.myTrafficLight)
                        else if (getTrafficLightComponentDataFromEntity.HasComponent(i.Entity))
                        {

                            //carComponentData.myTrafficLight = true;

                            //LogError("Ho pijato er semaforo fratè. " + "id semaforo:" + i.Entity.Index);
                            coll = i;
                            isCollisionFound = true;
                            break;
                        }
                        else if (getObstaclesComponentDataFromEntity.HasComponent(i.Entity))
                        {
                            LogError("An obstacle has been hit");
                            coll = i;
                            isCollisionFound = true;
                            break;
                        }
                    }
                }
                var slowDownTo0 = false;
                if (isCollisionFound)     // Braking method in case of raycast collision with another car
                {

                    // SISTEMA LAMPEGGIO - Michele
                    if (getCarComponentDataFromEntity.HasComponent(coll.Entity))
                    {
                        slowDownTo0 = true;
                        var othercarComponentData = getCarComponentDataFromEntity[coll.Entity];

                        if (carComponentData.maxSpeed > othercarComponentData.maxSpeed)
                        {
                            if (carComponentData.Speed > othercarComponentData.Speed - 30 && carComponentData.Speed < othercarComponentData.Speed + 30) // myCar has more maxSpeed, but is capped by otherCar in lane
                            {
                                if ((carComponentData.lastTimeTried == -1 || math.abs(carComponentData.lastTimeTried - elapsedTime) > 10) && othercarComponentData.Speed == 0f) // Avoid spam-trying
                                {
                                    LogError("Asked for overtake");
                                    carComponentData.tryOvertake = true;
                                    carComponentData.rightOvertakeAllowed = true;
                                }
                                else if ((carComponentData.lastTimeTried == -1 || math.abs(carComponentData.lastTimeTried - elapsedTime) > 10) && othercarComponentData.Speed != 0f)
                                {
                                    LogError("Asked for overtake");
                                    carComponentData.tryOvertake = true;
                                    carComponentData.rightOvertakeAllowed = false;
                                }
                            }
                            if (carComponentData.Speed > 20 && othercarComponentData.Speed < 10)
                            {
                                if ((carComponentData.lastTimeTried == -1 || math.abs(carComponentData.lastTimeTried - elapsedTime) > 10))
                                {
                                    LogError("Asked for overtake");
                                    carComponentData.tryOvertake = true;
                                    carComponentData.rightOvertakeAllowed = true;
                                }
                            }
                        }
                    }
                    else if (getTrafficLightComponentDataFromEntity.HasComponent(coll.Entity))
                    {
                        var trafficLight = getTrafficLightComponentDataFromEntity[coll.Entity];
                        //if (carComponentData.vehicleIsOn == VehicleIsOn.Street) // If you are already on the cross: free the cross
                        if (carComponentData.isOnStreet)
                        {
                            /*var trafficLightCross = getParentComponentDataFromEntity[coll.Entity];
                            if (getTrafficLightCrossComponentDataFromEntity.HasComponent(trafficLightCross.Value))
                            {
                                var trafficLightNumber = entityManager.GetName(coll.Entity).Substring(entityManager.GetName(coll.Entity).LastIndexOf('-') + 1);
                                var trafficLightCrossComponentData = getTrafficLightCrossComponentDataFromEntity[trafficLightCross.Value];

                                //LogError("trafficLightNumber: " + trafficLightNumber + ", isTurnOf: " + trafficLightCrossComponentData.greenTurn);
                                if (trafficLightNumber != trafficLightCrossComponentData.greenTurn.ToString())
                                {
                                    slowDownTo0 = true;
                                }
                                else
                                {
                                    slowDownTo0 = false;
                                }
                            }*/

                            /*
                             * vedo lane sulla quale mi trovo, può essere:
                             * a) Forward
                             * b) Backward
                             * 
                             * Se a) allora devo rispettare il semaforo solo se appartiene al cross Ending della mia strada
                             * Se b) allora devo rispettare il semaforo solo se appartiene al cross Starting della mia strada
                             */

                            
                            
                            var laneName = entityManager.GetName(carComponentData.TrackParent).ToString();
                            if (laneName.Contains("Lane"))
                            {
                                var CrossToListen = new Entity();
                                var street = getParentComponentDataFromEntity[carComponentData.TrackParent];
                                var trafficLightCrossApproaching = getParentComponentDataFromEntity[coll.Entity];
                                var crossApproaching = getParentComponentDataFromEntity[trafficLightCrossApproaching.Value];


                                if(getStreetComponentDataFromEntity.HasComponent(street.Value))
                                {
                                    var streetComponentData = getStreetComponentDataFromEntity[street.Value];
                                    var laneDirection = laneName.Substring(0, laneName.IndexOf('-'));
                                    if (laneDirection.Contains("Forward"))
                                    {
                                        CrossToListen = streetComponentData.endingCross;
                                    }
                                    else if (laneDirection.Contains("Backward"))
                                    {
                                        CrossToListen = streetComponentData.startingCross;
                                    }

                                    if (CrossToListen.Index == crossApproaching.Value.Index)
                                    {
                                        /* Additional check, if VehicleIsOn.Street but actually is half on the cross
                                         * Check if the lane the vehicle is staing on c
                                         */


                                        if (trafficLight.isGreen)
                                        {
                                            /*carComponentData.myTrafficLight = true;
                                            carComponentData.lastTimeMyTrafficLight = elapsedTime;*/
                                            slowDownTo0 = false;
                                        }
                                        else
                                        {
                                            slowDownTo0 = true;
                                        }
                                    }
                                }
                            }
                        }
                        //LogError("Traffic Light is green: " + trafficLight.isGreen);
                    }
                }
                if (slowDownTo0)
                {
                    //Log("sto rallentando");
                    if (carComponentData.Speed < 10)
                        carComponentData.Speed = 0;
                    else
                        carComponentData.Speed -= 0.03f * carComponentData.maxSpeed;        // that 0.10 is the braking factor. It reduces the car speed of 10% of the initial speed (it is just an example, we may change it to a proper value)
                }
                else
                {
                    if (carComponentData.Speed > carComponentData.maxSpeed)
                        carComponentData.Speed = carComponentData.maxSpeed;
                    else
                        carComponentData.Speed += 0.003f * carComponentData.maxSpeed;
                }
            }
            sphereHits.Dispose();
        }).Run();
}
}
