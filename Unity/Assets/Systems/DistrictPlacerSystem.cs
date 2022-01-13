using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using static UnityEngine.Debug;
using Domain;

public class DistrictPlacerSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var entityManager = World.EntityManager;

        City city;
        List<Entity> upperDistrictRow = new List<Entity>();

        Entities.ForEach((PrefabComponentData prefabComponentData) =>
        {
            // load the city as a json string
            string cityString = ((UnityEngine.TextAsset)UnityEngine.Resources.Load("City", typeof(UnityEngine.TextAsset))).text;
            // deserialize the string to City
            city = City.FromJson(cityString);
            var districts = city.Districts;

            var translation = new Translation
            {
                Value = new float3(100, 0, 100)
            };

            int k = 0;

            for (var i = 0; i < districts.Count; i++)
            {
                Entity thisDistrict;
                var row = districts[i];
                switch (row[0])
                {
                    case District.Sm1:
                        thisDistrict = entityManager.Instantiate(prefabComponentData.District);
                        break;

                    case District.Lg1:
                        thisDistrict = entityManager.Instantiate(prefabComponentData.District2);
                        break;

                    case District.Md1:
                        thisDistrict = entityManager.Instantiate(prefabComponentData.District3);
                        break;

                    default:
                        thisDistrict = Entity.Null;
                        LogError("Incorrect enum case, check the city.json file");
                        return;
                }
                entityManager.SetComponentData<Translation>(thisDistrict, translation);
                var tmpTranslation = translation;

                for (var j = 0; j < row.Count; j++)
                {
                    Entity rightDistrict;
                    if (i > 0) // You're not in the first row of district. You need to connect to your upper district too
                    {
                        LinkDistrictsTopBottom(entityManager, upperDistrictRow[j], thisDistrict);
                        upperDistrictRow[j] = thisDistrict;
                    }
                    else
                    {
                        upperDistrictRow.Add(thisDistrict);
                    }
                    if (j < row.Count - 1) // Spawn the district on your right if you are not the last district in the row
                    {
                        switch (row[j + 1])
                        {
                            case District.Sm1:
                                rightDistrict = entityManager.Instantiate(prefabComponentData.District);
                                break;

                            case District.Lg1:
                                rightDistrict = entityManager.Instantiate(prefabComponentData.District2);
                                break;

                            case District.Md1:
                                rightDistrict = entityManager.Instantiate(prefabComponentData.District3);
                                break;

                            default:
                                LogError("Incorrect enum case, check the city.json file");
                                return;
                        }

                        tmpTranslation = new Translation
                        {
                            Value = tmpTranslation.Value + new float3(900, 0, 0)
                        };
                        entityManager.SetComponentData<Translation>(rightDistrict, tmpTranslation);
                        LinkDistrictsLeftRight(entityManager, thisDistrict, rightDistrict);
                        thisDistrict = rightDistrict;
                    }
                }

                translation.Value += new float3(0, 0, -900);
            }
        }).WithStructuralChanges().Run();
        
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

    static void LinkDistrictsTopBottom(EntityManager entityManager, Entity topDistrict, Entity bottomDistrict)
    {
        var bottomBorderStreets = new List<Entity>();
        var topBorderStreets = new List<Entity>();
        var entities = entityManager.GetAllEntities();
        foreach (var entity in entities)
        {
            if (entityManager.HasComponent<StreetComponentData>(entity))
            {
                var streetComponentData = entityManager.GetComponentData<StreetComponentData>(entity);
                if (streetComponentData.IsBorder)
                {
                    if (streetComponentData.exitNumber >= 1 && streetComponentData.exitNumber <= 3) // Is TOP
                    {
                        var parent = GetDistrict(entityManager, entity);
                        if (parent == Entity.Null) continue;
                        if (parent.Index == bottomDistrict.Index)
                        {
                            topBorderStreets.Add(entity);
                        }
                    }
                    else if (streetComponentData.exitNumber >= 7 && streetComponentData.exitNumber <= 9) // Is BOTTOM
                    {
                        var parent = GetDistrict(entityManager, entity);
                        if (parent == Entity.Null) continue;
                        if (parent.Index == topDistrict.Index)
                        {
                            bottomBorderStreets.Add(entity);
                        }
                    }
                }
            }
        }
        
        foreach (var bottomBorderStreet in bottomBorderStreets)
        {
            Entity crossToConnect;
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
                        WeldDistricts(entityManager, crossToConnect, bottomBorderStreet, topBorderStreet, topStreetComponentData);
                    }
                }
                entityManager.AddComponentData<AskToDespawnComponentData>(bottomBorderStreet, new AskToDespawnComponentData { Asked = true });
            }
            if (streetComponentData.exitNumber == 8) // Link to exitNumber 2
            {
                foreach (var topBorderStreet in topBorderStreets)
                {
                    var topStreetComponentData = entityManager.GetComponentData<StreetComponentData>(topBorderStreet);
                    if (topStreetComponentData.exitNumber == 2)
                    {
                        // Substitute the old street from the crossToConnect component data
                        WeldDistricts(entityManager, crossToConnect, bottomBorderStreet, topBorderStreet, topStreetComponentData);
                    }
                }
                entityManager.AddComponentData<AskToDespawnComponentData>(bottomBorderStreet, new AskToDespawnComponentData { Asked = true });
            }
            if (streetComponentData.exitNumber == 9) // Link to exitNumber 1
            {
                foreach (var topBorderStreet in topBorderStreets)
                {
                    var topStreetComponentData = entityManager.GetComponentData<StreetComponentData>(topBorderStreet);
                    if (topStreetComponentData.exitNumber == 1)
                    {
                        WeldDistricts(entityManager, crossToConnect, bottomBorderStreet, topBorderStreet, topStreetComponentData);
                    }
                }
                entityManager.AddComponentData<AskToDespawnComponentData>(bottomBorderStreet, new AskToDespawnComponentData { Asked = true });
            }

        }
    }

    static void LinkDistrictsLeftRight(EntityManager entityManager, Entity leftDistrict, Entity rightDistrict)
    {
        var rightBorderStreets = new List<Entity>();
        var leftBorderStreets = new List<Entity>();
        var entities = entityManager.GetAllEntities();
        foreach (var entity in entities)
        {
            if (entityManager.HasComponent<StreetComponentData>(entity))
            {
                var streetComponentData = entityManager.GetComponentData<StreetComponentData>(entity);
                if (streetComponentData.IsBorder)
                {
                    if (streetComponentData.exitNumber >= 10 && streetComponentData.exitNumber <= 12) // Is LEFT
                    {
                        var parent = GetDistrict(entityManager, entity);
                        if (parent == Entity.Null) continue;
                        if (parent.Index == rightDistrict.Index)
                        {
                            leftBorderStreets.Add(entity);
                        }
                    }
                    else if (streetComponentData.exitNumber >= 4 && streetComponentData.exitNumber <= 6) // Is RIGHT
                    {
                        var parent = GetDistrict(entityManager, entity);
                        if (parent == Entity.Null) continue;
                        if (parent.Index == leftDistrict.Index)
                        {
                            rightBorderStreets.Add(entity);
                        }
                    }
                }
            }
        }

        foreach (var rightBorderStreet in rightBorderStreets)
        {
            Entity crossToConnect;
            var streetComponentData = entityManager.GetComponentData<StreetComponentData>(rightBorderStreet);
            if (streetComponentData.startingCross != Entity.Null)
            {
                crossToConnect = streetComponentData.startingCross;
            }
            else
            {
                crossToConnect = streetComponentData.endingCross;
            }

            if (streetComponentData.exitNumber == 6) // Link to exitNumber 10
            {
                foreach (var leftBorderStreet in leftBorderStreets)
                {
                    var leftStreetComponentData = entityManager.GetComponentData<StreetComponentData>(leftBorderStreet);
                    if (leftStreetComponentData.exitNumber == 10)
                    {
                        // Substitute the old street from the crossToConnect component data
                        WeldDistricts(entityManager, crossToConnect, rightBorderStreet, leftBorderStreet, leftStreetComponentData);
                    }
                }
                entityManager.AddComponentData<AskToDespawnComponentData>(rightBorderStreet, new AskToDespawnComponentData { Asked = true });
            }
            if (streetComponentData.exitNumber == 5) // Link to exitNumber 11
            {
                foreach (var leftBorderStreet in leftBorderStreets)
                {
                    var leftStreetComponentData = entityManager.GetComponentData<StreetComponentData>(leftBorderStreet);
                    if (leftStreetComponentData.exitNumber == 11)
                    {
                        // Substitute the old street from the crossToConnect component data
                        WeldDistricts(entityManager, crossToConnect, rightBorderStreet, leftBorderStreet, leftStreetComponentData);
                    }
                }
                entityManager.AddComponentData<AskToDespawnComponentData>(rightBorderStreet, new AskToDespawnComponentData { Asked = true });
            }
            if (streetComponentData.exitNumber == 4) // Link to exitNumber 12
            {
                foreach (var leftBorderStreet in leftBorderStreets)
                {
                    var leftStreetComponentData = entityManager.GetComponentData<StreetComponentData>(leftBorderStreet);
                    if (leftStreetComponentData.exitNumber == 12)
                    {
                        WeldDistricts(entityManager, crossToConnect, rightBorderStreet, leftBorderStreet, leftStreetComponentData);
                    }
                }
                entityManager.AddComponentData<AskToDespawnComponentData>(rightBorderStreet, new AskToDespawnComponentData { Asked = true });
            }

        }
    }

    static void WeldDistricts(EntityManager entityManager, Entity crossToConnect, Entity streetToBeDeleted, Entity streetToBeWelded,
        StreetComponentData streetToBeWeldedComponentData)
    {
        // Substitute the old street from the crossToConnect component data
        var crossToConnectComponentData = entityManager.GetComponentData<CrossComponentData>(crossToConnect);
        if (crossToConnectComponentData.TopStreet.Index == streetToBeDeleted.Index)
        {
            crossToConnectComponentData.TopStreet = streetToBeWelded;
        }
        else if (crossToConnectComponentData.RightStreet.Index == streetToBeDeleted.Index)
        {
            crossToConnectComponentData.RightStreet = streetToBeWelded;
        }
        else if (crossToConnectComponentData.BottomStreet.Index == streetToBeDeleted.Index)
        {
            crossToConnectComponentData.BottomStreet = streetToBeWelded;
        }
        else if (crossToConnectComponentData.LeftStreet.Index == streetToBeDeleted.Index)
        {
            crossToConnectComponentData.LeftStreet = streetToBeWelded;
        }
        else
        {
            crossToConnectComponentData.CornerStreet = streetToBeWelded;
        }
        entityManager.SetComponentData<CrossComponentData>(crossToConnect, crossToConnectComponentData);

        if (streetToBeWeldedComponentData.startingCross == Entity.Null)
        {
            var newStreetComponentData = streetToBeWeldedComponentData;
            newStreetComponentData.startingCross = crossToConnect;
            newStreetComponentData.IsBorder = false;
            entityManager.SetComponentData<StreetComponentData>(streetToBeWelded, newStreetComponentData);
        }
        else
        {
            var newStreetComponentData = streetToBeWeldedComponentData;
            newStreetComponentData.endingCross = crossToConnect;
            newStreetComponentData.IsBorder = false;
            entityManager.SetComponentData<StreetComponentData>(streetToBeWelded, newStreetComponentData);
        }
    }
}
