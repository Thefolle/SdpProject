using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Physics;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics.Systems;
using static UnityEngine.Debug;
using Unity.Collections;

/// <summary>
/// <para>This system assigns to a car the initial id to track when:
/// <list type="bullet">
/// <item>The car has spawned;</item>
/// <item>The car has physically passed from a street (cross) to a cross (street).</item>
/// </list></para>
/// </summary>
public class TrackAssignerSystem : SystemBase
{
    /// <summary>
    /// <para>For now, model this as a global variable. In future, when the spawning system will be created,
    /// you may want to improve this approach.</para>
    /// </summary>
    bool hasSpawned;

    protected override void OnStartRunning()
    {
        base.OnStartRunning();

        hasSpawned = true;
    }

    protected override void OnUpdate()
    {
        // if (condition 1 is true)
        // assign track
        // else if (condition 2 is true)
        // assign track (improvement: follow the same relative track as before)

        PhysicsWorld physicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>().PhysicsWorld;
        EntityManager entityManager = World.EntityManager;
        var getTrackComponentDataFromEntity = GetComponentDataFromEntity<TrackComponentData>();
        var getParentComponentDataFromEntity = GetComponentDataFromEntity<Parent>();
        var getStreetComponentDataFromEntity = GetComponentDataFromEntity<StreetComponentData>();

        var isSpawned = hasSpawned;
        hasSpawned = false;

        Entities.ForEach((Entity carEntity, LocalToWorld localToWorld, ref CarComponentData carComponentData) =>
        {
            if (isSpawned)
            {
                var raycastInputRight = new RaycastInput
                {
                    Start = localToWorld.Position,
                    End = localToWorld.Position + 20 * localToWorld.Right,
                    Filter = CollisionFilter.Default
                };
                var raycastInputLeft = new RaycastInput
                {
                    Start = localToWorld.Position,
                    End = localToWorld.Position + 20 * -localToWorld.Right,
                    Filter = CollisionFilter.Default
                };
                var rightHits = new NativeList<RaycastHit>(20, Allocator.TempJob);
                var leftHits = new NativeList<RaycastHit>(20, Allocator.TempJob);

                bool isTrackHitFound = false; // flag that tells whether at least one admissible hit has been found
                RaycastHit hit = default;
                var forward = math.normalize(localToWorld.Forward);

                if (physicsWorld.CastRay(raycastInputLeft, ref leftHits) && leftHits.Length > 1)
                {
                    foreach (var it in leftHits)
                    {
                        if (getTrackComponentDataFromEntity.HasComponent(it.Entity))
                        {
                            var trackComponentData = getTrackComponentDataFromEntity[it.Entity];

                            /* Does the track it.Entity belong to the current street? */
                            if (getParentComponentDataFromEntity.HasComponent(it.Entity))
                            {
                                var parent = getParentComponentDataFromEntity[it.Entity].Value;
                                if (getParentComponentDataFromEntity.HasComponent(parent))
                                {
                                    var grandParent = getParentComponentDataFromEntity[parent].Value;
                                    if (getStreetComponentDataFromEntity.HasComponent(grandParent))
                                    {
                                        // TODO: check if the street of the it.Entity track equals the assigned street of the car
                                        hit = it;

                                        isTrackHitFound = true;

                                        // hit found, no need to proceed
                                        break;
                                    }
                                }
                            } 
                        }
                    }
                }
                if (!isTrackHitFound && physicsWorld.CastRay(raycastInputRight, ref rightHits) && rightHits.Length > 1)
                {
                    foreach (var it in rightHits)
                    {
                        if (getTrackComponentDataFromEntity.HasComponent(it.Entity))
                        {
                            var trackComponentData = getTrackComponentDataFromEntity[it.Entity];

                            /* Does the track it.Entity belong to the current street? */
                            if (getParentComponentDataFromEntity.HasComponent(it.Entity))
                            {
                                var parent = getParentComponentDataFromEntity[it.Entity].Value;
                                if (getParentComponentDataFromEntity.HasComponent(parent))
                                {
                                    var grandParent = getParentComponentDataFromEntity[parent].Value;
                                    if (getStreetComponentDataFromEntity.HasComponent(grandParent))
                                    {
                                        // TODO: check if the street of the it.Entity track equals the assigned street of the car
                                        hit = it;

                                        isTrackHitFound = true;

                                        // hit found, no need to proceed
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }

                rightHits.Dispose();
                leftHits.Dispose();

                if (isTrackHitFound == false)
                {
                    /* This scenario can take place if the track is out-of-range or the car is turning at the beginning or at the end of a lane. */
                    LogError("The car with id " + carEntity.Index + " cannot find any track to follow.");
                }
                else
                {
                    carComponentData.TrackId = hit.Entity.Index;
                    Log("I've assigned track " + carComponentData.TrackId + " to car with id " + carEntity.Index);
                }
            }
        }).Run();

    }

}