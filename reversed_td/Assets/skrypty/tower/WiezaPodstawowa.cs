using UnityEngine;

// WBASIC – skupia ogień na jednym celu dopóki nie zginie lub nie wyjdzie z zasięgu.
public class WiezaPodstawowa : WiezaBaza
{
    [Header("Ustawienia Ataku")]
    public float zasieg = 12f;
    public float szybkoscAtaku = 1.5f;
    public float obrazenia = 20f;
    public LayerMask warstwaWroga;

    [Header("Prefab Pocisku")]
    public GameObject prefabPocisku;
    public Transform punktStrzalu;

    private float _licznikAtaku = 0f;
    private pojazd _aktywnycel;

    protected override void Start()
    {
        maxHP = 150f;
        nagrodaZlota = 50;
        base.Start();
    }

    protected override void Update()
    {
        base.Update();
        _licznikAtaku -= Time.deltaTime;
        if (_licznikAtaku <= 0f)
            SzukajIAtakuj();
    }

    void SzukajIAtakuj()
    {
        // Weryfikuj obecny cel
        if (_aktywnycel != null)
        {
            bool nieAktywny = !_aktywnycel.gameObject.activeInHierarchy;
            bool pozaZasiegiem = Vector3.Distance(transform.position, _aktywnycel.transform.position) > zasieg;
            if (nieAktywny || pozaZasiegiem)
                _aktywnycel = null;
        }

        // Znajdź nowy cel tylko jeśli aktualny jest pusty
        if (_aktywnycel == null)
        {
            Collider[] wrogowie = Physics.OverlapSphere(transform.position, zasieg, warstwaWroga);
            foreach (var w in wrogowie)
            {
                pojazd p = w.GetComponent<pojazd>();
                if (p != null) { _aktywnycel = p; break; }
            }
        }

        if (_aktywnycel != null)
        {
            Strzelaj(_aktywnycel.transform);
            _licznikAtaku = 1f / szybkoscAtaku;
        }
    }

    void Strzelaj(Transform cel)
    {
        if (prefabPocisku == null || punktStrzalu == null) return;

        GameObject nowyPocisk = Instantiate(prefabPocisku, punktStrzalu.position, punktStrzalu.rotation);
        Pocisk skrypt = nowyPocisk.GetComponent<Pocisk>();
        if (skrypt != null)
        {
            skrypt.obrazenia = obrazenia;
            skrypt.UstawCel(cel);
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        _aktywnycel = null;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, zasieg);
    }
}
