using UnityEngine;

public class FinishLine : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("POJAZD")) return;

        pojazd p = other.GetComponent<pojazd>();
        if (p != null && p.isGhost)
        {
            Destroy(other.gameObject);
            return;
        }

        GameplayUIManager.Instance?.AddGoldForEscapedVehicle();
        GameplayUIManager.Instance?.OnVehicleRemoved();
        Destroy(other.gameObject);
    }
}
