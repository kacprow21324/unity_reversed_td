using UnityEngine;

// Powolny pocisk armatni – wybucha przy celu i zadaje obrażenia obszarowe (AoE).
// Przebija pancerz wszystkich trafień.
public class PociskArmatni : MonoBehaviour
{
    [Header("Parametry")]
    public float predkosc = 7f;
    public float obrazenia = 75f;
    public float promienWybuchu = 5f;
    public float czasZycia = 10f;
    public LayerMask warstwaWroga;

    private Transform _cel;
    private Vector3 _ostatniaPozycja;

    public void UstawCel(Transform nowyCel)
    {
        _cel = nowyCel;
        _ostatniaPozycja = nowyCel.position;
    }

    void Start()
    {
        Destroy(gameObject, czasZycia);
    }

    void Update()
    {
        if (_cel != null && _cel.gameObject.activeInHierarchy)
            _ostatniaPozycja = _cel.position;

        Vector3 kierunek = _ostatniaPozycja - transform.position;
        if (kierunek.magnitude < 0.4f)
        {
            Eksploduj();
            return;
        }

        transform.Translate(kierunek.normalized * predkosc * Time.deltaTime, Space.World);
        transform.LookAt(_ostatniaPozycja);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("POJAZD"))
            Eksploduj();
    }

    void Eksploduj()
    {
        Collider[] trafieni = Physics.OverlapSphere(transform.position, promienWybuchu, warstwaWroga);
        foreach (var t in trafieni)
        {
            pojazd p = t.GetComponent<pojazd>();
            p?.OdejmijHp(obrazenia, przebijaPancerz: true);
        }
        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0f);
        Gizmos.DrawWireSphere(transform.position, promienWybuchu);
    }
}
