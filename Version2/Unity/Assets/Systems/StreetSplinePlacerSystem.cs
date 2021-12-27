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
        if (elapsedTime < 2) return;

        EntityManager entityManager = World.EntityManager;
        var getChildComponentDataFromEntity = GetBufferFromEntity<Child>();
        var getTrackComponentDataFromEntity = GetComponentDataFromEntity<TrackComponentData>();
        var getLocalToWorldComponentDataFromEntity = GetComponentDataFromEntity<LocalToWorld>();
        var getParentComponentDataFromEntity = GetComponentDataFromEntity<Parent>();
        var getNonUniformScaleFromEntity = GetComponentDataFromEntity<NonUniformScale>();
        var getRotationFromEntity = GetComponentDataFromEntity<Rotation>();


        Entities.ForEach((ref TrackComponentData trackComponentData, in Entity trackEntity, in LocalToWorld localToWorld, in Translation translation) =>
        {
            if (trackComponentData.allSplinesPlaced == false)
            {
                /*Entity splineEntity = entityManager.Instantiate(streetComponentData.splineEntity);
                entityManager.SetComponentData(splineEntity, new Translation { Value = localToWorld.Position + 0.5f * math.normalize(localToWorld.Up) });*/
                // Calculating the number of splines to be placed, one each 10f, (example if lenght = 60, 2*60/10 + 1 = 13 is the number of splines)
                var lane = getParentComponentDataFromEntity[trackEntity];
                if (getParentComponentDataFromEntity.HasComponent(lane.Value))
                {
                    var ecb = new EntityCommandBuffer(Allocator.TempJob);
                    var ecb2 = new EntityCommandBuffer(Allocator.TempJob);
                    var street = getParentComponentDataFromEntity[lane.Value];

                    var streetLoc = getLocalToWorldComponentDataFromEntity[street.Value];

                    var rotation = getRotationFromEntity[street.Value];

                    //var aaa = localToWorld.Rotation.value.y;
                    /*var bbb = quaternion.EulerXYZ(localToWorld.Rotation.value.y).value.y;
                    var ccc = rotation.Value.value.y;
                    var rot = quaternion.LookRotationSafe(localToWorld.Forward, localToWorld.Up);*/

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
                    if (laneName.Contains("Forward")) isForward = true;

                    var streetNonUniformScale = getNonUniformScaleFromEntity[street.Value];
                    //Debug.LogError(streetNonUniformScale.Value.z.ToString());

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
                        else if (nSplinePlaced == 3 && nSplinesToBePlaced >= 10 && isForward || (!isForward && nSplinePlaced == nSplinesToBePlaced - 3 - 1 && nSplinesToBePlaced >= 10))
                        {
                            var newSplineComponentData = new SplineComponentData
                            {
                                id = isForward ? nSplinePlaced : (nSplinesToBePlaced - nSplinePlaced - 1),
                                Track = trackEntity,
                                isSpawner = true,
                                isForward = isForward,
                                carEntity = trackComponentData.carEntity
                            };
                            ecb.AddComponent(spline, newSplineComponentData);
                        }
                        else if (nSplinePlaced == 0 && !isForward)
                        {
                            var newSplineComponentData = new SplineComponentData
                            {
                                id = nSplinesToBePlaced - nSplinePlaced - 1,
                                Track = trackEntity,
                                isLast = true,
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
                        var a = EntityManager.GetComponentData<LocalToWorld>(spline);
                        ecb2.AddComponent(spline, new Parent { Value = trackEntity });
                        ecb2.AddComponent(spline, new LocalToParent { });
                        //ecb2.AddComponent(spline, new LocalToWorld {Value =  localToWorld.Value });
                        //ecb2.SetComponent(spline, new LocalToWorld { Value = quaternion.Euler(newTranslation.Value).value });
                        //entityManager.GetComponentData<Transform>(spline).SetParent(transform, false);

                    }
                    ecb.Playback(EntityManager);
                    ecb2.Playback(EntityManager);
                    trackComponentData.allSplinesPlaced = true;
                    ecb.Dispose();
                    ecb2.Dispose();

                    //Debug.LogError(street.Value.Index + " : " + localToWorld.Forward.ToString() + " : " + math.normalize(localToWorld.Forward).ToString() + " : " + bbb.ToString());
                }
            }
        }).WithStructuralChanges().Run();
        this.Enabled = false;
    }
}
