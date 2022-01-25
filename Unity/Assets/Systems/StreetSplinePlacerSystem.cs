using Unity.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using static UnityEngine.Debug;

public class StreetSplinePlacerSystem : SystemBase
{
    protected override void OnUpdate()
    {
        double elapsedTime = Time.ElapsedTime;
        if (elapsedTime < 2 || World.GetExistingSystem<GraphGeneratorSystem>().Enabled || World.GetExistingSystem<BusPathFinderSystem>().Enabled) return;
        EntityManager entityManager = World.EntityManager;
        var getChildBuffer = GetBufferFromEntity<Child>();
        var getSplineComponentData = GetComponentDataFromEntity<SplineComponentData>();


        Entities.ForEach((ref TrackComponentData trackComponentData, in Entity trackEntity, in LocalToWorld localToWorld, in Translation translation) =>
        {
            // if the track belongs to a street
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

                    var isForward = trackComponentData.IsForward;
                    var relativeId = trackComponentData.relativeId;
                    //bool canContainSpawner = relativeId == 1;
                    bool canContainSpawner = true;

                    var streetNonUniformScale = entityManager.GetComponentData<NonUniformScale>(street.Value);


                    var streetLenght = streetNonUniformScale.Value.z;

                    var nSplinesToBePlaced = (int)(2 * streetNonUniformScale.Value.z / 10 + 1);

                    var splineBufferComponentData = ecb2.AddBuffer<SplineBufferComponentData>(trackEntity);

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

                        SplineComponentData newSplineComponentData;
                        if (nSplinePlaced == nSplinesToBePlaced - 1)
                        {
                            newSplineComponentData = new SplineComponentData
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
                            newSplineComponentData = new SplineComponentData
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
                            newSplineComponentData = new SplineComponentData
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
                            newSplineComponentData = new SplineComponentData
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

                        // Manage SplineBufferComponentData
                        //splineBufferComponentData.Insert(newSplineComponentData.id, new SplineBufferComponentData { spline = spline });
                        splineBufferComponentData.Add(new SplineBufferComponentData { spline = spline });

                        // Manage traffic light splines
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
                                                    var trafficLightComponentData = entityManager.GetComponentData<TrafficLightComponentData>(trafficLigthBorder.Value);
                                                    if (bottom)
                                                    {
                                                        if (trafficLightComponentData.Direction == Direction.Bottom)
                                                        {
                                                            entityManager.SetComponentData(trafficLigthBorder.Value, new TrafficLightComponentData
                                                            {
                                                                isGreen = trafficLightComponentData.isGreen,
                                                                Spline1 = trackComponentData.relativeId == 0 ? spline : trafficLightComponentData.Spline1,
                                                                Spline2 = trackComponentData.relativeId == 1 ? spline : trafficLightComponentData.Spline2,
                                                                Direction = trafficLightComponentData.Direction,
                                                                RelativeId = trafficLightComponentData.RelativeId
                                                            });
                                                        }
                                                    }
                                                    else if (right)
                                                    {
                                                        if (trafficLightComponentData.Direction == Direction.Right)
                                                        {
                                                            entityManager.SetComponentData(trafficLigthBorder.Value, new TrafficLightComponentData
                                                            {
                                                                isGreen = trafficLightComponentData.isGreen,
                                                                Spline1 = trackComponentData.relativeId == 0 ? spline : trafficLightComponentData.Spline1,
                                                                Spline2 = trackComponentData.relativeId == 1 ? spline : trafficLightComponentData.Spline2,
                                                                Direction = trafficLightComponentData.Direction,
                                                                RelativeId = trafficLightComponentData.RelativeId
                                                            });
                                                        }
                                                    }
                                                    else if (top)
                                                    {
                                                        if (trafficLightComponentData.Direction == Direction.Top)
                                                        {
                                                            entityManager.SetComponentData(trafficLigthBorder.Value, new TrafficLightComponentData
                                                            {
                                                                isGreen = trafficLightComponentData.isGreen,
                                                                Spline1 = trackComponentData.relativeId == 0 ? spline : trafficLightComponentData.Spline1,
                                                                Spline2 = trackComponentData.relativeId == 1 ? spline : trafficLightComponentData.Spline2,
                                                                Direction = trafficLightComponentData.Direction,
                                                                RelativeId = trafficLightComponentData.RelativeId
                                                            });
                                                        }
                                                    }
                                                    else if (left)
                                                    {
                                                        if (trafficLightComponentData.Direction == Direction.Left)
                                                        {
                                                            entityManager.SetComponentData(trafficLigthBorder.Value, new TrafficLightComponentData
                                                            {
                                                                isGreen = trafficLightComponentData.isGreen,
                                                                Spline1 = trackComponentData.relativeId == 0 ? spline : trafficLightComponentData.Spline1,
                                                                Spline2 = trackComponentData.relativeId == 1 ? spline : trafficLightComponentData.Spline2,
                                                                Direction = trafficLightComponentData.Direction,
                                                                RelativeId = trafficLightComponentData.RelativeId
                                                            });
                                                        }
                                                    }
                                                    else if (corner)
                                                    {
                                                        if (trafficLightComponentData.Direction == Direction.Corner)
                                                        {
                                                            entityManager.SetComponentData(trafficLigthBorder.Value, new TrafficLightComponentData
                                                            {
                                                                isGreen = trafficLightComponentData.isGreen,
                                                                Spline1 = trackComponentData.relativeId == 0 ? spline : trafficLightComponentData.Spline1,
                                                                Spline2 = trackComponentData.relativeId == 1 ? spline : trafficLightComponentData.Spline2,
                                                                Direction = trafficLightComponentData.Direction,
                                                                RelativeId = trafficLightComponentData.RelativeId
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

                    if (!isForward)
                    {
                        for (int i = 0; i < splineBufferComponentData.Length / 2; i++)
                        {
                            var tmp = splineBufferComponentData[i];
                            splineBufferComponentData[i] = splineBufferComponentData[splineBufferComponentData.Length - i - 1];
                            splineBufferComponentData[splineBufferComponentData.Length - i - 1] = tmp;
                        }
                    }

                    splineBufferComponentData.TrimExcess();
                    //LogFormat("{0}, {1} ({2})", splineBufferComponentData.Length, splineBufferComponentData.Capacity, trackEntity.Index);

                    ecb.Playback(EntityManager);
                    ecb2.Playback(EntityManager);

                    ecb.Dispose();
                    ecb2.Dispose();
                }

                // If track has got another track on the left with the same direction, link it

                if (entityManager.HasComponent<LaneComponentData>(lane.Value))
                {
                    var isForward = trackComponentData.IsForward;
                    var relativeId = trackComponentData.relativeId;
                    if (relativeId != 0) // Avoid to look at your left, you're at the left most lane
                    {
                        if (entityManager.HasComponent<Parent>(lane.Value))
                        {
                            var currentStreet = entityManager.GetComponentData<Parent>(lane.Value).Value;
                            foreach (var laneBrother in entityManager.GetBuffer<Child>(currentStreet))
                            {
                                if (entityManager.HasComponent<LaneComponentData>(laneBrother.Value))
                                {
                                    var leftTrack = entityManager.GetBuffer<Child>(laneBrother.Value)[0].Value;
                                    var leftTrackComponentData = entityManager.GetComponentData<TrackComponentData>(leftTrack);
                                    if (isForward == leftTrackComponentData.IsForward && relativeId - 1 == leftTrackComponentData.relativeId)
                                    {
                                        trackComponentData.leftTrack = leftTrack;
                                    }
                                }
                            }
                        }
                        else
                        {
                            //LogErrorFormat("laneName: {0}, index: {1}", laneName, lane.Value.Index);
                            //entityManager.SetName(lane.Value, "SONOIOOOOOOOOOOOOOOOO");
                        }
                    }
                }

                trackComponentData.allSplinesPlaced = true;
            }
            else // the track belongs to a cross
            {
                var ecb = new EntityCommandBuffer(Allocator.TempJob);
                var children = entityManager.GetBuffer<Child>(trackEntity);
                var splineBufferComponentData = ecb.AddBuffer<SplineBufferComponentData>(trackEntity);
                foreach (var child in children)
                {
                    splineBufferComponentData.Add(new SplineBufferComponentData { spline = child.Value });
                }

                // invert the buffer elements so that their id is increasing
                if (splineBufferComponentData.Length >= 2 && entityManager.GetComponentData<SplineComponentData>(splineBufferComponentData[0].spline).id > entityManager.GetComponentData<SplineComponentData>(splineBufferComponentData[1].spline).id)
                {
                    for (int i = 0; i < splineBufferComponentData.Length / 2; i++)
                    {
                        var tmp = splineBufferComponentData[i];
                        splineBufferComponentData[i] = splineBufferComponentData[splineBufferComponentData.Length - i - 1];
                        splineBufferComponentData[splineBufferComponentData.Length - i - 1] = tmp;
                    }
                }

                ecb.Playback(entityManager);
                ecb.Dispose();
            }
        }).WithStructuralChanges().Run();
        this.Enabled = false;
        Debug.LogFormat("StreetSplinePlacerSystem: the city has been correctly initialized. Simulation is starting...");
    }
}
