using UnityEngine;

public class FinishLine : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        pojazd p = other.GetComponent<pojazd>();
        if (p == null) return;

        if (p.isGhost)
        {
            Destroy(other.gameObject);
            return;
        }

        GameplayUIManager.Instance?.AddGoldForEscapedVehicle();
        GameplayUIManager.Instance?.OnVehicleRemoved();
        Destroy(other.gameObject);
    }
}
