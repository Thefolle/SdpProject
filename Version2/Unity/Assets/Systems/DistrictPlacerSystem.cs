using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using static UnityEngine.Debug;

public class DistrictPlacerSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var entityManager = World.EntityManager;
        //var borderStreets = new List<Entity>();
        var bottomBorderStreets = new List<Entity>();
        var topBorderStreets = new List<Entity>();

        Entities.ForEach((PrefabComponentData prefabComponentData) =>
        {
            //entityManager.Debug.LogEntityInfo(prefabComponentData.District);
            var district = entityManager.Instantiate(prefabComponentData.District);
            var translation = new Translation
            {
                Value = new float3(100, 0, 100)
            };
            entityManager.SetComponentData<Translation>(district, translation);

            var district2 = entityManager.Instantiate(prefabComponentData.District2);
            translation = new Translation
            {
                Value = translation.Value + new float3(0, 0, -1000)
            };
            entityManager.SetComponentData<Translation>(district2, translation);

            var entities = entityManager.GetAllEntities();
            foreach (var entity in entities)
            {
                if (entityManager.HasComponent<StreetComponentData>(entity))
                {
                    var streetComponentData = entityManager.GetComponentData<StreetComponentData>(entity);
                    if (streetComponentData.IsBorder)
                    {
                        if (streetComponentData.exitNumber >= 7 && streetComponentData.exitNumber <= 9) // Is BOTTOM
                        {
                            var parent = GetDistrict(entityManager, entity);
                            if (parent == Entity.Null) continue;
                            if (parent.Index == district.Index)
                            {
                                bottomBorderStreets.Add(entity);
                            }
                        }
                        else if(streetComponentData.exitNumber >= 1 && streetComponentData.exitNumber <= 3) // Is TOP
                        {
                            var parent = GetDistrict(entityManager, entity);
                            if (parent == Entity.Null) continue;
                            if (parent.Index == district2.Index)
                            {
                                topBorderStreets.Add(entity);
                            }
                        }
                    }
                }
            }

            /*LogErrorFormat("BOTTOM");
            foreach(var bottom in bottomBorderStreets)
            {
                entityManager.Debug.LogEntityInfo(bottom);
            }
            LogErrorFormat("TOP");
            foreach (var top in topBorderStreets)
            {
                entityManager.Debug.LogEntityInfo(top);
            }*/

            // Save starting OR ending cross of street of border 1
            foreach (var bottomBorderStreet in bottomBorderStreets)
            {
                //entityManager.Debug.LogEntityInfo(bottomBorderStreet);
                Entity crossToConnect = new Entity();
                var streetComponentData = entityManager.GetComponentData<StreetComponentData>(bottomBorderStreet);
                if (streetComponentData.startingCross != Entity.Null)
                {
                    crossToConnect = streetComponentData.startingCross;
                }
                else
                {
                    crossToConnect = streetComponentData.endingCross;
                }
                
                if (streetComponentData.exitNumber == 7) // Link to exitNumber 3
                {
                    foreach (var topBorderStreet in topBorderStreets)
                    {
                        var topStreetComponentData = entityManager.GetComponentData<StreetComponentData>(topBorderStreet);
                        if (topStreetComponentData.exitNumber == 3)
                        {
                            // Substitute the old street from the crossToConnect component data
                            var crossToConnectComponentData = entityManager.GetComponentData<CrossComponentData>(crossToConnect);
                            if (crossToConnectComponentData.TopStreet.Index == bottomBorderStreet.Index)
                            {
                                crossToConnectComponentData.TopStreet = topBorderStreet;
                            } 
                            else if (crossToConnectComponentData.RightStreet.Index == bottomBorderStreet.Index)
                            {
                                crossToConnectComponentData.RightStreet = topBorderStreet;
                            }
                            else if (crossToConnectComponentData.BottomStreet.Index == bottomBorderStreet.Index)
                            {
                                crossToConnectComponentData.BottomStreet = topBorderStreet;
                            }
                            else if (crossToConnectComponentData.LeftStreet.Index == bottomBorderStreet.Index)
                            {
                                crossToConnectComponentData.LeftStreet = topBorderStreet;
                            }
                            else
                            {
                                crossToConnectComponentData.CornerStreet = topBorderStreet;
                            }
                            entityManager.SetComponentData<CrossComponentData>(crossToConnect, crossToConnectComponentData);

                            if (topStreetComponentData.startingCross == Entity.Null)
                            {
                                var newStreetComponentData = topStreetComponentData;
                                newStreetComponentData.startingCross = crossToConnect;
                                newStreetComponentData.IsBorder = false;
                                entityManager.SetComponentData<StreetComponentData>(topBorderStreet, newStreetComponentData);
                            }
                            else
                            {
                                var newStreetComponentData = topStreetComponentData;
                                newStreetComponentData.endingCross = crossToConnect;
                                newStreetComponentData.IsBorder = false;
                                entityManager.SetComponentData<StreetComponentData>(topBorderStreet, newStreetComponentData);
                            }
                        }
                    }
                    //LogErrorFormat("{0}", entityManager.Exists(bottomBorderStreet));
                    entityManager.AddComponentData<AskToDespawnComponentData>(bottomBorderStreet, new AskToDespawnComponentData { Asked = true });
                }
                if (streetComponentData.exitNumber == 8) // Link to exitNumber 2
                {

                }
                if (streetComponentData.exitNumber == 9) // Link to exitNumber 1
                {

                }

                //entityManager.DestroyEntity(bottomBorderStreet);
            }


        }).WithStructuralChanges().Run();

        //foreach (var bottomBorderStreet in bottomBorderStreets)
        //{
        //    var topStreetComponentData = entityManager.GetComponentData<StreetComponentData>(bottomBorderStreet);
        //    if (topStreetComponentData.exitNumber == 7)
        //    {
        //        entityManager.DestroyEntity(bottomBorderStreet);
        //    }
        //}
        this.Enabled = false;
    }

    static Entity GetDistrict(EntityManager entityManager, Entity entity)
    {
        Entity parent;
        if (!entityManager.HasComponent<Parent>(entity))
            return Entity.Null;

        parent = entityManager.GetComponentData<Parent>(entity).Value;

        while (!entityManager.HasComponent<DistrictComponentData>(parent))
        {
            if (!entityManager.HasComponent<Parent>(parent))
                return Entity.Null;
            parent = entityManager.GetComponentData<Parent>(parent).Value;
        };

        return parent;
    }
}
