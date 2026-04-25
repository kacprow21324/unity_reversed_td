using UnityEngine;
using System.Collections;

// WSONAR – nie zadaje obrażeń. Nakłada debuff na pojazdy w zasięgu, przez co
// otrzymują więcej obrażeń od innych wież.
public class WiezaSonar : WiezaBaza
{
    [Header("Parametry Sonara")]
    public float zasieg = 15f;
    public float mnoznikDebuffa = 1.5f;
    public float czestotliwoscSkanowania = 0.5f;
    public LayerMask warstwaWroga;

    protected override void Start()
    {
        maxHP = 80f;
        nagrodaZlota = 40;
        base.Start();
        StartCoroutine(Skanuj());
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        StopAllCoroutines();
        // Start nie odpali ponownie, więc uruchamiamy koroutynę tutaj przy re-aktywacji
        if (gameObject.activeInHierarchy)
            StartCoroutine(Skanuj());
    }

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

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, zasieg);
    }
}
