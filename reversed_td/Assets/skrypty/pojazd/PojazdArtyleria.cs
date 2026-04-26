using UnityEngine;

public class PojazdArtyleria : pojazd
{
    [Header("Parametry Ataku")]
    public float zasiegAtaku    = 22f;
    public float cooldownStrzalu = 2.5f;
    public float obrazeniaStrzalu = DecreeManager.BASE_ALT_DMG;

    [Header("Prefab Pocisku Artyleryjskiego")]
    public GameObject prefabPocisku;
    public Transform punktStrzalu;

    private float _licznikStrzalu = 0f;

    protected override void Start()
    {
        maxHp   = DecreeManager.Instance != null
            ? DecreeManager.Instance.FinalHP("Artyleria", DecreeManager.BASE_ALT_HP)
            : DecreeManager.BASE_ALT_HP;
        pancerz = DecreeManager.Instance != null
            ? DecreeManager.Instance.FinalArmor("Artyleria", DecreeManager.BASE_ALT_ARM)
            : DecreeManager.BASE_ALT_ARM;
        base.Start();
        _agent.speed = DecreeManager.Instance != null
            ? DecreeManager.Instance.FinalSpeed("Artyleria", DecreeManager.BASE_ALT_SPD)
            : DecreeManager.BASE_ALT_SPD;

        obrazeniaStrzalu = DecreeManager.Instance != null
            ? DecreeManager.BASE_ALT_DMG * (1f + DecreeManager.Instance.ArtyleriaObrazeniaBonus)
            : DecreeManager.BASE_ALT_DMG;

        _licznikStrzalu = 0f;
    }

    protected override void Update()
    {
        base.Update();

        _licznikStrzalu -= Time.deltaTime;
        if (_licznikStrzalu <= 0f)
            SzukajWiezyIStrzelaj();
    }

    void SzukajWiezyIStrzelaj()
    {
        Collider[] wszystkie = Physics.OverlapSphere(transform.position, zasiegAtaku);
        WiezaBaza cel = null;

        foreach (var c in wszystkie)
        {
            WiezaBaza w = c.GetComponent<WiezaBaza>();
            if (w != null && w.gameObject.activeInHierarchy)
            {
                cel = w;
                break;
            }
        }

        if (cel == null) return;

        Strzelaj(cel.transform);
        _licznikStrzalu = cooldownStrzalu;
    }

    void Strzelaj(Transform cel)
    {
        if (prefabPocisku == null) return;

        Vector3 skadStrzelac = punktStrzalu != null ? punktStrzalu.position : transform.position + Vector3.up;
        GameObject pociskGO = Instantiate(prefabPocisku, skadStrzelac, Quaternion.identity);

        PociskArtyleryjski skrypt = pociskGO.GetComponent<PociskArtyleryjski>();
        if (skrypt != null)
        {
            skrypt.obrazenia = obrazeniaStrzalu;
            skrypt.UstawCel(cel);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.8f, 0.6f, 0f);
        Gizmos.DrawWireSphere(transform.position, zasiegAtaku);
    }
}
