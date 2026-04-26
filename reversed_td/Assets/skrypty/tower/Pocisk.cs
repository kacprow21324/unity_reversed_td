using UnityEngine;

public class Pocisk : MonoBehaviour
{
    [Header("Ustawienia Pocisku")]
    public float predkosc = 20f;
    public float obrazenia = 20f;
    public float czasZycia = 5f;

    // True po odbiciu przez Wóz Lustrzany – leci w kierunku wieży.
    [HideInInspector] public bool odbity = false;

    private Transform cel;

    public void UstawCel(Transform nowyCel)
    {
        cel = nowyCel;
    }

    void Start()
    {
        Destroy(gameObject, czasZycia);
    }

    void Update()
    {
        if (cel == null)
        {
            Destroy(gameObject);
            return;
        }

        // Jeśli wieża docelowa została dezaktywowana, pocisk znika.
        if (odbity && !cel.gameObject.activeInHierarchy)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 kierunek = cel.position - transform.position;
        float dystans = kierunek.magnitude;

        // Odbity pocisk niszczy wieżę przy zbliżeniu (bez triggerów – wieże są na domyślnym layerze).
        if (odbity && dystans < 0.5f)
        {
            WiezaBaza w = cel.GetComponent<WiezaBaza>();
            w?.TakeDamage(obrazenia);
            Destroy(gameObject);
            return;
        }

        transform.Translate(kierunek.normalized * predkosc * Time.deltaTime, Space.World);
        transform.LookAt(cel);
    }

    void OnTriggerEnter(Collider other)
    {
        // Odbite pociski nie szkodzą pojazdom.
        if (odbity) return;

        if (other.gameObject.layer == LayerMask.NameToLayer("POJAZD"))
        {
            pojazd p = other.GetComponent<pojazd>();
            if (p != null) p.OdejmijHp(obrazenia);
            Destroy(gameObject);
        }
    }
}
