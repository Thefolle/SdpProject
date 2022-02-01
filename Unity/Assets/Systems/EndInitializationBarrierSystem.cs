using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Jobs;

public class EndInitializationBarrierSystem : SystemBase
{
    protected override void OnUpdate()
    {
        if (World.GetExistingSystem<DistrictPlacerSystem>().Enabled ||
            World.GetExistingSystem<GraphGeneratorSystem>().Enabled ||
            World.GetExistingSystem<BusPathFinderSystem>().Enabled ||
            World.GetExistingSystem<StreetSplinePlacerSystem>().Enabled ||
            World.GetExistingSystem<CrossTrackIndexerSystem>().Enabled)
        {
            return;
        }

        this.Enabled = false;
    }
}
