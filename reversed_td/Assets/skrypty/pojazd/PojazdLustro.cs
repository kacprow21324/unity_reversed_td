using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// AMIRROR – Wóz Lustrzany. Używa Physics.Raycast do wykrywania nadlatujących
// standardowych pocisków (Pocisk) i odbija je w kierunku wież.
// NIE działa na: kolce (Kolec), obrażenia obszarowe (PociskArmatni), wiązkę plazmową.
public class PojazdLustro : pojazd
{
    [Header("Parametry Tarczy")]
    public float zasiegDetekcji = 6f;
    public float cooldownSkanowania = 0.1f;

    private float _licznikSkanowania = 0f;
    private readonly HashSet<Pocisk> _juzOdbite = new HashSet<Pocisk>();

    protected override void Start()
    {
        maxHp = 90f;
        pancerz = 5f;
        base.Start();
        _agent.speed = 3f;
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
        // Znajdź wszystkie pociski w zasięgu
        Collider[] pobliskieCollidery = Physics.OverlapSphere(transform.position, zasiegDetekcji);

        foreach (var col in pobliskieCollidery)
        {
            Pocisk pocisk = col.GetComponent<Pocisk>();

            // Pomijaj: już odbite, AoE (PociskArmatni to inny komponent), brak Pocisk
            if (pocisk == null || pocisk.odbity || _juzOdbite.Contains(pocisk)) continue;

            // Raycast z pocisku w kierunku lustra – weryfikuje linię widzenia
            Vector3 kierunekDoLustra = transform.position - col.transform.position;
            RaycastHit hit;
            if (!Physics.Raycast(col.transform.position, kierunekDoLustra.normalized,
                                  out hit, zasiegDetekcji + 1f))
                continue;

            // Musi trafić dokładnie w nasze lustro
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
        _juzOdbite.Add(pocisk);
    }

    WiezaBaza ZnajdzNajblizszeWieze()
    {
        WiezaBaza[] wiezy = FindObjectsOfType<WiezaBaza>();
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
