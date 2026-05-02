using UnityEngine;

// Pojedynczy kolec – po trafieniu pojazdu zadaje obrażenia i natychmiast się niszczy.
public class Kolec : MonoBehaviour
{
    public float obrazenia = 30f;

    void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        pojazd p = other.GetComponent<pojazd>();
        if (p == null) return;

        float efektywne = Mathf.Max(1f, obrazenia - 5f);
        p.OdejmijHp(efektywne, przebijaPancerz: true);
        Destroy(gameObject);
    }
}
