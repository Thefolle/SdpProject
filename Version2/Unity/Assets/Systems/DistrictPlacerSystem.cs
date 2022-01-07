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
        //var borderStreets = new List<Entity>();
        //var bottomBorderStreets = new List<Entity>();
        //var topBorderStreets = new List<Entity>();
        //var rightBorderStreets = new List<Entity>();
        //var leftBorderStreets = new List<Entity>();
        City city;
        List<Entity> upperDistrictRow = new List<Entity>();

        Entities.ForEach((PrefabComponentData prefabComponentData) =>
        {
            /// Static version: linking district with district2 from top to bottom
            /// and linking district with district3 from left to right

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

                    case District.Sm2:
                        thisDistrict = entityManager.Instantiate(prefabComponentData.District2);
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

                            case District.Sm2:
                                rightDistrict = entityManager.Instantiate(prefabComponentData.District2);
                                break;

                            default:
                                LogError("Incorrect enum case, check the city.json file");
                                return;
                        }

                        tmpTranslation = new Translation
                        {
                            Value = tmpTranslation.Value + new float3(0, 0, -900)
                        };
                        entityManager.SetComponentData<Translation>(rightDistrict, tmpTranslation);
                        LinkDistrictsLeftRight(entityManager, thisDistrict, rightDistrict);
                        thisDistrict = rightDistrict;
                    }
                }

                translation.Value += new float3(-900, 0, 0);
            }


            ////entityManager.Debug.LogEntityInfo(prefabComponentData.District);
            //var district = entityManager.Instantiate(prefabComponentData.District);
            ////var translation = new Translation
            ////{
            ////    Value = new float3(100, 0, 100)
            ////};
            //entityManager.SetComponentData<Translation>(district, translation);

            ////var tmpTranslation = translation;

            //var district2 = entityManager.Instantiate(prefabComponentData.District);
            //translation = new Translation
            //{
            //    Value = translation.Value + new float3(0, 0, -900)
            //};
            //entityManager.SetComponentData<Translation>(district2, translation);

            //LinkDistrictsTopBottom(entityManager, district, district2);

            //var district3 = entityManager.Instantiate(prefabComponentData.District2);
            //translation = new Translation
            //{
            //    Value = tmpTranslation.Value + new float3(900, 0, 0)
            //};
            //entityManager.SetComponentData<Translation>(district3, translation);

            //LinkDistrictsLeftRight(entityManager, district, district3);

            //var entities = entityManager.GetAllEntities();
            //foreach (var entity in entities)
            //{
            //    if (entityManager.HasComponent<StreetComponentData>(entity))
            //    {
            //        var streetComponentData = entityManager.GetComponentData<StreetComponentData>(entity);
            //        if (streetComponentData.IsBorder)
            //        {
            //            if (streetComponentData.exitNumber >= 1 && streetComponentData.exitNumber <= 3) // Is TOP
            //            {
            //                var parent = GetDistrict(entityManager, entity);
            //                if (parent == Entity.Null) continue;
            //                if (parent.Index == district2.Index)
            //                {
            //                    topBorderStreets.Add(entity);
            //                }
            //            }
            //            else if (streetComponentData.exitNumber >= 4 && streetComponentData.exitNumber <= 6) // Is RIGHT
            //            {
            //                var parent = GetDistrict(entityManager, entity);
            //                if (parent == Entity.Null) continue;
            //                if (parent.Index == district.Index)
            //                {
            //                    rightBorderStreets.Add(entity);
            //                }
            //            }
            //            else if (streetComponentData.exitNumber >= 7 && streetComponentData.exitNumber <= 9) // Is BOTTOM
            //            {
            //                var parent = GetDistrict(entityManager, entity);
            //                if (parent == Entity.Null) continue;
            //                if (parent.Index == district.Index)
            //                {
            //                    bottomBorderStreets.Add(entity);
            //                }
            //            }
            //            else if (streetComponentData.exitNumber >= 10 && streetComponentData.exitNumber <= 12) // Is Left
            //            {
            //                var parent = GetDistrict(entityManager, entity);
            //                if (parent == Entity.Null) continue;
            //                if (parent.Index == district3.Index)
            //                {
            //                    leftBorderStreets.Add(entity);
            //                }
            //            }
            //        }
            //    }
            //}

            ///*LogErrorFormat("BOTTOM");
            //foreach(var bottom in bottomBorderStreets)
            //{
            //    entityManager.Debug.LogEntityInfo(bottom);
            //}
            //LogErrorFormat("TOP");
            //foreach (var top in topBorderStreets)
            //{
            //    entityManager.Debug.LogEntityInfo(top);
            //}*/

            //// LINK TOP TO BOTTOM
            //// Save starting OR ending cross of street of border 1
            //foreach (var bottomBorderStreet in bottomBorderStreets)
            //{
            //    //entityManager.Debug.LogEntityInfo(bottomBorderStreet);
            //    Entity crossToConnect = new Entity();
            //    var streetComponentData = entityManager.GetComponentData<StreetComponentData>(bottomBorderStreet);
            //    if (streetComponentData.startingCross != Entity.Null)
            //    {
            //        crossToConnect = streetComponentData.startingCross;
            //    }
            //    else
            //    {
            //        crossToConnect = streetComponentData.endingCross;
            //    }

            //    if (streetComponentData.exitNumber == 7) // Link to exitNumber 3
            //    {
            //        foreach (var topBorderStreet in topBorderStreets)
            //        {
            //            var topStreetComponentData = entityManager.GetComponentData<StreetComponentData>(topBorderStreet);
            //            if (topStreetComponentData.exitNumber == 3)
            //            {
            //                // Substitute the old street from the crossToConnect component data
            //                var crossToConnectComponentData = entityManager.GetComponentData<CrossComponentData>(crossToConnect);
            //                if (crossToConnectComponentData.TopStreet.Index == bottomBorderStreet.Index)
            //                {
            //                    crossToConnectComponentData.TopStreet = topBorderStreet;
            //                }
            //                else if (crossToConnectComponentData.RightStreet.Index == bottomBorderStreet.Index)
            //                {
            //                    crossToConnectComponentData.RightStreet = topBorderStreet;
            //                }
            //                else if (crossToConnectComponentData.BottomStreet.Index == bottomBorderStreet.Index)
            //                {
            //                    crossToConnectComponentData.BottomStreet = topBorderStreet;
            //                }
            //                else if (crossToConnectComponentData.LeftStreet.Index == bottomBorderStreet.Index)
            //                {
            //                    crossToConnectComponentData.LeftStreet = topBorderStreet;
            //                }
            //                else
            //                {
            //                    crossToConnectComponentData.CornerStreet = topBorderStreet;
            //                }
            //                entityManager.SetComponentData<CrossComponentData>(crossToConnect, crossToConnectComponentData);

            //                if (topStreetComponentData.startingCross == Entity.Null)
            //                {
            //                    var newStreetComponentData = topStreetComponentData;
            //                    newStreetComponentData.startingCross = crossToConnect;
            //                    newStreetComponentData.IsBorder = false;
            //                    entityManager.SetComponentData<StreetComponentData>(topBorderStreet, newStreetComponentData);
            //                }
            //                else
            //                {
            //                    var newStreetComponentData = topStreetComponentData;
            //                    newStreetComponentData.endingCross = crossToConnect;
            //                    newStreetComponentData.IsBorder = false;
            //                    entityManager.SetComponentData<StreetComponentData>(topBorderStreet, newStreetComponentData);
            //                }
            //            }
            //        }
            //        //LogErrorFormat("{0}", entityManager.Exists(bottomBorderStreet));
            //        entityManager.AddComponentData<AskToDespawnComponentData>(bottomBorderStreet, new AskToDespawnComponentData { Asked = true });
            //    }
            //    if (streetComponentData.exitNumber == 8) // Link to exitNumber 2
            //    {
            //        foreach (var topBorderStreet in topBorderStreets)
            //        {
            //            var topStreetComponentData = entityManager.GetComponentData<StreetComponentData>(topBorderStreet);
            //            if (topStreetComponentData.exitNumber == 2)
            //            {
            //                // Substitute the old street from the crossToConnect component data
            //                var crossToConnectComponentData = entityManager.GetComponentData<CrossComponentData>(crossToConnect);
            //                if (crossToConnectComponentData.TopStreet.Index == bottomBorderStreet.Index)
            //                {
            //                    crossToConnectComponentData.TopStreet = topBorderStreet;
            //                }
            //                else if (crossToConnectComponentData.RightStreet.Index == bottomBorderStreet.Index)
            //                {
            //                    crossToConnectComponentData.RightStreet = topBorderStreet;
            //                }
            //                else if (crossToConnectComponentData.BottomStreet.Index == bottomBorderStreet.Index)
            //                {
            //                    crossToConnectComponentData.BottomStreet = topBorderStreet;
            //                }
            //                else if (crossToConnectComponentData.LeftStreet.Index == bottomBorderStreet.Index)
            //                {
            //                    crossToConnectComponentData.LeftStreet = topBorderStreet;
            //                }
            //                else
            //                {
            //                    crossToConnectComponentData.CornerStreet = topBorderStreet;
            //                }
            //                entityManager.SetComponentData<CrossComponentData>(crossToConnect, crossToConnectComponentData);

            //                if (topStreetComponentData.startingCross == Entity.Null)
            //                {
            //                    var newStreetComponentData = topStreetComponentData;
            //                    newStreetComponentData.startingCross = crossToConnect;
            //                    newStreetComponentData.IsBorder = false;
            //                    entityManager.SetComponentData<StreetComponentData>(topBorderStreet, newStreetComponentData);
            //                }
            //                else
            //                {
            //                    var newStreetComponentData = topStreetComponentData;
            //                    newStreetComponentData.endingCross = crossToConnect;
            //                    newStreetComponentData.IsBorder = false;
            //                    entityManager.SetComponentData<StreetComponentData>(topBorderStreet, newStreetComponentData);
            //                }
            //            }
            //        }
            //        //LogErrorFormat("{0}", entityManager.Exists(bottomBorderStreet));
            //        entityManager.AddComponentData<AskToDespawnComponentData>(bottomBorderStreet, new AskToDespawnComponentData { Asked = true });
            //    }
            //    if (streetComponentData.exitNumber == 9) // Link to exitNumber 1
            //    {
            //        foreach (var topBorderStreet in topBorderStreets)
            //        {
            //            var topStreetComponentData = entityManager.GetComponentData<StreetComponentData>(topBorderStreet);
            //            if (topStreetComponentData.exitNumber == 1)
            //            {
            //                // Substitute the old street from the crossToConnect component data
            //                var crossToConnectComponentData = entityManager.GetComponentData<CrossComponentData>(crossToConnect);
            //                if (crossToConnectComponentData.TopStreet.Index == bottomBorderStreet.Index)
            //                {
            //                    crossToConnectComponentData.TopStreet = topBorderStreet;
            //                }
            //                else if (crossToConnectComponentData.RightStreet.Index == bottomBorderStreet.Index)
            //                {
            //                    crossToConnectComponentData.RightStreet = topBorderStreet;
            //                }
            //                else if (crossToConnectComponentData.BottomStreet.Index == bottomBorderStreet.Index)
            //                {
            //                    crossToConnectComponentData.BottomStreet = topBorderStreet;
            //                }
            //                else if (crossToConnectComponentData.LeftStreet.Index == bottomBorderStreet.Index)
            //                {
            //                    crossToConnectComponentData.LeftStreet = topBorderStreet;
            //                }
            //                else
            //                {
            //                    crossToConnectComponentData.CornerStreet = topBorderStreet;
            //                }
            //                entityManager.SetComponentData<CrossComponentData>(crossToConnect, crossToConnectComponentData);

            //                if (topStreetComponentData.startingCross == Entity.Null)
            //                {
            //                    var newStreetComponentData = topStreetComponentData;
            //                    newStreetComponentData.startingCross = crossToConnect;
            //                    newStreetComponentData.IsBorder = false;
            //                    entityManager.SetComponentData<StreetComponentData>(topBorderStreet, newStreetComponentData);
            //                }
            //                else
            //                {
            //                    var newStreetComponentData = topStreetComponentData;
            //                    newStreetComponentData.endingCross = crossToConnect;
            //                    newStreetComponentData.IsBorder = false;
            //                    entityManager.SetComponentData<StreetComponentData>(topBorderStreet, newStreetComponentData);
            //                }
            //            }
            //        }
            //        //LogErrorFormat("{0}", entityManager.Exists(bottomBorderStreet));
            //        entityManager.AddComponentData<AskToDespawnComponentData>(bottomBorderStreet, new AskToDespawnComponentData { Asked = true });
            //    }

            //}
            // LINK LEFT TO RIGHT
            // Save starting OR ending cross of street of border 1
            //foreach (var rightBorderStreet in rightBorderStreets)
            //{
            //    //entityManager.Debug.LogEntityInfo(bottomBorderStreet);
            //    var crossToConnect = new Entity();
            //    var streetComponentData = entityManager.GetComponentData<StreetComponentData>(rightBorderStreet);
            //    if (streetComponentData.startingCross != Entity.Null)
            //    {
            //        crossToConnect = streetComponentData.startingCross;
            //    }
            //    else
            //    {
            //        crossToConnect = streetComponentData.endingCross;
            //    }

            //    if (streetComponentData.exitNumber == 6) // Link to exitNumber 10
            //    {
            //        foreach (var leftBorderStreet in leftBorderStreets)
            //        {
            //            var leftStreetComponentData = entityManager.GetComponentData<StreetComponentData>(leftBorderStreet);
            //            if (leftStreetComponentData.exitNumber == 10)
            //            {
            //                // Substitute the old street from the crossToConnect component data
            //                var crossToConnectComponentData = entityManager.GetComponentData<CrossComponentData>(crossToConnect);
            //                if (crossToConnectComponentData.TopStreet.Index == rightBorderStreet.Index)
            //                {
            //                    crossToConnectComponentData.TopStreet = leftBorderStreet;
            //                }
            //                else if (crossToConnectComponentData.RightStreet.Index == rightBorderStreet.Index)
            //                {
            //                    crossToConnectComponentData.RightStreet = leftBorderStreet;
            //                }
            //                else if (crossToConnectComponentData.BottomStreet.Index == rightBorderStreet.Index)
            //                {
            //                    crossToConnectComponentData.BottomStreet = leftBorderStreet;
            //                }
            //                else if (crossToConnectComponentData.LeftStreet.Index == rightBorderStreet.Index)
            //                {
            //                    crossToConnectComponentData.LeftStreet = leftBorderStreet;
            //                }
            //                else
            //                {
            //                    crossToConnectComponentData.CornerStreet = leftBorderStreet;
            //                }
            //                entityManager.SetComponentData<CrossComponentData>(crossToConnect, crossToConnectComponentData);

            //                if (leftStreetComponentData.startingCross == Entity.Null)
            //                {
            //                    var newStreetComponentData = leftStreetComponentData;
            //                    newStreetComponentData.startingCross = crossToConnect;
            //                    newStreetComponentData.IsBorder = false;
            //                    entityManager.SetComponentData<StreetComponentData>(leftBorderStreet, newStreetComponentData);
            //                }
            //                else
            //                {
            //                    var newStreetComponentData = leftStreetComponentData;
            //                    newStreetComponentData.endingCross = crossToConnect;
            //                    newStreetComponentData.IsBorder = false;
            //                    entityManager.SetComponentData<StreetComponentData>(leftBorderStreet, newStreetComponentData);
            //                }
            //            }
            //        }
            //        //LogErrorFormat("{0}", entityManager.Exists(bottomBorderStreet));
            //        entityManager.AddComponentData<AskToDespawnComponentData>(rightBorderStreet, new AskToDespawnComponentData { Asked = true });
            //    }
            //    if (streetComponentData.exitNumber == 5) // Link to exitNumber 11
            //    {
            //        foreach (var leftBorderStreet in leftBorderStreets)
            //        {
            //            var leftStreetComponentData = entityManager.GetComponentData<StreetComponentData>(leftBorderStreet);
            //            if (leftStreetComponentData.exitNumber == 11)
            //            {
            //                // Substitute the old street from the crossToConnect component data
            //                var crossToConnectComponentData = entityManager.GetComponentData<CrossComponentData>(crossToConnect);
            //                if (crossToConnectComponentData.TopStreet.Index == rightBorderStreet.Index)
            //                {
            //                    crossToConnectComponentData.TopStreet = leftBorderStreet;
            //                }
            //                else if (crossToConnectComponentData.RightStreet.Index == rightBorderStreet.Index)
            //                {
            //                    crossToConnectComponentData.RightStreet = leftBorderStreet;
            //                }
            //                else if (crossToConnectComponentData.BottomStreet.Index == rightBorderStreet.Index)
            //                {
            //                    crossToConnectComponentData.BottomStreet = leftBorderStreet;
            //                }
            //                else if (crossToConnectComponentData.LeftStreet.Index == rightBorderStreet.Index)
            //                {
            //                    crossToConnectComponentData.LeftStreet = leftBorderStreet;
            //                }
            //                else
            //                {
            //                    crossToConnectComponentData.CornerStreet = leftBorderStreet;
            //                }
            //                entityManager.SetComponentData<CrossComponentData>(crossToConnect, crossToConnectComponentData);

            //                if (leftStreetComponentData.startingCross == Entity.Null)
            //                {
            //                    var newStreetComponentData = leftStreetComponentData;
            //                    newStreetComponentData.startingCross = crossToConnect;
            //                    newStreetComponentData.IsBorder = false;
            //                    entityManager.SetComponentData<StreetComponentData>(leftBorderStreet, newStreetComponentData);
            //                }
            //                else
            //                {
            //                    var newStreetComponentData = leftStreetComponentData;
            //                    newStreetComponentData.endingCross = crossToConnect;
            //                    newStreetComponentData.IsBorder = false;
            //                    entityManager.SetComponentData<StreetComponentData>(leftBorderStreet, newStreetComponentData);
            //                }
            //            }
            //        }
            //        //LogErrorFormat("{0}", entityManager.Exists(bottomBorderStreet));
            //        entityManager.AddComponentData<AskToDespawnComponentData>(rightBorderStreet, new AskToDespawnComponentData { Asked = true });
            //    }
            //    if (streetComponentData.exitNumber == 4) // Link to exitNumber 12
            //    {
            //        foreach (var leftBorderStreet in leftBorderStreets)
            //        {
            //            var leftStreetComponentData = entityManager.GetComponentData<StreetComponentData>(leftBorderStreet);
            //            if (leftStreetComponentData.exitNumber == 12)
            //            {
            //                // Substitute the old street from the crossToConnect component data
            //                var crossToConnectComponentData = entityManager.GetComponentData<CrossComponentData>(crossToConnect);
            //                if (crossToConnectComponentData.TopStreet.Index == rightBorderStreet.Index)
            //                {
            //                    crossToConnectComponentData.TopStreet = leftBorderStreet;
            //                }
            //                else if (crossToConnectComponentData.RightStreet.Index == rightBorderStreet.Index)
            //                {
            //                    crossToConnectComponentData.RightStreet = leftBorderStreet;
            //                }
            //                else if (crossToConnectComponentData.BottomStreet.Index == rightBorderStreet.Index)
            //                {
            //                    crossToConnectComponentData.BottomStreet = leftBorderStreet;
            //                }
            //                else if (crossToConnectComponentData.LeftStreet.Index == rightBorderStreet.Index)
            //                {
            //                    crossToConnectComponentData.LeftStreet = leftBorderStreet;
            //                }
            //                else
            //                {
            //                    crossToConnectComponentData.CornerStreet = leftBorderStreet;
            //                }
            //                entityManager.SetComponentData<CrossComponentData>(crossToConnect, crossToConnectComponentData);

            //                if (leftStreetComponentData.startingCross == Entity.Null)
            //                {
            //                    var newStreetComponentData = leftStreetComponentData;
            //                    newStreetComponentData.startingCross = crossToConnect;
            //                    newStreetComponentData.IsBorder = false;
            //                    entityManager.SetComponentData<StreetComponentData>(leftBorderStreet, newStreetComponentData);
            //                }
            //                else
            //                {
            //                    var newStreetComponentData = leftStreetComponentData;
            //                    newStreetComponentData.endingCross = crossToConnect;
            //                    newStreetComponentData.IsBorder = false;
            //                    entityManager.SetComponentData<StreetComponentData>(leftBorderStreet, newStreetComponentData);
            //                }
            //            }
            //        }
            //        //LogErrorFormat("{0}", entityManager.Exists(bottomBorderStreet));
            //        entityManager.AddComponentData<AskToDespawnComponentData>(rightBorderStreet, new AskToDespawnComponentData { Asked = true });
            //    }

            //    //entityManager.DestroyEntity(bottomBorderStreet);
            //}

            //entityManager.DestroyEntity(bottomBorderStreet);



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
            //entityManager.Debug.LogEntityInfo(bottomBorderStreet);
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
                //LogErrorFormat("{0}", entityManager.Exists(bottomBorderStreet));
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
                //LogErrorFormat("{0}", entityManager.Exists(bottomBorderStreet));
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
                //LogErrorFormat("{0}", entityManager.Exists(bottomBorderStreet));
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
            //entityManager.Debug.LogEntityInfo(rightBorderStreet);
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
                //LogErrorFormat("{0}", entityManager.Exists(rightBorderStreet));
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
                //LogErrorFormat("{0}", entityManager.Exists(rightBorderStreet));
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
                //LogErrorFormat("{0}", entityManager.Exists(rightBorderStreet));
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
