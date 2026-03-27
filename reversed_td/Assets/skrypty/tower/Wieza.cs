using UnityEngine;

public class WiezaAtak : MonoBehaviour
{
    [Header("Ustawienia Wie¿y")]
    public float zasieg = 10f;
    public float szybkoscAtaku = 1f;  
    public LayerMask warstwaWroga;

    [Header("Ustawienia Pocisku")]
    public GameObject prefabPocisku;
    public Transform punktStrzalu;

    private float licznikAtaku = 0f;

    void Update()
    {
        licznikAtaku -= Time.deltaTime;

        if (licznikAtaku <= 0f)
        {
            SzukajIAtakuj();
        }
    }

    void SzukajIAtakuj()
    {
        Collider[] wrogowieWZasiegu = Physics.OverlapSphere(transform.position, zasieg, warstwaWroga);

        if (wrogowieWZasiegu.Length > 0)
        {
            Transform cel = wrogowieWZasiegu[0].transform;
            if (prefabPocisku != null && punktStrzalu != null)
            {
                Strzelaj(cel);
            }

            licznikAtaku = 1f / szybkoscAtaku;
        }
    }

    void Strzelaj(Transform cel)
    {
        Debug.Log("Wie¿a strzela do: " + cel.name);

        GameObject nowyPociskGO = Instantiate(prefabPocisku, punktStrzalu.position, punktStrzalu.rotation);

        Pocisk skryptPocisku = nowyPociskGO.GetComponent<Pocisk>();

        if (skryptPocisku != null)
        {
            // Mówimy pociskowi, jaki jest jego cel
            skryptPocisku.UstawCel(cel);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, zasieg);
    }
}