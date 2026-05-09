using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// WSONAR – nie zadaje obrażeń. Nakłada debuff na pojazdy w zasięgu, przez co
// otrzymują więcej obrażeń od innych wież. Globalne ActiveRadarsCount informuje
// Kamikaze i inne stealthowe jednostki o tym, czy muszą ujawnić swoją pozycję.
public class WiezaSonar : WiezaBaza
{
    [Header("Parametry Sonara")]
    public float zasieg = 15f;
    public float mnoznikDebuffa = 1.5f;
    public float czestotliwoscSkanowania = 0.5f;
    public LayerMask warstwaWroga;

    // ── Globalny licznik i lista aktywnych sonarów ──────────────────────────
    public static int ActiveRadarsCount = 0;
    private static readonly List<WiezaSonar> _aktywne = new List<WiezaSonar>();

    private bool _zarejestrowany = false;

    /// <summary>Zwraca true, jeśli position leży w zasięgu dowolnego aktywnego sonara.</summary>
    public static bool IsInRadarRange(Vector3 position)
    {
        foreach (var s in _aktywne)
        {
            if (s != null && Vector3.Distance(position, s.transform.position) <= s.zasieg)
                return true;
        }
        return false;
    }

    // ── Cykl życia ──────────────────────────────────────────────────────────
    protected override void Start()
    {
        base.Start();
        UtworzKragZasiegu(zasieg, new Color(0f, 1f, 1f, 0.85f));
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        Rejestruj();
        StopAllCoroutines();
        StartCoroutine(Skanuj());
    }

    private void OnDisable()
    {
        Wyrejestruj();
        StopAllCoroutines();
    }

    private void OnDestroy()
    {
        Wyrejestruj();
    }

    private void Rejestruj()
    {
        if (_zarejestrowany) return;
        ActiveRadarsCount++;
        _aktywne.Add(this);
        _zarejestrowany = true;
    }

    private void Wyrejestruj()
    {
        if (!_zarejestrowany) return;
        ActiveRadarsCount = Mathf.Max(0, ActiveRadarsCount - 1);
        _aktywne.Remove(this);
        _zarejestrowany = false;
    }

    // ── Logika skanowania ───────────────────────────────────────────────────
    IEnumerator Skanuj()
    {
        // Czas życia debuffa = 2x interwał skanowania, żeby nigdy nie wygasał
        // dopóki pojazd jest w zasięgu.
        float czasDebuffa = czestotliwoscSkanowania * 2.2f;

        while (true)
        {
            Collider[] wrogowie = Physics.OverlapSphere(transform.position, zasieg, warstwaWroga);
            foreach (var w in wrogowie)
            {
                pojazd p = w.GetComponent<pojazd>();
                p?.AplikujDebuff(mnoznikDebuffa, czasDebuffa);
            }
            yield return new WaitForSeconds(czestotliwoscSkanowania);
        }
    }

    protected override void OnZniszcz()
    {
        StopAllCoroutines();
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, zasieg);
    }
}
