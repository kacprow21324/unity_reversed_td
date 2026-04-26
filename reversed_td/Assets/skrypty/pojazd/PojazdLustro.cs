using UnityEngine;
using System.Collections.Generic;

public class PojazdLustro : pojazd
{
    [Header("Parametry Tarczy")]
    public float zasiegDetekcji   = 6f;
    public float cooldownSkanowania = 0.1f;

    private float _licznikSkanowania = 0f;
    private readonly HashSet<Pocisk> _juzOdbite = new HashSet<Pocisk>();

    protected override void Start()
    {
        maxHp   = DecreeManager.Instance != null
            ? DecreeManager.Instance.FinalHP("Lustro", DecreeManager.BASE_LUS_HP)
            : DecreeManager.BASE_LUS_HP;
        pancerz = DecreeManager.Instance != null
            ? DecreeManager.Instance.FinalArmor("Lustro", DecreeManager.BASE_LUS_ARM)
            : DecreeManager.BASE_LUS_ARM;
        base.Start();
        _agent.speed = DecreeManager.Instance != null
            ? DecreeManager.Instance.FinalSpeed("Lustro", DecreeManager.BASE_LUS_SPD)
            : DecreeManager.BASE_LUS_SPD;
    }

    protected override void Update()
    {
        base.Update();

        _licznikSkanowania -= Time.deltaTime;
        if (_licznikSkanowania <= 0f)
        {
            SkanujIPrzebijTarcza();
            _licznikSkanowania = cooldownSkanowania;
        }
    }

    void SkanujIPrzebijTarcza()
    {
        Collider[] pobliskieCollidery = Physics.OverlapSphere(transform.position, zasiegDetekcji);

        foreach (var col in pobliskieCollidery)
        {
            Pocisk pocisk = col.GetComponent<Pocisk>();
            if (pocisk == null || pocisk.odbity || _juzOdbite.Contains(pocisk)) continue;

            Vector3 kierunekDoLustra = transform.position - col.transform.position;
            RaycastHit hit;
            if (!Physics.Raycast(col.transform.position, kierunekDoLustra.normalized,
                                  out hit, zasiegDetekcji + 1f))
                continue;

            if (hit.collider.gameObject != gameObject) continue;

            Odbij(pocisk);
        }
    }

    void Odbij(Pocisk pocisk)
    {
        WiezaBaza najblisza = ZnajdzNajblizszeWieze();
        if (najblisza == null)
        {
            Destroy(pocisk.gameObject);
            return;
        }

        pocisk.odbity = true;
        pocisk.UstawCel(najblisza.transform);

        if (DecreeManager.Instance != null && DecreeManager.Instance.LustroOdbicieBonus > 0f)
            pocisk.obrazenia *= (1f + DecreeManager.Instance.LustroOdbicieBonus);

        _juzOdbite.Add(pocisk);
    }

    WiezaBaza ZnajdzNajblizszeWieze()
    {
        WiezaBaza[] wiezy = FindObjectsByType<WiezaBaza>(FindObjectsSortMode.None);
        WiezaBaza najblisza = null;
        float minDyst = float.MaxValue;

        foreach (var w in wiezy)
        {
            if (!w.gameObject.activeInHierarchy) continue;
            float d = Vector3.Distance(transform.position, w.transform.position);
            if (d < minDyst) { minDyst = d; najblisza = w; }
        }

        return najblisza;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.5f, 0.8f, 1f);
        Gizmos.DrawWireSphere(transform.position, zasiegDetekcji);
    }
}
