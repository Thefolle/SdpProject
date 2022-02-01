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

        UnityEngine.Debug.LogFormat("{0}: the city has been correctly initialized. Simulation started.", this.GetType().Name);
        this.Enabled = false;
    }
}
