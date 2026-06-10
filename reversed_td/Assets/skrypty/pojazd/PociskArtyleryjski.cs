using UnityEngine;

// Pocisk wystrzelony przez Artylerię – niszczy wieże, nie pojazdy.
public class PociskArtyleryjski : MonoBehaviour
{
    public float predkosc = 28f;
    public float obrazenia = 40f;
    public float czasZycia = 8f;

    [Header("Efekty")]
    public ParticleSystem smokePrefab;      // efekt dymu/ogona — assign prefab lub ParticleSystem dziecka
    public GameObject explosionPrefab;      // efekt eksplozji przy trafieniu

    private Transform _cel;
    private ParticleSystem _smokeInstance;

    public void UstawCel(Transform nowyCel)
    {
        _cel = nowyCel;
    }

    void Start()
    {
        Destroy(gameObject, czasZycia);

        if (smokePrefab != null)
        {
            _smokeInstance = Instantiate(smokePrefab, transform.position, Quaternion.identity);
            _smokeInstance.transform.SetParent(transform);
            _smokeInstance.Play();
        }
    }

    void Update()
    {
        if (_cel == null || !_cel.gameObject.activeInHierarchy)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 kierunek = _cel.position - transform.position;
        if (kierunek.magnitude < 0.5f)
        {
            Trafil();
            return;
        }

        transform.Translate(kierunek.normalized * predkosc * Time.deltaTime, Space.World);
        transform.LookAt(_cel);
    }

    void Trafil()
    {
        if (explosionPrefab != null)
        {
            GameObject fx = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            Destroy(fx, 3f);
        }

        WiezaBaza w = _cel.GetComponent<WiezaBaza>();
        w?.TakeDamage(obrazenia);
        Destroy(gameObject);
    }
}
