using UnityEngine;

// AMIRROR – odbija pociski WBASIC i laser WPLAZMA z powrotem w wieżę,
// o ile pojazd nie jest wykryty przez żaden aktywny sonar (WiezaSonar).
public class PojazdLustro : pojazd
{
    protected override void Start()
    {
        base.Start();
        if (DecreeManager.Instance != null)
            _agent.speed = DecreeManager.Instance.FinalSpeed("Lustro", _agent.speed);
    }

    // ── Odbicie lasera (WPLAZMA wywołuje to PRZED OdejmijHp) ───────────────
    public override bool ReflektujLaser(float dmg, WiezaBaza zrodlo)
    {
        if (isInvulnerable) return false;
        if (WiezaSonar.IsInRadarRange(transform.position)) return false;

        zrodlo?.TakeDamage(dmg);
        return true; // laser odbity – wieża pomija własny strzał
    }

    // ── Odbicie pocisków (WBASIC uderza przez trigger → OdejmijHp) ─────────
    public override void OdejmijHp(float obrazenia, DamageType typ, Component zrodlo, bool przebijaPancerz = false)
    {
        if (isInvulnerable) return;

        if (WiezaSonar.IsInRadarRange(transform.position))
        {
            base.OdejmijHp(obrazenia, przebijaPancerz);
            return;
        }

        if (typ == DamageType.Basic)
        {
            OdbijPocisk(zrodlo as Pocisk);
            return;
        }

        // Pozostałe typy (AoE armaty, kolce) – normalne obrażenia.
        base.OdejmijHp(obrazenia, przebijaPancerz);
    }

    void OdbijPocisk(Pocisk pocisk)
    {
        if (pocisk == null) return;

        if (DecreeManager.Instance != null && DecreeManager.Instance.LustroOdbicieBonus > 0f)
            pocisk.obrazenia *= (1f + DecreeManager.Instance.LustroOdbicieBonus);

        pocisk.OdbijWStroneWiezy();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.5f, 0.8f, 1f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, 1f);
    }
}
