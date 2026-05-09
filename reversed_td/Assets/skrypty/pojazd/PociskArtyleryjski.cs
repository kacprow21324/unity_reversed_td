using UnityEngine;

// Pocisk wystrzelony przez Artylerię – niszczy wieże, nie pojazdy.
public class PociskArtyleryjski : MonoBehaviour
{
    public float predkosc = 28f;
    public float obrazenia = 40f;
    public float czasZycia = 8f;

    private Transform _cel;

    public void UstawCel(Transform nowyCel)
    {
        _cel = nowyCel;
    }

    void Start()
    {
        Destroy(gameObject, czasZycia);
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
        WiezaBaza w = _cel.GetComponent<WiezaBaza>();
        w?.TakeDamage(obrazenia);
        Destroy(gameObject);
    }
}
