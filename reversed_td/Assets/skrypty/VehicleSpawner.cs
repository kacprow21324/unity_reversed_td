using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class VehicleSpawner : MonoBehaviour
{
    public static VehicleSpawner Instance;

    [Header("Punkt Spawnu")]
    public Transform spawnPoint;

    [Header("Prefaby Fasolek (0-4, kolejność zgodna z GameConfig)")]
    public GameObject[] vehiclePrefabs = new GameObject[5];

    void Awake() => Instance = this;

    /// Uruchamiany przez GameplayUIManager po kliknięciu Start.
    /// Spawny odbywają się co 1.5s z rzeczywistego punku startowego.
    public void StartSpawning(List<GameplayUIManager.QueueEntry> queue)
    {
        StartCoroutine(SpawnCoroutine(queue));
    }

    IEnumerator SpawnCoroutine(List<GameplayUIManager.QueueEntry> queue)
    {
        foreach (var entry in queue)
        {
            int idx = entry.vehicleIndex;
            if (idx >= 0 && idx < vehiclePrefabs.Length && vehiclePrefabs[idx] != null)
            {
                Instantiate(vehiclePrefabs[idx], spawnPoint.position, spawnPoint.rotation);
                GameStatistics.Instance?.RegisterDeployedUnit(vehiclePrefabs[idx].name);
                GameplayUIManager.Instance?.OnVehicleSpawned();
            }
            yield return new WaitForSeconds(1.5f);
        }

        GameplayUIManager.Instance?.OnSpawningComplete();
    }
}
