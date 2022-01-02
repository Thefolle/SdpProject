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
        /*
        var getTrackComponentDataFromEntity = GetComponentDataFromEntity<TrackComponentData>();
        var getLocalToWorldComponentDataFromEntity = GetComponentDataFromEntity<LocalToWorld>();
        var getParentComponentDataFromEntity = GetComponentDataFromEntity<Parent>();
        var getNonUniformScaleFromEntity = GetComponentDataFromEntity<NonUniformScale>();
        var getRotationFromEntity = GetComponentDataFromEntity<Rotation>();
        var getStreetComponentData = GetComponentDataFromEntity<StreetComponentData>();*/


        Entities.ForEach((ref TrackComponentData trackComponentData, in Entity trackEntity, in LocalToWorld localToWorld, in Translation translation) =>
        {
            if (trackComponentData.allSplinesPlaced == false)
            {
                /*Entity splineEntity = entityManager.Instantiate(streetComponentData.splineEntity);
                entityManager.SetComponentData(splineEntity, new Translation { Value = localToWorld.Position + 0.5f * math.normalize(localToWorld.Up) });*/
                // Calculating the number of splines to be placed, one each 10f, (example if lenght = 60, 2*60/10 + 1 = 13 is the number of splines)
                var lane = entityManager.GetComponentData<Parent>(trackEntity);
                //var lane = getParentComponentDataFromEntity[trackEntity];
                if (entityManager.HasComponent<Parent>(lane.Value))
                {
                    var ecb = new EntityCommandBuffer(Allocator.TempJob);
                    var ecb2 = new EntityCommandBuffer(Allocator.TempJob);

                    //var street = getParentComponentDataFromEntity[lane.Value];
                    var street = entityManager.GetComponentData<Parent>(lane.Value);

                    //var streetComponentData = getStreetComponentData[street.Value];
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
                    //else
                    //{
                    //    Debug.LogErrorFormat("figlio di puttana: {0}", startingCross.Index);
                    //    entityManager.Debug.LogEntityInfo(startingCross);
                    //}

                    //var streetLoc = getLocalToWorldComponentDataFromEntity[street.Value];
                    var streetLoc = entityManager.GetComponentData<LocalToWorld>(street.Value);
                    //var rotation = getRotationFromEntity[street.Value];

                    //var aaa = localToWorld.Rotation.value.y;
                    /*var bbb = quaternion.EulerXYZ(localToWorld.Rotation.value.y).value.y;
                    var ccc = rotation.Value.value.y;
                    var rot = quaternion.LookRotationSafe(localToWorld.Forward, localToWorld.Up);
                    Debug.LogError(rot.value + " , " + ccc);*/

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
                    bool isForward = false;
                    bool canContainSpawner = false;
                    if (laneName.Contains("Forward")) isForward = true;
                    //if (laneName.Contains("1")) canContainSpawner = true;
                    canContainSpawner = true;

                    //var streetNonUniformScale = getNonUniformScaleFromEntity[street.Value];
                    var streetNonUniformScale = entityManager.GetComponentData<NonUniformScale>(street.Value);


                    var streetLenght = streetNonUniformScale.Value.z;

                    var nSplinesToBePlaced = (int)(2 * streetNonUniformScale.Value.z / 10 + 1);

                    //var splineContainer = getChildComponentDataFromEntity[trackEntity][0].Value;

                    for (var nSplinePlaced = 0; nSplinePlaced < nSplinesToBePlaced; nSplinePlaced++)
                    {
                        var spline = entityManager.Instantiate(trackComponentData.splineEntity);
                        var newTranslation = new Translation
                        {
                            Value = math.rotate(quaternion.RotateY(math.radians(-degree)), localToWorld.Forward + nSplinePlaced * 10f * math.normalize(localToWorld.Forward) / streetNonUniformScale.Value.z - (streetNonUniformScale.Value.z * math.normalize(localToWorld.Forward) + math.normalize(localToWorld.Forward)))
                            //Value = (localToWorld.Forward + nSplinePlaced * 10f * math.normalize(localToWorld.Forward) * (math.normalize(aaa)) / streetLenght - (streetLenght * math.normalize(localToWorld.Forward) + math.normalize(localToWorld.Forward))) * new float3(1,0,0)
                        };
                        ecb.AddComponent(spline, newTranslation);

                        //var currentQuaternon = getLocalToWorldComponentDataFromEntity[spline].Rotation; // Da errore:
                        // Attempted to access ComponentDataFromEntity<Unity.Transforms.LocalToWorld> which has been invalidated by a structural change.

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

                        //var splineLocalBeforeParenting = getLocalToWorldComponentDataFromEntity[spline];
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


                        //ecb2.AddComponent(spline, new LocalToWorld {Value =  localToWorld.Value });
                        //ecb2.SetComponent(spline, new LocalToWorld { Value = quaternion.Euler(newTranslation.Value).value });
                        //entityManager.GetComponentData<Transform>(spline).SetParent(transform, false);

                    }

                    ecb.Playback(EntityManager);
                    ecb2.Playback(EntityManager);

                    ecb.Dispose();
                    ecb2.Dispose();

                    //Debug.LogError(street.Value.Index + " : " + localToWorld.Forward.ToString() + " : " + math.normalize(localToWorld.Forward).ToString() + " : " + bbb.ToString());
                }
                trackComponentData.allSplinesPlaced = true;
            }
        }).WithStructuralChanges().Run();
        this.Enabled = false;
        Debug.LogFormat("StreetSplinePlacerSystem - FINISHED PLACING");
    }
}
