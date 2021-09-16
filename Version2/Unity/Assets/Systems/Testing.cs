using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Testing : MonoBehaviour
{
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;
    [SerializeField] private Transform pointAB;
    private float interpolateAmount;

    private void Update()
    {
        interpolateAmount = (interpolateAmount + Time.deltaTime) %1f;
        pointAB.position = Vector3.Lerp(pointA.position, pointB.position, interpolateAmount);
        pointAB.transform.LookAt(pointB);
        
    }
}
