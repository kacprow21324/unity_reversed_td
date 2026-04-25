using UnityEngine;

// Pojedynczy kolec – po trafieniu pojazdu dezaktywuje się (jednorazowy).
public class Kolec : MonoBehaviour
{
    public float obrazenia = 30f;

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer != LayerMask.NameToLayer("POJAZD")) return;

        pojazd p = other.GetComponent<pojazd>();
        p?.OdejmijHp(obrazenia);

        gameObject.SetActive(false);
    }
}
