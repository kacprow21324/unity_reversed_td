using Mirror;
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
    public float boostCost      = 50f;
    public float boostRadius    = 12f;
    public float boostDuration  = 10f;
    public float boostFlatBonus = 5.0f;

    [Header("Detekcja Terytorium (Multiplayer)")]
    [Tooltip("Transform centrum NASZEJ planszy (wież). Jeśli null — używa NetworkMatchManager.spawnerMap1/2.")]
    public Transform myMapRoot;
    [Tooltip("Transform centrum planszy WROGA. Jeśli null — używa NetworkMatchManager.spawnerMap1/2.")]
    public Transform enemyMapRoot;

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

    // ── Wykrywanie mapy ───────────────────────────────────────────────────

    // Zwraca true jeśli pozycja leży na własnej planszy (z wieżami gracza).
    // Detekcja: myMapRoot/enemyMapRoot (z Inspektora) lub pozycje TowerSpawnerów z NMM.
    // W SP zawsze zwraca true — oryginalne zachowanie.
    bool IsOnMyMap(Vector3 worldPos)
    {
        var nmm = NetworkMatchManager.Instance;
        if (nmm == null) return true;

        var localPlayer = NetworkClient.localPlayer?.GetComponent<NetworkPlayer>();
        if (localPlayer == null) return true;

        // Preferuj explicite podane rooty map; fallback na spawnerów z NetworkMatchManager
        Transform root1 = myMapRoot    != null ? myMapRoot    : nmm.spawnerMap1?.transform;
        Transform root2 = enemyMapRoot != null ? enemyMapRoot : nmm.spawnerMap2?.transform;

        if (root1 == null || root2 == null) return true;

        float d1 = Vector3.Distance(worldPos, root1.position);
        float d2 = Vector3.Distance(worldPos, root2.position);
        bool onMap1 = d1 <= d2;

        // Gracz 1 → root1 (Map1) jest jego planszą; Gracz 2 → root2 (Map2) jest jego planszą
        return localPlayer.playerIndex == 1 ? onMap1 : !onMap1;
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

        bool onMyMap = IsOnMyMap(center);
        bool isMP    = NetworkMatchManager.Instance != null;

        if (!isMP)
        {
            // ── SP: lokalny nalot na wieże (oryginalne zachowanie) ────────────
            foreach (var col in Physics.OverlapSphere(center, radius))
            {
                WiezaBaza wieza = col.GetComponent<WiezaBaza>()
                               ?? col.GetComponentInParent<WiezaBaza>();
                if (wieza != null && wieza.gameObject.activeInHierarchy)
                    wieza.TakeDamage(damage);
            }
        }
        else if (onMyMap)
        {
            // ── MP + własna plansza: nalot na WIEŻE, zsynchronizowany przez serwer ──
            // Serwer wyśle ClientRpc do obu klientów → identyczne zniszczenie bez NetworkIdentity.
            NetworkClient.localPlayer?.GetComponent<NetworkPlayer>()
                ?.CmdApplySpecialPower("airstrike", center, true, radius, damage, 0f);
        }
        else
        {
            // ── MP + plansza wroga: nalot na POJAZDY, zsynchronizowany przez serwer ──
            // RPC zabija realne pojazdy u rzucającego + duchy u przeciwnika jednocześnie.
            NetworkClient.localPlayer?.GetComponent<NetworkPlayer>()
                ?.CmdApplySpecialPower("airstrike_vehicles", center, false, radius, damage, 0f);
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

        bool onMyMap = IsOnMyMap(center);
        bool isMP    = NetworkMatchManager.Instance != null;

        if (!isMP)
        {
            // ── SP: lokalny efekt na pojazdy (oryginalne zachowanie) ──────────
            foreach (var col in Physics.OverlapSphere(center, radius))
            {
                pojazd p = col.GetComponent<pojazd>();
                if (p != null && !p.isGhost) p.AktywujTarcze(duration);
            }
        }
        else if (onMyMap)
        {
            // ── MP + własna plansza: tarcza dla pojazdów w strefie ───────────
            // Na własnej mapie są duchy wroga (isGhost=true) — chronimy je wizualnie.
            foreach (var col in Physics.OverlapSphere(center, radius))
            {
                pojazd p = col.GetComponent<pojazd>();
                if (p != null) p.AktywujTarcze(duration);
            }
        }
        else
        {
            // ── MP + plansza wroga: tarcza dla WIEŻ, zsynchronizowana przez serwer ──
            NetworkClient.localPlayer?.GetComponent<NetworkPlayer>()
                ?.CmdApplySpecialPower("shield_towers", center, false, radius, duration, 0f);
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

        float flat     = boostFlatBonus;
        float duration = boostDuration;
        if (DecreeManager.Instance != null)
        {
            flat     += DecreeManager.Instance.BoostFlatBonus;
            duration += DecreeManager.Instance.BoostDurationBonus;
        }

        bool onMyMap = IsOnMyMap(center);
        bool isMP    = NetworkMatchManager.Instance != null;

        if (!isMP)
        {
            // ── SP: lokalny boost pojazdów (oryginalne zachowanie) ───────────
            foreach (var col in Physics.OverlapSphere(center, boostRadius))
            {
                pojazd p = col.GetComponent<pojazd>();
                if (p != null && !p.isGhost) p.DoladujPredkosc(flat, duration);
            }
        }
        else if (onMyMap)
        {
            // ── MP + własna plansza: boost pojazdów — zsynchronizowany przez serwer ──
            // RPC przyspiesza realne pojazdy u rzucającego + duchy u przeciwnika jednocześnie.
            NetworkClient.localPlayer?.GetComponent<NetworkPlayer>()
                ?.CmdApplySpecialPower("boost_vehicles", center, true, boostRadius, flat, duration);
        }
        else
        {
            // ── MP + plansza wroga: boost WIEŻ (szybkość ataku), zsynchronizowany ──
            // flat/boostFlatBonus: domyślnie 5/5 = 2x szybkość ataku (mnożnik 2.0)
            float speedMult = 1f + flat / boostFlatBonus;
            NetworkClient.localPlayer?.GetComponent<NetworkPlayer>()
                ?.CmdApplySpecialPower("boost_towers", center, false, boostRadius, speedMult, duration);
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
