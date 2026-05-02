using UnityEngine;

// WBASIC – skupia ogień na jednym celu; priorytetuje pojazdy z tauntem (Czołg).
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
        base.Start();
        UtworzKragZasiegu(zasieg, new Color(0f, 0.4f, 1f, 0.85f));
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
            bool nieDocelowy = !_aktywnycel.IsTargetable;
            if (nieAktywny || pozaZasiegiem || nieDocelowy)
                _aktywnycel = null;
        }

        // Szukaj nowego celu z priorytetem dla tauntera
        if (_aktywnycel == null)
            _aktywnycel = pojazd.ZnajdzCelWZasiegu(transform.position, zasieg, warstwaWroga);

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
            skrypt.strzelajacaWieza = transform;
            skrypt.UstawCel(cel);
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        _aktywnycel = null;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 0.4f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, zasieg);
    }
}
