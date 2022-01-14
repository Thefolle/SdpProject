using Unity.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class StreetSplinePlacerSystem : SystemBase
{
    protected override void OnUpdate()
    {
        double elapsedTime = Time.ElapsedTime;
        if (elapsedTime < 2 || World.GetExistingSystem<GraphGeneratorSystem>().Enabled) return;
        EntityManager entityManager = World.EntityManager;


        Entities.ForEach((ref TrackComponentData trackComponentData, in Entity trackEntity, in LocalToWorld localToWorld, in Translation translation) =>
        {
            if (trackComponentData.allSplinesPlaced == false)
            {
                // Calculating the number of splines to be placed, one each 10f, (example if lenght = 60, 2*60/10 + 1 = 13 is the number of splines)
                var lane = entityManager.GetComponentData<Parent>(trackEntity);
                if (entityManager.HasComponent<Parent>(lane.Value))
                {
                    var ecb = new EntityCommandBuffer(Allocator.TempJob);
                    var ecb2 = new EntityCommandBuffer(Allocator.TempJob);

                    var street = entityManager.GetComponentData<Parent>(lane.Value);

                    var streetComponentData = entityManager.GetComponentData<StreetComponentData>(street.Value);
                    var endingCross = streetComponentData.endingCross;
                    bool bottomOfEnding = false;
                    bool rightOfEnding = false;
                    bool topOfEnding = false;
                    bool leftOfEnding = false;
                    bool cornerOfEnding = false;
                    if (endingCross != Entity.Null)
                    {
                        var endingCrossComponentData = entityManager.GetComponentData<CrossComponentData>(endingCross);
                        if (endingCrossComponentData.BottomStreet == street.Value)
                        {
                            bottomOfEnding = true;
                        }
                        else if (endingCrossComponentData.RightStreet == street.Value)
                        {
                            rightOfEnding = true;
                        }
                        else if (endingCrossComponentData.TopStreet == street.Value)
                        {
                            topOfEnding = true;
                        }
                        else if (endingCrossComponentData.LeftStreet == street.Value)
                        {
                            leftOfEnding = true;
                        }
                        else if (endingCrossComponentData.CornerStreet == street.Value)
                        {
                            cornerOfEnding = true;
                        }
                    }

                    var startingCross = streetComponentData.startingCross;
                    bool bottomOfStarting = false;
                    bool rightOfStarting = false;
                    bool topOfStarting = false;
                    bool leftOfStarting = false;
                    bool cornerOfStarting = false;
                    if (startingCross != Entity.Null)
                    {
                        var startingCrossComponentData = entityManager.GetComponentData<CrossComponentData>(startingCross);
                        if (startingCrossComponentData.BottomStreet == street.Value)
                        {
                            bottomOfStarting = true;
                        }
                        else if (startingCrossComponentData.RightStreet == street.Value)
                        {
                            rightOfStarting = true;
                        }
                        else if (startingCrossComponentData.TopStreet == street.Value)
                        {
                            topOfStarting = true;
                        }
                        else if (startingCrossComponentData.LeftStreet == street.Value)
                        {
                            leftOfStarting = true;
                        }
                        else if (startingCrossComponentData.CornerStreet == street.Value)
                        {
                            cornerOfStarting = true;
                        }
                    }

                    var streetLoc = entityManager.GetComponentData<LocalToWorld>(street.Value);

                    var ltwForward = math.normalize(localToWorld.Forward);
                    int degree = 0;
                    if (math.abs(ltwForward.x - 1f) < 0.00001 || math.abs(ltwForward.x - (-1f)) < 0.00001)
                    {
                        if (math.abs(ltwForward.x - 1f) < 0.00001)
                            degree = 90;
                        else
                            degree = -90;
                    }
                    else if (math.abs(ltwForward.z - 1f) < 0.00001 || math.abs(ltwForward.z - (-1f)) < 0.00001)
                    {
                        if (ltwForward.z == 1f)
                            degree = 0;
                        else
                            degree = 180;
                    }
                    else if (math.abs(ltwForward.x - 0.7071067) < 0.00001 && math.abs(ltwForward.z - 0.7071067) < 0.00001 ||
                    math.abs(ltwForward.x - (-0.7071067)) < 0.00001 && math.abs(ltwForward.z - 0.7071067) < 0.00001)
                    {
                        if (math.abs(ltwForward.x - 0.7071067) < 0.00001 && math.abs(ltwForward.z - 0.7071067) < 0.00001)
                            degree = 45;
                        else
                            degree = -45;
                    }

                    var laneName = entityManager.GetName(lane.Value).ToString();
                    var laneDirection = laneName.Substring(0, laneName.IndexOf('-'));
                    var laneNumber= laneName.Substring(laneName.LastIndexOf('-') + 1);
                    bool isForward = false;
                    //bool canContainSpawner = laneNumber.Equals("1");
                    bool canContainSpawner = true;
                    if (laneName.Contains("Forward")) isForward = true;

                    //canContainSpawner = true;

                    var streetNonUniformScale = entityManager.GetComponentData<NonUniformScale>(street.Value);


                    var streetLenght = streetNonUniformScale.Value.z;

                    var nSplinesToBePlaced = (int)(2 * streetNonUniformScale.Value.z / 10 + 1);

                    for (var nSplinePlaced = 0; nSplinePlaced < nSplinesToBePlaced; nSplinePlaced++)
                    {
                        var spline = entityManager.Instantiate(trackComponentData.splineEntity);
                        var newTranslation = new Translation
                        {
                            Value = math.rotate(quaternion.RotateY(math.radians(-degree)), localToWorld.Forward + nSplinePlaced * 10f * math.normalize(localToWorld.Forward) / streetNonUniformScale.Value.z - (streetNonUniformScale.Value.z * math.normalize(localToWorld.Forward) + math.normalize(localToWorld.Forward)))
                        };
                        ecb.AddComponent(spline, newTranslation);

                        var currentQuaternon = localToWorld.Rotation.value; //in realtà questo è il current quaternon del track, non dello spline

                        var newRotation = new Rotation
                        {
                            Value = quaternion.RotateZ(math.radians(-degree))
                        };
                        ecb.AddComponent(spline, newRotation);

                        if (nSplinePlaced == nSplinesToBePlaced - 1)
                        {
                            var newSplineComponentData = new SplineComponentData
                            {
                                id = isForward ? nSplinePlaced : (nSplinesToBePlaced - nSplinePlaced - 1),
                                Track = trackEntity,
                                isLast = isForward ? true : false,
                                isForward = isForward,
                                carEntity = trackComponentData.carEntity
                            };
                            ecb.AddComponent(spline, newSplineComponentData);
                        }
                        else if (nSplinePlaced == 0)
                        {
                            var newSplineComponentData = new SplineComponentData
                            {
                                id = isForward ? nSplinePlaced : (nSplinesToBePlaced - nSplinePlaced - 1),
                                Track = trackEntity,
                                isLast = isForward ? false : true,
                                isForward = isForward,
                                carEntity = trackComponentData.carEntity
                            };
                            ecb.AddComponent(spline, newSplineComponentData);
                        }
                        else if (canContainSpawner && ((nSplinePlaced == 3 && nSplinesToBePlaced >= 10 && isForward) || (!isForward && nSplinePlaced == (nSplinesToBePlaced - 3 - 1) && nSplinesToBePlaced >= 10)))
                        {
                            var newSplineComponentData = new SplineComponentData
                            {
                                id = isForward ? nSplinePlaced : (nSplinesToBePlaced - nSplinePlaced - 1),
                                Track = trackEntity,
                                isSpawner = streetComponentData.IsBorder ? false : true,
                                isForward = isForward,
                                carEntity = trackComponentData.carEntity
                            };
                            ecb.AddComponent(spline, newSplineComponentData);
                        }
                        else
                        {
                            var newSplineComponentData = new SplineComponentData
                            {
                                id = isForward ? nSplinePlaced : (nSplinesToBePlaced - nSplinePlaced - 1),
                                Track = trackEntity,
                                isForward = isForward,
                                carEntity = trackComponentData.carEntity
                            };
                            ecb.AddComponent(spline, newSplineComponentData);
                        }

                        var a = entityManager.GetComponentData<LocalToWorld>(spline);
                        ecb2.AddComponent(spline, new Parent { Value = trackEntity });
                        ecb2.AddComponent(spline, new LocalToParent { });

                        if (endingCross != Entity.Null)
                        {
                            if ((nSplinePlaced == nSplinesToBePlaced - 1 && isForward) || (nSplinePlaced == 0 && !isForward)) // this spline is the last
                            {
                                var theCross = isForward ? endingCross : startingCross;
                                bool bottom = isForward ? bottomOfEnding : bottomOfStarting;
                                bool right = isForward ? rightOfEnding : rightOfStarting;
                                bool top = isForward ? topOfEnding : topOfStarting;
                                bool left = isForward ? leftOfEnding : leftOfStarting;
                                bool corner = isForward ? cornerOfEnding : cornerOfStarting;
                                if (entityManager.HasComponent<Child>(theCross))
                                {
                                    foreach (var child in entityManager.GetBuffer<Child>(theCross))
                                    {
                                        if (entityManager.HasComponent<TrafficLightCrossComponentData>(child.Value))
                                        {
                                            if (entityManager.HasComponent<Child>(child.Value))
                                            {
                                                foreach (var trafficLigthBorder in entityManager.GetBuffer<Child>(child.Value))
                                                {
                                                    if (bottom)
                                                    {
                                                        if (entityManager.GetName(trafficLigthBorder.Value).Contains("Bottom"))
                                                        {
                                                            var trafficLightComponentData = entityManager.GetComponentData<TrafficLightComponentData>(trafficLigthBorder.Value);
                                                            entityManager.SetComponentData(trafficLigthBorder.Value, new TrafficLightComponentData
                                                            {
                                                                isGreen = trafficLightComponentData.isGreen,
                                                                Spline1 = entityManager.GetName(lane.Value).Contains("0") ? spline : trafficLightComponentData.Spline1,
                                                                Spline2 = entityManager.GetName(lane.Value).Contains("1") ? spline : trafficLightComponentData.Spline2,
                                                            });
                                                        }
                                                    }
                                                    else if (right)
                                                    {
                                                        if (entityManager.GetName(trafficLigthBorder.Value).Contains("Right"))
                                                        {
                                                            var trafficLightComponentData = entityManager.GetComponentData<TrafficLightComponentData>(trafficLigthBorder.Value);
                                                            entityManager.SetComponentData(trafficLigthBorder.Value, new TrafficLightComponentData
                                                            {
                                                                isGreen = trafficLightComponentData.isGreen,
                                                                Spline1 = entityManager.GetName(lane.Value).Contains("0") ? spline : trafficLightComponentData.Spline1,
                                                                Spline2 = entityManager.GetName(lane.Value).Contains("1") ? spline : trafficLightComponentData.Spline2,
                                                            });
                                                        }
                                                    }
                                                    else if (top)
                                                    {
                                                        if (entityManager.GetName(trafficLigthBorder.Value).Contains("Top"))
                                                        {
                                                            var trafficLightComponentData = entityManager.GetComponentData<TrafficLightComponentData>(trafficLigthBorder.Value);
                                                            entityManager.SetComponentData(trafficLigthBorder.Value, new TrafficLightComponentData
                                                            {
                                                                isGreen = trafficLightComponentData.isGreen,
                                                                Spline1 = entityManager.GetName(lane.Value).Contains("0") ? spline : trafficLightComponentData.Spline1,
                                                                Spline2 = entityManager.GetName(lane.Value).Contains("1") ? spline : trafficLightComponentData.Spline2,
                                                            });
                                                        }
                                                    }
                                                    else if (left)
                                                    {
                                                        if (entityManager.GetName(trafficLigthBorder.Value).Contains("Left"))
                                                        {
                                                            var trafficLightComponentData = entityManager.GetComponentData<TrafficLightComponentData>(trafficLigthBorder.Value);
                                                            entityManager.SetComponentData(trafficLigthBorder.Value, new TrafficLightComponentData
                                                            {
                                                                isGreen = trafficLightComponentData.isGreen,
                                                                Spline1 = entityManager.GetName(lane.Value).Contains("0") ? spline : trafficLightComponentData.Spline1,
                                                                Spline2 = entityManager.GetName(lane.Value).Contains("1") ? spline : trafficLightComponentData.Spline2,
                                                            });
                                                        }
                                                    }
                                                    else if (corner)
                                                    {
                                                        if (entityManager.GetName(trafficLigthBorder.Value).Contains("Corner"))
                                                        {
                                                            var trafficLightComponentData = entityManager.GetComponentData<TrafficLightComponentData>(trafficLigthBorder.Value);
                                                            entityManager.SetComponentData(trafficLigthBorder.Value, new TrafficLightComponentData
                                                            {
                                                                isGreen = trafficLightComponentData.isGreen,
                                                                Spline1 = entityManager.GetName(lane.Value).Contains("0") ? spline : trafficLightComponentData.Spline1,
                                                                Spline2 = entityManager.GetName(lane.Value).Contains("1") ? spline : trafficLightComponentData.Spline2,
                                                            });
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    ecb.Playback(EntityManager);
                    ecb2.Playback(EntityManager);

                    ecb.Dispose();
                    ecb2.Dispose();
                }
                trackComponentData.allSplinesPlaced = true;
            }
        }).WithStructuralChanges().Run();
        this.Enabled = false;
        Debug.LogFormat("StreetSplinePlacerSystem: the city has been correctly initialized. Simulation is starting...");
    }
}
