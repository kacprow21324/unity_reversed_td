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

    private float aktualneHp;
    private float _debuffMnoznik = 1f;
    private float _debuffTimer = 0f;

    private NavMeshAgent _agent;

    void Start()
    {
        aktualneHp = maxHp;
        _agent = GetComponent<NavMeshAgent>();

        FinishLine meta = FindObjectOfType<FinishLine>();
        if (meta != null)
            _agent.SetDestination(meta.transform.position);
        else
            Debug.LogError("Pojazd: Nie znaleziono obiektu ze skryptem FinishLine na mapie!");
    }

    void Update()
    {
        if (_debuffTimer > 0f)
        {
            _debuffTimer -= Time.deltaTime;
            if (_debuffTimer <= 0f)
                _debuffMnoznik = 1f;
        }
    }

    // Nakłada debuff (np. od Wieży Sonar). Odświeżenie przedłuża czas trwania.
    public void AplikujDebuff(float mnoznik, float czas)
    {
        _debuffMnoznik = mnoznik;
        _debuffTimer = czas;
    }

    // przebijaPancerz = true: pancerz jest ignorowany (np. Wieża Armatnia).
    public void OdejmijHp(float obrazenia, bool przebijaPancerz = false)
    {
        float skuteczne = przebijaPancerz
            ? obrazenia
            : Mathf.Max(1f, obrazenia - pancerz);

        skuteczne *= _debuffMnoznik;
        aktualneHp -= skuteczne;

        if (aktualneHp <= 0f)
            Smierc();
    }

    void Smierc()
    {
        Destroy(gameObject);
    }

    public float PobierzAktualneHP()
    {
        return aktualneHp;
    }
}
