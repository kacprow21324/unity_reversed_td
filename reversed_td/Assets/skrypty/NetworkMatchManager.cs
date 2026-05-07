using System.Collections;
using Mirror;
using UnityEngine;

/// Zarządza stanem meczu multiplayer: seed map, timer przygotowawczy, HP graczy, wyniki rund.
public class NetworkMatchManager : NetworkBehaviour
{
    public static NetworkMatchManager Instance { get; private set; }

    // ── SyncVars ──────────────────────────────────────────────────────────

    [SyncVar(hook = nameof(OnSeedReceived))]
    public int matchSeed;

    [SyncVar]
    private bool _actionPhaseStarted;

    [SyncVar(hook = nameof(OnP1HPChanged))]
    public int player1HP = 3;

    [SyncVar(hook = nameof(OnP2HPChanged))]
    public int player2HP = 3;

    [SyncVar(hook = nameof(OnTimerChanged))]
    public float preparationTime = 0f;

    // ── Flagi serwera ─────────────────────────────────────────────────────

    private bool _isMapGenerated     = false;
    private int  _p1EscapedThisRound = -1;
    private int  _p2EscapedThisRound = -1;
    private int  _pendingRoundResults = 0;
    private int  _currentRound        = 1;
    private Coroutine _timerCoroutine;

    private int[]     _p1QueueIndices;
    private int[]     _p2QueueIndices;
    private bool      _earlyStartCounting = false;
    private bool      _ghostsSpawned      = false;
    private Coroutine _earlyStartCoroutine;

    [Header("Referencje Map (Multiplayer)")]
    public TowerSpawner spawnerMap1;
    public TowerSpawner spawnerMap2;

    [Header("Pojazdy - Ghost (Multiplayer)")]
    public VehicleSpawner vehicleSpawnerMap1;
    public VehicleSpawner vehicleSpawnerMap2;
    public FinishLine     finishLine1;
    public FinishLine     finishLine2;

    // ── Unity / Mirror lifecycle ──────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public override void OnStartServer()
    {
        matchSeed           = Random.Range(1000, 999999);
        _actionPhaseStarted = false;
        _isMapGenerated     = false;
        player1HP           = 3;
        player2HP           = 3;
        _currentRound       = 1;
        _p1QueueIndices     = null;
        _p2QueueIndices     = null;
        _earlyStartCounting = false;
        _ghostsSpawned      = false;
        Debug.Log($"[NetworkMatchManager] Seed: {matchSeed}");
    }

    public override void OnStartClient()
    {
        MatchHPUI.EnsureExists();
    }

    // ── Hooks SyncVar ─────────────────────────────────────────────────────

    void OnSeedReceived(int _, int newSeed)
    {
        if (newSeed == 0) return;
        Debug.Log($"[NetworkMatchManager] Seed odebrany: {newSeed}");
    }

    void OnP1HPChanged(int _, int __)     => MatchHPUI.Instance?.RefreshHP();
    void OnP2HPChanged(int _, int __)     => MatchHPUI.Instance?.RefreshHP();
    void OnTimerChanged(float _, float t) => MatchHPUI.Instance?.UpdateTimer(t);

    // ── Update: oczekiwanie na graczy ─────────────────────────────────────

    void Update()
    {
        if (!isServer) return;

        // Generuj mapy gdy obaj gracze są w scenie
        if (!_isMapGenerated && NetworkServer.connections.Count == 2)
        {
            NetworkPlayer[] players = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None);
            if (players.Length == 2)
            {
                _isMapGenerated = true;
                Debug.Log($"[NetworkMatchManager] Generuję mapy seedem {matchSeed}.");
                RpcGenerateMapsOnClients(matchSeed);
                StartPreparationTimer(60f);
            }
        }

        // Wczesny start: obaj graczy kliknęli START i wysłali kolejki
        if (!_actionPhaseStarted && _isMapGenerated && !_earlyStartCounting)
        {
            NetworkPlayer[] players = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None);
            if (players.Length == 2)
            {
                bool allReady = true;
                foreach (var p in players)
                    if (!p.isReady) { allReady = false; break; }

                bool bothQueues = _p1QueueIndices != null && _p2QueueIndices != null;

                if (allReady && bothQueues)
                {
                    foreach (var p in players) p.isReady = false;
                    _earlyStartCounting = true;
                    if (_timerCoroutine != null) { StopCoroutine(_timerCoroutine); _timerCoroutine = null; }
                    _earlyStartCoroutine = StartCoroutine(EarlyStartCountdown());
                }
            }
        }
    }

    // ── Timer przygotowawczy ──────────────────────────────────────────────

    [Server]
    void StartPreparationTimer(float duration = 60f)
    {
        if (_timerCoroutine != null) StopCoroutine(_timerCoroutine);
        _timerCoroutine = StartCoroutine(PreparationTimerCoroutine(duration));
    }

    IEnumerator PreparationTimerCoroutine(float duration)
    {
        preparationTime = duration;
        while (preparationTime > 0f)
        {
            yield return new WaitForSecondsRealtime(1f);
            preparationTime = Mathf.Max(0f, preparationTime - 1f);
        }
        if (!_actionPhaseStarted)
            ForceStartActionPhase();
    }

    // ── Mapy ─────────────────────────────────────────────────────────────

    [ClientRpc]
    void RpcGenerateMapsOnClients(int seed)
    {
        Debug.Log($"[NetworkMatchManager] RPC: Generuję mapy seedem {seed}.");
        GenerateBothMaps(seed, 1);
    }

    void GenerateBothMaps(int seed, int round = 1)
    {
        spawnerMap1?.GenerateMapWithSeed(seed, round);
        spawnerMap2?.GenerateMapWithSeed(seed, round);
    }

    // ── Faza ataku ────────────────────────────────────────────────────────

    [ClientRpc]
    public void RpcStartActionPhase()
    {
        Debug.Log("[NetworkMatchManager] RPC: Faza ataku.");
        GameplayUIManager.Instance?.StartNetworkRound();
    }

    [Server]
    public void ForceStartActionPhase()
    {
        if (_actionPhaseStarted) return;
        _actionPhaseStarted = true;
        if (_timerCoroutine != null) { StopCoroutine(_timerCoroutine); _timerCoroutine = null; }
        preparationTime = 0f;
        RpcStartActionPhase();
    }

    public void RequestStartFromLocalPlayer()
    {
        NetworkClient.localPlayer?.GetComponent<NetworkPlayer>()?.CmdTryStartGame();
    }

    // ── Wyniki rundy (zbierane z obu klientów) ────────────────────────────

    [Server]
    public void OnRoundResultReceived(int playerIdx, int escaped)
    {
        if      (playerIdx == 1) _p1EscapedThisRound = escaped;
        else if (playerIdx == 2) _p2EscapedThisRound = escaped;

        _pendingRoundResults++;
        if (_pendingRoundResults < 2) return;

        bool p1Failed = _p1EscapedThisRound == 0;
        bool p2Failed = _p2EscapedThisRound == 0;

        if (p1Failed) player1HP = Mathf.Max(0, player1HP - 1);
        if (p2Failed) player2HP = Mathf.Max(0, player2HP - 1);

        _pendingRoundResults  = 0;
        _p1EscapedThisRound   = -1;
        _p2EscapedThisRound   = -1;

        bool p1Dead = player1HP <= 0;
        bool p2Dead = player2HP <= 0;

        if (p1Dead || p2Dead)
        {
            // 0 = remis, 1 = wygrywa gracz 1, 2 = wygrywa gracz 2
            int winner = (p1Dead && p2Dead) ? 0 : (p1Dead ? 2 : 1);
            RpcTriggerEndGame(winner);
            return;
        }

        _currentRound++;
        int nextSeed = matchSeed + _currentRound;
        ResetForNextRound();
        RpcPrepareNextRound(nextSeed, _currentRound);
        StartPreparationTimer(60f);
    }

    [ClientRpc]
    void RpcTriggerEndGame(int winnerPlayerIndex)
    {
        var localPlayer = NetworkClient.localPlayer?.GetComponent<NetworkPlayer>();
        if (localPlayer == null) return;
        int myIndex = localPlayer.playerIndex;

        if (winnerPlayerIndex == 0)
            GameManager.Instance?.TriggerDraw();
        else if (winnerPlayerIndex == myIndex)
            GameManager.Instance?.TriggerVictory();
        else
            GameManager.Instance?.TriggerDefeat();
    }

    [ClientRpc]
    void RpcPrepareNextRound(int seed, int round)
    {
        GenerateBothMaps(seed, round);
        GameplayUIManager.Instance?.PrepareNextNetworkRound(round);
    }

    [Server]
    public void ResetForNextRound()
    {
        _actionPhaseStarted = false;
        _p1QueueIndices     = null;
        _p2QueueIndices     = null;
        _earlyStartCounting = false;
        _ghostsSpawned      = false;
        if (_earlyStartCoroutine != null) { StopCoroutine(_earlyStartCoroutine); _earlyStartCoroutine = null; }
        Debug.Log("[NetworkMatchManager] Reset dla następnej rundy.");
    }

    // ── Kolejki pojazdów + Ghost spawn ────────────────────────────────────

    [Server]
    public void StorePlayerQueue(int playerIdx, int[] vehicleIndices)
    {
        if (playerIdx == 1) _p1QueueIndices = vehicleIndices;
        else                _p2QueueIndices = vehicleIndices;

        // Ścieżka ekspiracji timera: faza ataku już trwa — spawnuj duchy gdy obaj zgłosili
        if (_actionPhaseStarted && !_ghostsSpawned && _p1QueueIndices != null && _p2QueueIndices != null)
        {
            _ghostsSpawned = true;
            RpcSpawnEnemyVehicles(1, _p1QueueIndices);
            RpcSpawnEnemyVehicles(2, _p2QueueIndices);
        }
    }

    IEnumerator EarlyStartCountdown()
    {
        for (int i = 3; i >= 1; i--)
        {
            preparationTime = i;
            yield return new WaitForSecondsRealtime(1f);
        }
        _earlyStartCounting  = false;
        _earlyStartCoroutine = null;

        _ghostsSpawned = true;
        RpcSpawnEnemyVehicles(1, _p1QueueIndices);
        RpcSpawnEnemyVehicles(2, _p2QueueIndices);

        ForceStartActionPhase();
    }

    [ClientRpc]
    void RpcSpawnEnemyVehicles(int ownerPlayerIdx, int[] vehicleIndices)
    {
        var localPlayer = NetworkClient.localPlayer?.GetComponent<NetworkPlayer>();
        if (localPlayer == null) return;
        if (ownerPlayerIdx == localPlayer.playerIndex) return; // własne pojazdy – już zspawnowane lokalnie

        // Duchy wroga pojawiają się na WŁASNEJ mapie lokalnego gracza (tam gdzie jego wieże).
        // Gracz 1 → vehicleSpawnerMap1 / finishLine1, Gracz 2 → vehicleSpawnerMap2 / finishLine2.
        VehicleSpawner ghostSpawner = localPlayer.playerIndex == 1 ? vehicleSpawnerMap1 : vehicleSpawnerMap2;
        FinishLine     ghostFinish  = localPlayer.playerIndex == 1 ? finishLine1        : finishLine2;

        if (ghostSpawner != null && ghostFinish != null)
            ghostSpawner.StartSpawningGhosts(vehicleIndices, ghostFinish);
    }
}
