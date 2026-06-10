using UnityEngine;

// Pojedynczy kolec – po trafieniu pojazdu zadaje obrażenia i natychmiast się niszczy.
public class Kolec : MonoBehaviour
{
    public float obrazenia = 30f;

    void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        // NavMeshAgent porusza pojazdy przez Transform (bez Rigidbody), więc
        // z perspektywy fizyki pojazd = statyczny collider.
        // Żeby OnTriggerEnter działało na "Static Trigger vs Static Collider",
        // Kolec musi mieć własne Rigidbody (non-kinematic + FreezeAll).
        // Wtedy jest "Rigidbody Trigger" → wykrywa statyczne collidery pojazdów.
        var rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.useGravity  = false;
        rb.constraints = RigidbodyConstraints.FreezeAll;
    }

    void OnTriggerEnter(Collider other)
    {
        pojazd p = other.GetComponent<pojazd>();
        if (p == null || p.isGhost) return;

        p.OdejmijHp(obrazenia, przebijaPancerz: true);
        Destroy(gameObject);
    }
}
