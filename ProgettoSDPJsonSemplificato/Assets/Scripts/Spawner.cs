using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    public class Street
    {
        public int id;
        public float lenght;
        //public int endingIntersectionId;
        //public int startingIntersectionId;
        public int x;
        public int z;
        public string direction; // per il momento ho pensato a strade di 90 gradi, direction = x | direction = z
    }

    [SerializeField]
    GameObject streetPrefab;

    List<GameObject> streetsListObject = new List<GameObject>();

    // Start is called before the first frame update
    void Start()
    {
        /*string jsonStr = @"[
                            { 'id' : '0', 'lenght' : '30', 'endingIntersectionId' : '1', 'startingIntersectionId' : '3', 'direction': 'x' },
                            { 'id' : '1', 'lenght' : '30', 'endingIntersectionId' : '0', 'startingIntersectionId' : '2', 'direction': 'x' },
                            { 'id' : '2', 'lenght' : '30', 'endingIntersectionId' : '1', 'startingIntersectionId' : '3', 'direction': 'z' },
                            { 'id' : '3', 'lenght' : '30', 'endingIntersectionId' : '2', 'startingIntersectionId' : '0', 'direction': 'z' }
                        ]";*/
        string jsonStr = @"[
                            { 'id' : '0', 'lenght' : '30', 'x' : '0', 'z' : '0', 'direction': 'x' },
                            { 'id' : '1', 'lenght' : '30', 'x' : '30', 'z' : '0', 'direction': 'x' },
                            { 'id' : '2', 'lenght' : '30', 'x' : '30', 'z' : '30', 'direction': 'z' },
                            { 'id' : '3', 'lenght' : '30', 'x' : '0', 'z' : '30', 'direction': 'z' }
                        ]";
        var streets = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Street>>(jsonStr);

        // Prima instanzio tutto
        foreach (Street street in streets)
        {
            Debug.Log(street.direction);
            GameObject newObject = Instantiate(streetPrefab, new Vector3(street.x*street.lenght, 0, street.z*street.lenght), streetPrefab.transform.rotation) as GameObject;  // instatiate the 
            if(street.direction == "x")
            {
                newObject.transform.localScale = new Vector3(street.lenght, 1, 5);
            } else
            {
                newObject.transform.localScale = new Vector3(5, 1, street.lenght);
            }
            

            streetsListObject.Add(newObject);
        }

        /*int i = 0;
        // Poi connetto / ruoto come serve
        foreach ( GameObject street in streetsListObject) // ipotizzo che la prima strada stia a 0, 0...
        {
            if(i > 0)
            {
                // cambia posizione
                if(street.transform.)
            }
            i++;
        }*/


    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
