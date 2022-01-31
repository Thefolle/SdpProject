using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using static UnityEngine.Debug;

public class CrossTrackIndexerSystem : SystemBase
{

    public Dictionary<Direction, Dictionary<Direction, Dictionary<int, int>>> Indexes;
    int currentIndex;

    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        Indexes = new Dictionary<Direction, Dictionary<Direction, Dictionary<int, int>>>();
        currentIndex = 0;
    }

    protected override void OnUpdate()
    {
        if (World.GetExistingSystem<StreetSplinePlacerSystem>().Enabled) return;

        var getTrackComponentData = GetComponentDataFromEntity<TrackComponentData>();

        var entityManager = World.EntityManager;
        var ecb = new EntityCommandBuffer(Allocator.TempJob);

        Entities.ForEach((ref DynamicBuffer<EntityIndexBuffer> indexer, in CrossComponentData crossComponentData, in Entity cross, in DynamicBuffer<Child> tracks) =>
        {
            for (int i = 0; i < 45; i++)
            {
                indexer.Add(new EntityIndexBuffer { Track = Entity.Null });
            }
            
            foreach (var trackChild in tracks)
            {
                var track = trackChild.Value;
                if (getTrackComponentData.HasComponent(track))
                {
                    var trackComponentData = getTrackComponentData[track];

                    var source = trackComponentData.SourceDirection;
                    var destination = trackComponentData.DestinationDirection;
                    var relativeId = trackComponentData.relativeId;
                    int index;

                    if (Indexes.ContainsKey(source) && Indexes[source].ContainsKey(destination) && Indexes[source][destination].ContainsKey(relativeId))
                    {
                        index = Indexes[source][destination][relativeId];
                    }
                    else
                    {
                        if (!Indexes.ContainsKey(source))
                        {
                            Indexes.Add(source, new Dictionary<Direction, Dictionary<int, int>>());
                        }
                        if (!Indexes[source].ContainsKey(destination))
                        {
                            Indexes[source].Add(destination, new Dictionary<int, int>());
                        }
                        if (!Indexes[source][destination].ContainsKey(relativeId))
                        {
                            Indexes[source][destination].Add(relativeId, currentIndex);
                        }
                        index = currentIndex;
                        currentIndex++;
                    }

                    //int index = GetIndex(trackComponentData.SourceDirection, trackComponentData.DestinationDirection, trackComponentData.relativeId, Indexes, ref currentIndex);

                    indexer[index] = new EntityIndexBuffer { Track = track };
                    
                }
            }
            
            
        }).WithStructuralChanges().Run();

        ecb.Playback(entityManager);
        ecb.Dispose();

        this.Enabled = false;
    }

}
