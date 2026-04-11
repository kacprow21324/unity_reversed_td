using UnityEngine;

public class FinishLine : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Sprawdzamy czy to co wjecha³o to pojazd gracza
        if (other.CompareTag("POJAZD"))
        {
            if (GameplayUIManager.Instance != null)
            {
                GameplayUIManager.Instance.AddGoldForEscapedVehicle();
            }

            // Niszczymy pojazd po dodaniu z³ota
            Destroy(other.gameObject);
        }
    }
}