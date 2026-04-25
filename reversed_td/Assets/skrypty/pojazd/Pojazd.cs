using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class pojazd : MonoBehaviour
{
    [Header("Ustawienia Zdrowia")]
    public float maxHp = 100f;

    [Header("Pancerz")]
    [Tooltip("Redukuje każde trafienie o tę wartość (chyba że pocisk przebija pancerz).")]
    public float pancerz = 8f;

    // Wóz Tank ustawia tę flagę – wieże go preferują jako cel.
    [HideInInspector] public bool maTaunt       = false;
    [HideInInspector] public bool isInvulnerable = false;

    protected float aktualneHp;
    protected float _debuffMnoznik = 1f;
    protected float _debuffTimer = 0f;
    protected NavMeshAgent _agent;

    protected virtual void Start()
    {
        aktualneHp = maxHp;
        _agent = GetComponent<NavMeshAgent>();

        FinishLine meta = FindFirstObjectByType<FinishLine>();
        if (meta != null)
            _agent.SetDestination(meta.transform.position);
        else
            Debug.LogError("Pojazd: Nie znaleziono obiektu ze skryptem FinishLine na mapie!");
    }

    protected virtual void Update()
    {
        if (_debuffTimer > 0f)
        {
            _debuffTimer -= Time.deltaTime;
            if (_debuffTimer <= 0f)
                _debuffMnoznik = 1f;
        }
    }

    public void AplikujDebuff(float mnoznik, float czas)
    {
        _debuffMnoznik = mnoznik;
        _debuffTimer = czas;
    }

    public virtual void OdejmijHp(float obrazenia, bool przebijaPancerz = false)
    {
        if (isInvulnerable) return;

        float skuteczne = przebijaPancerz
            ? obrazenia
            : Mathf.Max(1f, obrazenia - pancerz);

        skuteczne *= _debuffMnoznik;
        aktualneHp -= skuteczne;

        if (aktualneHp <= 0f)
            Smierc();
    }

    protected virtual void Smierc()
    {
        GameplayUIManager.Instance?.OnVehicleRemoved();
        Destroy(gameObject);
    }

    public float PobierzAktualneHP()
    {
        return aktualneHp;
    }

    public void AktywujTarcze(float czas)
    {
        StartCoroutine(TarczaKoroutyna(czas));
    }

    IEnumerator TarczaKoroutyna(float czas)
    {
        isInvulnerable = true;
        yield return new WaitForSeconds(czas);
        isInvulnerable = false;
    }

    public void DoladujPredkosc(float mnoznik, float czas)
    {
        if (_agent == null) return;
        StartCoroutine(BoostKoroutyna(mnoznik, czas));
    }

    IEnumerator BoostKoroutyna(float mnoznik, float czas)
    {
        float original = _agent.speed;
        _agent.speed *= mnoznik;
        yield return new WaitForSeconds(czas);
        _agent.speed = original;
    }

    // Pomocnik dla wież: zwraca pojazd priorytetyzując taunterów.
    public static pojazd ZnajdzCelWZasiegu(Vector3 pozycja, float zasieg, LayerMask warstwa)
    {
        Collider[] wrogowie = Physics.OverlapSphere(pozycja, zasieg, warstwa);
        pojazd priorytet = null;
        pojazd pierwszy = null;

        foreach (var w in wrogowie)
        {
            pojazd p = w.GetComponent<pojazd>();
            if (p == null) continue;
            if (pierwszy == null) pierwszy = p;
            if (p.maTaunt && priorytet == null) priorytet = p;
        }

        return priorytet ?? pierwszy;
    }
}
