using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Collections;

public class SemaphoreStateAssignerSystem : SystemBase
{
    protected override void OnUpdate()
    {
        if (World.GetExistingSystem<CrossTrackIndexerSystem>().Enabled) return;

        var ecb = new EntityCommandBuffer(Allocator.TempJob);

        Entities.ForEach((in TrackComponentData trackComponentData, in DynamicBuffer<SplineBufferComponentData> splineBuffer) =>
        {
            if (trackComponentData.IsOnStreet)
            {
                var spline = splineBuffer[splineBuffer.Length - 1].spline;
            
                ecb.AddComponent(spline, new SemaphoreStateComponentData { });
            }
        }).Run();

        ecb.Playback(World.EntityManager);
        ecb.Dispose();

        this.Enabled = false;
    }
}
