using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class VehicleSpawner : MonoBehaviour
{
    public static VehicleSpawner Instance;

    [Header("Punkt Spawnu")]
    public Transform spawnPoint;
    [SerializeField] private float minOdstepSpawnu = 3f;

    [Header("Prefaby Fasolek (0-4, kolejność zgodna z GameConfig)")]
    public GameObject[] vehiclePrefabs = new GameObject[5];

    void Awake() => Instance = this;

    /// Uruchamiany przez GameplayUIManager po kliknięciu Start.
    /// Spawny odbywają się co 1.5s z rzeczywistego punku startowego.
    public void StartSpawning(List<GameplayUIManager.QueueEntry> queue, FinishLine targetFinish = null)
    {
        StartCoroutine(SpawnCoroutine(queue, targetFinish));
    }

    public void StopSpawning()
    {
        StopAllCoroutines();
    }

    public void StartSpawningGhosts(int[] vehicleIndices, FinishLine targetFinish)
    {
        StartCoroutine(GhostSpawnCoroutine(vehicleIndices, targetFinish));
    }

    IEnumerator GhostSpawnCoroutine(int[] vehicleIndices, FinishLine targetFinish)
    {
        foreach (int idx in vehicleIndices)
        {
            if (idx >= 0 && idx < vehiclePrefabs.Length && vehiclePrefabs[idx] != null)
            {
                var go = Instantiate(vehiclePrefabs[idx], spawnPoint.position, spawnPoint.rotation);
                var p  = go.GetComponent<pojazd>();
                if (p != null)
                {
                    p.isGhost      = true;
                    p.targetFinish = targetFinish;
                }
            }
            yield return new WaitUntil(() =>
            {
                Collider[] col = Physics.OverlapSphere(spawnPoint.position, minOdstepSpawnu);
                foreach (var c in col)
                    if (c.GetComponent<pojazd>() != null) return false;
                return true;
            });
        }
    }

    IEnumerator SpawnCoroutine(List<GameplayUIManager.QueueEntry> queue, FinishLine targetFinish)
    {
        foreach (var entry in queue)
        {
            int idx = entry.vehicleIndex;
            if (idx >= 0 && idx < vehiclePrefabs.Length && vehiclePrefabs[idx] != null)
            {
                var go = Instantiate(vehiclePrefabs[idx], spawnPoint.position, spawnPoint.rotation);
                if (targetFinish != null)
                {
                    var p = go.GetComponent<pojazd>();
                    if (p != null) p.targetFinish = targetFinish;
                }
                GameStatistics.Instance?.RegisterDeployedUnit(vehiclePrefabs[idx].name);
                GameplayUIManager.Instance?.OnVehicleSpawned();
            }
            yield return new WaitUntil(() =>
            {
                Collider[] col = Physics.OverlapSphere(spawnPoint.position, minOdstepSpawnu);
                foreach (var c in col)
                    if (c.GetComponent<pojazd>() != null) return false;
                return true;
            });
        }

        GameplayUIManager.Instance?.OnSpawningComplete();
    }
}
