using UnityEngine;
using UnityEngine.UI;

public class StatsSystem : MonoBehaviour
{
    public Text maxNumberVehicleText;
    public Text currentVehicleNumberText;
    public Text numberOfDespawnedVehiclesText;
    public Text districtDescriberText;
    public Text lastSecondStats;

    private bool updateOnce = true;

    void Update()
    {
        if (Globals.startStats)
        {
            int maxVehicleNumber = Globals.maxVehicleNumber;

            int currentVehicleNumber = Globals.currentVehicleNumber;
            int currentBusesNumber = Globals.currentBusNumber;
            int currentCarsNumber = currentVehicleNumber - currentBusesNumber;

            int numberDespawnedVehicles = Globals.numberDespawnedVehicles;
            int numberOfVehicleSpawnedInLastSecond = Globals.numberOfVehicleSpawnedInLastSecond;
            int numberOfVehicleDespawnedInLastSecond = Globals.numberOfVehicleDespawnedInLastSecond;
            int trend = numberOfVehicleSpawnedInLastSecond - numberOfVehicleDespawnedInLastSecond;

            float fillPercentage = maxVehicleNumber != 0 ? (float)currentVehicleNumber / (float)maxVehicleNumber * 100 : 0f;

            int numberOfSmallDistricts = Globals.numberOfSmallDistricts;
            int numberOfMediumDistricts = Globals.numberOfMediumDistricts;
            int numberOfLargeDistricts = Globals.numberOfLargeDistricts;
            int totNumberOfDistricts = numberOfSmallDistricts + numberOfMediumDistricts + numberOfLargeDistricts;


            //Debug.LogErrorFormat("currentVehicleNumber: {0}, maxVehicleNumber: {1}, fillPercentage: {2}", currentVehicleNumber, maxVehicleNumber, fillPercentage);

            if (updateOnce)
            {
                if (maxNumberVehicleText != null)
                    maxNumberVehicleText.text = maxVehicleNumber != -1 ?  $"Max vehicle number: {maxVehicleNumber}" : "No max vehicle number limit";
                if (districtDescriberText != null)
                    districtDescriberText.text = $"District Describer [{totNumberOfDistricts} Total]\n {numberOfSmallDistricts} low street density districts\n {numberOfMediumDistricts} medium street density districts\n {numberOfLargeDistricts} high street density districts";
                updateOnce = false;
            }

            if (currentVehicleNumberText != null)
                currentVehicleNumberText.text =  maxVehicleNumber != -1 ? $"Current vehicle number: {currentVehicleNumber} [{fillPercentage}%]\n[{currentCarsNumber} Cars, {currentBusesNumber} Buses]" : $"Current vehicle number: {currentVehicleNumber}\n[{currentCarsNumber} Cars, {currentBusesNumber} Buses]";
            if (numberOfDespawnedVehiclesText != null)
                numberOfDespawnedVehiclesText.text = $"Number of despawned vehicles: {numberDespawnedVehicles}";
            if (lastSecondStats != null)
                lastSecondStats.text = $"Last Second Stats:\nSpawned = {numberOfVehicleSpawnedInLastSecond} | Despawned = {numberOfVehicleDespawnedInLastSecond} [Trend = {trend}]";
        }
    }    
}