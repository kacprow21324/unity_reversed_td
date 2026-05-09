using UnityEngine;

public class PojazdKamikaze : pojazd
{
    [Header("Parametry Eksplozji")]
    public float promienWyzwalacza = 4f;
    public float promienWybuchu    = 6f;
    public float obrazeniaWybuchu  = 150f;

    // Kamikaze jest niewidzialny dla wież domyślnie; radar (WiezaSonar) go wykrywa.
    public override bool IsTargetable => WiezaSonar.ActiveRadarsCount > 0;

    private bool _eksplodowal = false;

    protected override void Start()
    {
        if (DecreeManager.Instance != null)
        {
            maxHp            = DecreeManager.Instance.FinalHP("Kamikaze", maxHp);
            promienWybuchu  *= (1f + DecreeManager.Instance.KamikazeRadiusBonus);
            obrazeniaWybuchu += DecreeManager.Instance.KamikazeDamageBonus;
        }
        base.Start();
        if (DecreeManager.Instance != null)
            _agent.speed = DecreeManager.Instance.FinalSpeed("Kamikaze", _agent.speed);
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
