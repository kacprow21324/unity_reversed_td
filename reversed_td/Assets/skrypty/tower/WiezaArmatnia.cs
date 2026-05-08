using UnityEngine;

// WCANON – wolne przeładowanie, potężne pociski AoE przebijające pancerz.
// Priorytetuje pojazdy z tauntem (Czołg).
public class WiezaArmatnia : WiezaBaza
{
    [Header("Parametry Armaty")]
    public float zasieg = 18f;
    public float czasPrzeladowania = 4f;
    public LayerMask warstwaWroga;

    [Header("Pocisk Armatni")]
    public GameObject prefabPocisku;
    public Transform punktStrzalu;

    private float _licznikPrzeladowania;

    protected override void Start()
    {
        base.Start();
        UtworzKragZasiegu(zasieg, new Color(1f, 0.4f, 0f, 0.85f));
        _licznikPrzeladowania = 0f;
    }

    protected override void Update()
    {
        base.Update();
        _licznikPrzeladowania -= Time.deltaTime;
        if (_licznikPrzeladowania <= 0f)
            SzukajCeluIStrzelaj();
    }

    void SzukajCeluIStrzelaj()
    {
        pojazd cel = pojazd.ZnajdzCelWZasiegu(transform.position, zasieg, warstwaWroga);
        if (cel == null) return;

        Strzelaj(cel.transform);
        _licznikPrzeladowania = czasPrzeladowania / attackSpeedMultiplier;
    }

    void Strzelaj(Transform cel)
    {
        if (prefabPocisku == null || punktStrzalu == null) return;

        GameObject pociskGO = Instantiate(prefabPocisku, punktStrzalu.position, punktStrzalu.rotation);
        PociskArmatni skrypt = pociskGO.GetComponent<PociskArmatni>();
        if (skrypt != null)
        {
            skrypt.warstwaWroga = warstwaWroga;
            skrypt.UstawCel(cel);
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, zasieg);
    }
}
