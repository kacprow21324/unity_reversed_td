using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public enum DamageType { Generic, Basic, Laser }

[RequireComponent(typeof(NavMeshAgent))]
public class pojazd : MonoBehaviour
{
    [Header("Ustawienia Zdrowia")]
    public float maxHp = 100f;

    [Header("Pancerz")]
    [Tooltip("Redukuje każde trafienie o tę wartość (chyba że pocisk przebija pancerz).")]
    public float pancerz = 8f;

    [Header("Ruch (NavMeshAgent)")]
    [Tooltip("Im wyższe, tym szybciej jednostka osiąga max prędkość po zwolnieniu.")]
    public float przyspieszenie = 80f;
    [Tooltip("Minimalna odległość utrzymywana od jednostki bezpośrednio z przodu.")]
    public float minOdstep = 3.5f;

    // Wóz Tank ustawia tę flagę – wieże go preferują jako cel.
    [HideInInspector] public bool maTaunt       = false;
    [HideInInspector] public bool isInvulnerable = false;

    // false = wieże całkowicie ignorują ten pojazd (np. kamuflaż Kamikaze bez radaru)
    public virtual bool IsTargetable => true;

    // Zwraca true, jeśli pojazd odbił laser i wieża powinna pominąć własny strzał.
    // Domyślnie false – tylko PojazdLustro override'uje.
    public virtual bool ReflektujLaser(float dmg, WiezaBaza zrodlo) => false;

    protected float aktualneHp;
    protected float _debuffMnoznik = 1f;
    protected float _debuffTimer = 0f;
    protected NavMeshAgent _agent;
    protected float _predkoscBazowa;

    protected virtual void Start()
    {
        aktualneHp = maxHp;
        _agent = GetComponent<NavMeshAgent>();

        _agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
        _agent.acceleration = przyspieszenie;
        _agent.autoBraking  = false;
        _predkoscBazowa = _agent.speed;

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

        if (_agent != null && _agent.isOnNavMesh)
            RegulujOdstep();
    }

    void RegulujOdstep()
    {
        Vector3 dir = _agent.desiredVelocity.magnitude > 0.01f
            ? _agent.desiredVelocity.normalized
            : transform.forward;

        Collider[] kolizje = Physics.OverlapSphere(
            transform.position + dir * minOdstep * 0.8f,
            minOdstep * 0.45f);

        pojazd przodek = null;
        float najblizszy = float.MaxValue;
        foreach (var k in kolizje)
        {
            if (k.gameObject == gameObject) continue;
            pojazd p = k.GetComponent<pojazd>();
            if (p == null) continue;
            float dist = Vector3.Distance(transform.position, p.transform.position);
            if (dist < najblizszy) { najblizszy = dist; przodek = p; }
        }

        if (przodek != null)
        {
            // Dopasuj prędkość do pojazdu z przodu – nigdy nie jedź szybciej niż on
            float predkoscPrzodka = przodek._agent != null
                ? przodek._agent.velocity.magnitude
                : 0f;
            _agent.speed = Mathf.Min(predkoscPrzodka, _predkoscBazowa);
        }
        else
        {
            _agent.speed = _predkoscBazowa;
        }
    }

    public void AplikujDebuff(float mnoznik, float czas)
    {
        _debuffMnoznik = mnoznik;
        _debuffTimer = czas;
    }

    // Przeciążenie z typem obrażeń – podklasy mogą override'ować dla specjalnych efektów.
    public virtual void OdejmijHp(float obrazenia, DamageType typ, Component zrodlo, bool przebijaPancerz = false)
    {
        OdejmijHp(obrazenia, przebijaPancerz);
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

    public void DoladujPredkosc(float bonus, float czas)
    {
        if (_agent == null) return;
        StartCoroutine(BoostKoroutyna(bonus, czas));
    }

    IEnumerator BoostKoroutyna(float bonus, float czas)
    {
        _predkoscBazowa += bonus;
        yield return new WaitForSeconds(czas);
        if (_agent != null) _predkoscBazowa -= bonus;
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
            if (p == null || !p.IsTargetable) continue;
            if (pierwszy == null) pierwszy = p;
            if (p.maTaunt && priorytet == null) priorytet = p;
        }

        return priorytet ?? pierwszy;
    }
}
