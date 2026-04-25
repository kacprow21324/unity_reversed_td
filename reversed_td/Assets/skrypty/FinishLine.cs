using UnityEngine;

public class FinishLine : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("POJAZD")) return;

        GameplayUIManager.Instance?.AddGoldForEscapedVehicle();
        GameplayUIManager.Instance?.OnVehicleRemoved();
        Destroy(other.gameObject);
    }
}
