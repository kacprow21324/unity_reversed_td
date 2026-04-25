using UnityEngine;

// WCANON – wolne przeładowanie, potężne pociski AoE przebijające pancerz.
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
        maxHP = 120f;
        nagrodaZlota = 80;
        base.Start();
        // Startujemy z gotową armatą
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
        Collider[] wrogowie = Physics.OverlapSphere(transform.position, zasieg, warstwaWroga);
        if (wrogowie.Length == 0) return;

        Strzelaj(wrogowie[0].transform);
        _licznikPrzeladowania = czasPrzeladowania;
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

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0f);
        Gizmos.DrawWireSphere(transform.position, zasieg);
    }
}
