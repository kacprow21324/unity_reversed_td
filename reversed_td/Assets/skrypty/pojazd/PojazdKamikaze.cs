using UnityEngine;

// AKAMIKAZE – Szybki wóz-samobójca. Gdy jakakolwiek wieża znajdzie się
// w promieniu wyzwalacza, pojazd eksploduje zadając ogromne obrażenia AoE
// wszystkim wieżom w promieniu wybuchu i niszczy siebie.
public class PojazdKamikaze : pojazd
{
    [Header("Parametry Eksplozji")]
    public float promienWyzwalacza = 4f;
    public float promienWybuchu = 6f;
    public float obrazeniaWybuchu = 150f;

    private bool _eksplodowal = false;

    protected override void Start()
    {
        maxHp = 80f;
        pancerz = 0f;
        base.Start();
        _agent.speed = 8f;
    }

    protected override void Update()
    {
        base.Update();

        if (!_eksplodowal)
            SprawdzZasiegWiez();
    }

    void SprawdzZasiegWiez()
    {
        // Sprawdź czy jakakolwiek aktywna wieża jest wystarczająco blisko
        Collider[] wszystkie = Physics.OverlapSphere(transform.position, promienWyzwalacza);
        foreach (var c in wszystkie)
        {
            if (c.GetComponent<WiezaBaza>() != null)
            {
                Eksploduj();
                return;
            }
        }
    }

    void Eksploduj()
    {
        _eksplodowal = true;

        // Obrażenia AoE wszystkim wieżom w promieniu wybuchu
        Collider[] wZasiegu = Physics.OverlapSphere(transform.position, promienWybuchu);
        foreach (var c in wZasiegu)
        {
            WiezaBaza w = c.GetComponent<WiezaBaza>();
            w?.TakeDamage(obrazeniaWybuchu);
        }

        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, promienWyzwalacza);
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, promienWybuchu);
    }
}
