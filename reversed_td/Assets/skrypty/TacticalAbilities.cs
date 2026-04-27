using UnityEngine;

public class TacticalAbilities : MonoBehaviour
{
    public static TacticalAbilities Instance;

    [Header("Nalot")]
    public float airstrikeCost   = 100f;
    public float airstrikeRadius = 8f;
    public float airstrikeDamage = 100f;

    [Header("Tarcza")]
    public float shieldCost     = 75f;
    public float shieldRadius   = 10f;
    public float shieldDuration = 4f;

    [Header("Boost")]
    public float boostCost       = 50f;
    public float boostRadius     = 12f;
    public float boostDuration   = 5f;
    public float boostMultiplier = 1.5f;

    [Header("Celowanie")]
    public LayerMask groundLayer;
    [SerializeField] private int   circleSegments = 48;
    [SerializeField] private Color circleColor    = Color.cyan;
    [SerializeField] private float circleWidth    = 0.12f;

    private LineRenderer _targetCircle;
    private int   _activeAbility = -1;
    private float _currentRadius;

    void Awake() => Instance = this;

    void Start()
    {
        _targetCircle = BuildLineRenderer();
        _targetCircle.gameObject.SetActive(false);
    }

    void Update()
    {
        if (_activeAbility < 0) return;
        if (GameplayUIManager.Instance == null || GameplayUIManager.Instance.IsPlanning) return;

        UpdateTargetCircle();

        if (Input.GetMouseButtonDown(0))      ExecuteAbility();
        else if (Input.GetMouseButtonDown(1)) CancelAbility();
    }

    // ── Aktywacja ─────────────────────────────────────────────────────────

    public void ActivateAirstrike()
    {
        float r = airstrikeRadius;
        if (DecreeManager.Instance != null)
            r *= (1f + DecreeManager.Instance.NalotRadiusBonus);
        StartActivation(0, r);
    }

    public void ActivateShield()
    {
        float r = shieldRadius;
        if (DecreeManager.Instance != null)
            r *= (1f + DecreeManager.Instance.ShieldRadiusBonus);
        StartActivation(1, r);
    }

    public void ActivateBoost() => StartActivation(2, boostRadius);

    void StartActivation(int index, float radius)
    {
        if (GameplayUIManager.Instance == null || GameplayUIManager.Instance.IsPlanning) return;
        _activeAbility = index;
        _currentRadius = radius;
    }

    void CancelAbility()
    {
        _activeAbility = -1;
        _targetCircle.gameObject.SetActive(false);
    }

    // ── Celowanie ─────────────────────────────────────────────────────────

    void UpdateTargetCircle()
    {
        if (!TryGetGroundPoint(out Vector3 point))
        {
            _targetCircle.gameObject.SetActive(false);
            return;
        }
        DrawCircle(point, _currentRadius);
        _targetCircle.gameObject.SetActive(true);
    }

    bool TryGetGroundPoint(out Vector3 point)
    {
        point = Vector3.zero;
        if (Camera.main == null) return false;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 500f, groundLayer))
        {
            point = hit.point;
            return true;
        }
        return false;
    }

    void ExecuteAbility()
    {
        if (!TryGetGroundPoint(out Vector3 center)) return;

        switch (_activeAbility)
        {
            case 0: UseAirstrike(center); break;
            case 1: UseShield(center);    break;
            case 2: UseBoost(center);     break;
        }

        CancelAbility();
    }

    // ── Moce z dekretami ──────────────────────────────────────────────────

    void UseAirstrike(Vector3 center)
    {
        if (!GameplayUIManager.Instance.TrySpendGold((int)airstrikeCost))
        {
            GameplayUIManager.Instance.FlashNoMoney();
            return;
        }

        float radius = airstrikeRadius;
        float damage = airstrikeDamage;
        if (DecreeManager.Instance != null)
        {
            radius *= (1f + DecreeManager.Instance.NalotRadiusBonus);
            damage += DecreeManager.Instance.NalotDamageBonus;
        }

        foreach (var col in Physics.OverlapSphere(center, radius))
        {
            WiezaBaza wieza = col.GetComponent<WiezaBaza>()
                           ?? col.GetComponentInParent<WiezaBaza>();
            if (wieza != null && wieza.gameObject.activeInHierarchy)
                wieza.TakeDamage(damage);
        }

        GameStatistics.Instance?.RegisterAbility("airstrike");
    }

    void UseShield(Vector3 center)
    {
        if (!GameplayUIManager.Instance.TrySpendGold((int)shieldCost))
        {
            GameplayUIManager.Instance.FlashNoMoney();
            return;
        }

        float radius   = shieldRadius;
        float duration = shieldDuration;
        if (DecreeManager.Instance != null)
        {
            radius   *= (1f + DecreeManager.Instance.ShieldRadiusBonus);
            duration += DecreeManager.Instance.ShieldDurationBonus;
        }

        foreach (var col in Physics.OverlapSphere(center, radius))
        {
            pojazd p = col.GetComponent<pojazd>();
            p?.AktywujTarcze(duration);
        }

        GameStatistics.Instance?.RegisterAbility("shield");
    }

    void UseBoost(Vector3 center)
    {
        if (!GameplayUIManager.Instance.TrySpendGold((int)boostCost))
        {
            GameplayUIManager.Instance.FlashNoMoney();
            return;
        }

        float multiplier = boostMultiplier;
        float duration   = boostDuration;
        if (DecreeManager.Instance != null)
        {
            multiplier *= (1f + DecreeManager.Instance.BoostMultBonus);
            duration   += DecreeManager.Instance.BoostDurationBonus;
        }

        foreach (var col in Physics.OverlapSphere(center, boostRadius))
        {
            pojazd p = col.GetComponent<pojazd>();
            p?.DoladujPredkosc(multiplier, duration);
        }

        GameStatistics.Instance?.RegisterAbility("boost");
    }

    // ── Okrag ─────────────────────────────────────────────────────────────

    void DrawCircle(Vector3 center, float radius)
    {
        float step = 2f * Mathf.PI / circleSegments;
        for (int i = 0; i < circleSegments; i++)
        {
            float a = i * step;
            _targetCircle.SetPosition(i,
                new Vector3(center.x + radius * Mathf.Cos(a),
                            center.y + 0.08f,
                            center.z + radius * Mathf.Sin(a)));
        }
    }

    LineRenderer BuildLineRenderer()
    {
        var go = new GameObject("AbilityTargetCircle");
        var lr = go.AddComponent<LineRenderer>();
        lr.loop          = true;
        lr.useWorldSpace = true;
        lr.startWidth    = circleWidth;
        lr.endWidth      = circleWidth;
        lr.startColor    = circleColor;
        lr.endColor      = circleColor;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.positionCount = circleSegments;
        return lr;
    }
}
