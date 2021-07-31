using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

public class JsonTest : MonoBehaviour
{
    public class Street
    {
        public int id;
        public float lenght;
        public int endingIntersectionId;
        public int startingIntersectionId;
    }

    // Start is called before the first frame update
    void Start()
    {
        string jsonStr = @"[
                            { 'id' : '0', 'lenght' : '30', 'endingIntersectionId' : '1', 'startingIntersectionId' : '3' },
                            { 'id' : '1', 'lenght' : '30', 'endingIntersectionId' : '0', 'startingIntersectionId' : '2' },
                            { 'id' : '2', 'lenght' : '30', 'endingIntersectionId' : '1', 'startingIntersectionId' : '3' },
                            { 'id' : '3', 'lenght' : '30', 'endingIntersectionId' : '2', 'startingIntersectionId' : '0' },
                        ]";
        var streets = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Street>>(jsonStr);
        foreach(Street street in streets)
        {
            Debug.Log(street.id);
        }
        //Debug.Log(Newtonsoft.Json.JsonConvert.SerializeObject(streets));
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
