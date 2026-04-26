using UnityEngine;

public class PojazdKamikaze : pojazd
{
    [Header("Parametry Eksplozji")]
    public float promienWyzwalacza = 4f;
    public float promienWybuchu    = DecreeManager.BASE_KAM_RAD;
    public float obrazeniaWybuchu  = DecreeManager.BASE_KAM_DMG;

    private bool _eksplodowal = false;

    protected override void Start()
    {
        maxHp   = DecreeManager.Instance != null
            ? DecreeManager.Instance.FinalHP("Kamikaze", DecreeManager.BASE_KAM_HP)
            : DecreeManager.BASE_KAM_HP;
        pancerz = 0f;
        base.Start();
        _agent.speed = DecreeManager.Instance != null
            ? DecreeManager.Instance.FinalSpeed("Kamikaze", DecreeManager.BASE_KAM_SPD)
            : DecreeManager.BASE_KAM_SPD;

        if (DecreeManager.Instance != null)
        {
            promienWybuchu   = DecreeManager.BASE_KAM_RAD * (1f + DecreeManager.Instance.KamikazeRadiusBonus);
            obrazeniaWybuchu = DecreeManager.BASE_KAM_DMG + DecreeManager.Instance.KamikazeDamageBonus;
        }
    }

    protected override void Update()
    {
        base.Update();
        if (!_eksplodowal)
            SprawdzZasiegWiez();
    }

    void SprawdzZasiegWiez()
    {
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

        Collider[] wZasiegu = Physics.OverlapSphere(transform.position, promienWybuchu);
        foreach (var c in wZasiegu)
        {
            WiezaBaza w = c.GetComponent<WiezaBaza>();
            w?.TakeDamage(obrazeniaWybuchu);
        }

        Smierc();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, promienWyzwalacza);
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, promienWybuchu);
    }
}
