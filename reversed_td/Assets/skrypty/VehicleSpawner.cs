using UnityEngine;

public class VehicleSpawner : MonoBehaviour
{
    public static VehicleSpawner Instance;

    [Header("Gdzie spawnowaæ?")]
    public Transform spawnPoint;

    [Header("Prefaby Fasolek (od 0 do 4)")]
    [Tooltip("Upewnij siê, ¿e kolejnoœæ zgadza siê z plikiem GameConfig!")]
    public GameObject[] vehiclePrefabs = new GameObject[5];

    void Awake()
    {
        Instance = this;
    }

    public void TrySpawnVehicle(int vehicleIndex, int cost)
    {
        // Zabezpieczenie, jeœli nie masz jeszcze wszystkich 5 prefabów
        if (vehiclePrefabs[vehicleIndex] == null)
        {
            Debug.LogWarning("Nie przypisano prefabu do tego przycisku!");
            return;
        }

        // Próba pobrania z³ota z UI Managera
        if (GameplayUIManager.Instance.TrySpendGold(cost))
        {
            // Z³oto pobrane, spawniemy fasolkê!
            Instantiate(vehiclePrefabs[vehicleIndex], spawnPoint.position, spawnPoint.rotation);
        }
        else
        {
            // Brak z³ota
            Debug.Log("Nie staæ Ciê na ten pojazd!");
        }
    }
}