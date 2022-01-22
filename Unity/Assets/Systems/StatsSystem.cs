using UnityEngine;
using UnityEngine.UI;

public class StatsSystem : MonoBehaviour
{
    public Text maxNumberVehicleText;
    public Text currentVehicleNumberText;

    void Update()
    {
        int maxVehicleNumber = Globals.maxVehicleNumber;
        int currentVehicleNumber = Globals.currentVehicleNumber;
        float fillPercentage = maxVehicleNumber != 0 ? (float) currentVehicleNumber / (float) maxVehicleNumber * 100 : 0f;

        //Debug.LogErrorFormat("currentVehicleNumber: {0}, maxVehicleNumber: {1}, fillPercentage: {2}", currentVehicleNumber, maxVehicleNumber, fillPercentage);

        if (maxNumberVehicleText != null)
            maxNumberVehicleText.text = $"Max vehicle number: {maxVehicleNumber}";
        if (currentVehicleNumberText != null)
            currentVehicleNumberText.text = $"Current vehicle number: {currentVehicleNumber} [{fillPercentage}%]";
    }
}