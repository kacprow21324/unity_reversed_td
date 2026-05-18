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

    // Synchronizacja końca fali — zlicza graczy którzy zgłosili zakończenie walki
    private int _waveFinishedCount = 0;
    private int _p1WaveEscaped     = -1;
    private int _p2WaveEscaped     = -1;

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
        _waveFinishedCount  = 0;
        _p1WaveEscaped      = -1;
        _p2WaveEscaped      = -1;
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
        GameplayUIManager.Instance?.ShowPlanningUIForFirstRound();
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

    /// Gracz o podanym indeksie poddaje się — drugi wygrywa natychmiast.
    [Server]
    public void HandleForfeit(int forfeitingPlayerIndex)
    {
        int winner = forfeitingPlayerIndex == 1 ? 2 : 1;
        RpcTriggerEndGame(winner);
    }

    [TargetRpc]
    void RpcShowWaitingOverlay(NetworkConnection target)
    {
        GameplayUIManager.Instance?.ShowWaitingForOpponent();
    }

    /// Nowy przepływ synchronizacji: klient zgłasza koniec fali natychmiast po jej zakończeniu.
    /// Serwer decyduje, który gracz naprawdę czeka, i wysyła mu overlay przez TargetRpc.
    /// Dopiero gdy obaj zgłoszą koniec, serwer wysyła RpcStartDecreePhase do obu.
    [Server]
    public void OnWaveFinishedReceived(int playerIdx, int escaped)
    {
        if      (playerIdx == 1) _p1WaveEscaped = escaped;
        else if (playerIdx == 2) _p2WaveEscaped = escaped;

        _waveFinishedCount++;
        if (_waveFinishedCount < 2)
        {
            // Tylko ten gracz skończył – pokaż mu overlay "Oczekiwanie".
            var players = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None);
            foreach (var p in players)
            {
                if (p.playerIndex == playerIdx && p.connectionToClient != null)
                {
                    RpcShowWaitingOverlay(p.connectionToClient);
                    break;
                }
            }
            return;
        }

        bool p1Failed = _p1WaveEscaped == 0;
        bool p2Failed = _p2WaveEscaped == 0;

        if (p1Failed) player1HP = Mathf.Max(0, player1HP - 1);
        if (p2Failed) player2HP = Mathf.Max(0, player2HP - 1);

        _waveFinishedCount = 0;
        _p1WaveEscaped     = -1;
        _p2WaveEscaped     = -1;

        bool p1Dead = player1HP <= 0;
        bool p2Dead = player2HP <= 0;

        if (p1Dead || p2Dead)
        {
            int winner = (p1Dead && p2Dead) ? 0 : (p1Dead ? 2 : 1);
            RpcTriggerEndGame(winner);
            return;
        }

        _currentRound++;
        int nextSeed = matchSeed + _currentRound;
        ResetForNextRound();
        RpcStartDecreePhase(nextSeed, _currentRound);
        StartPreparationTimer(60f);
    }

    /// Stary handler zachowany dla kompatybilności — nowy przepływ używa OnWaveFinishedReceived.
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
            int winner = (p1Dead && p2Dead) ? 0 : (p1Dead ? 2 : 1);
            RpcTriggerEndGame(winner);
            return;
        }

        _currentRound++;
        int nextSeed = matchSeed + _currentRound;
        ResetForNextRound();
        RpcCleanupBoard();
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

    // ── Czyszczenie planszy ───────────────────────────────────────────────

    void CleanupBoardLocal()
    {
        foreach (var p in FindObjectsByType<pojazd>(FindObjectsSortMode.None))
            if (p != null) Destroy(p.gameObject);

        foreach (var k in FindObjectsByType<Kolec>(FindObjectsSortMode.None))
            if (k != null) Destroy(k.gameObject);

        foreach (var p in FindObjectsByType<Pocisk>(FindObjectsSortMode.None))
            if (p != null) Destroy(p.gameObject);

        foreach (var p in FindObjectsByType<PociskArmatni>(FindObjectsSortMode.None))
            if (p != null) Destroy(p.gameObject);
    }

    [ClientRpc]
    public void RpcCleanupBoard()
    {
        CleanupBoardLocal();
        MultiplayerCameraSwitcher.Instance?.ResetCameraToOwnBoard();
    }

    // ── Synchronizacja efektów Mocy Specjalnych ───────────────────────────

    // Efekty na wieżach są ZSYNCHRONIZOWANE przez ClientRpc — wieże nie mają NetworkIdentity
    // (generowane lokalnie z seeda), więc NetworkServer.Destroy nie może ich dotknąć.
    // ClientRpc odpala identyczny kod na obu klientach → identyczny wynik, brak widmowych wież.

    [ClientRpc]
    public void RpcApplyAirstrikeOnTowers(Vector3 center, float radius, float damage)
    {
        foreach (var col in Physics.OverlapSphere(center, radius))
        {
            WiezaBaza wieza = col.GetComponent<WiezaBaza>()
                           ?? col.GetComponentInParent<WiezaBaza>();
            if (wieza != null && wieza.gameObject.activeInHierarchy)
                wieza.TakeDamage(damage);
        }
    }

    // Nalot na pojazdy: RPC trafia oba klienty.
    // U rzucającego zabija realne pojazdy (isGhost=false) → game state OK.
    // U przeciwnika zabija duchy (isGhost=true) → Smierc() pomija OnVehicleRemoved → tylko wizualne.
    [ClientRpc]
    public void RpcApplyAirstrikeOnVehicles(Vector3 center, float radius, float damage)
    {
        foreach (var col in Physics.OverlapSphere(center, radius))
        {
            pojazd p = col.GetComponent<pojazd>();
            if (p != null && p.gameObject.activeInHierarchy)
                p.OdejmijHp(damage, przebijaPancerz: true);
        }
    }

    [ClientRpc]
    public void RpcApplyShieldOnTowers(Vector3 center, float radius, float duration)
    {
        foreach (var col in Physics.OverlapSphere(center, radius))
        {
            WiezaBaza wieza = col.GetComponent<WiezaBaza>()
                           ?? col.GetComponentInParent<WiezaBaza>();
            if (wieza != null && wieza.gameObject.activeInHierarchy)
                wieza.AktywujTarcze(duration);
        }
    }

    [ClientRpc]
    public void RpcApplyBoostOnTowers(Vector3 center, float radius, float speedMult, float duration)
    {
        foreach (var col in Physics.OverlapSphere(center, radius))
        {
            WiezaBaza wieza = col.GetComponent<WiezaBaza>()
                           ?? col.GetComponentInParent<WiezaBaza>();
            if (wieza != null && wieza.gameObject.activeInHierarchy)
                wieza.BoostAttackSpeed(speedMult, duration);
        }
    }

    // Boost pojazdów: RPC trafia oba klienty.
    // U rzucającego przyspiesza realne pojazdy (isGhost=false) → game state OK.
    // U przeciwnika przyspiesza duchy (isGhost=true) → tylko wizualne.
    [ClientRpc]
    public void RpcApplyBoostOnVehicles(Vector3 center, float radius, float flat, float duration)
    {
        foreach (var col in Physics.OverlapSphere(center, radius))
        {
            pojazd p = col.GetComponent<pojazd>();
            if (p != null && p.gameObject.activeInHierarchy)
                p.DoladujPredkosc(flat, duration);
        }
    }

    [ClientRpc]
    void RpcPrepareNextRound(int seed, int round)
    {
        GenerateBothMaps(seed, round);
        GameplayUIManager.Instance?.PrepareNextNetworkRound(round);
    }

    /// Nowy RPC: czyści planszę, resetuje kamerę, generuje mapy nowej rundy,
    /// a potem u obu klientów jednocześnie pokazuje panel Dekretów.
    /// Szybszy gracz przestaje czekać i widzi dekrety w tej samej chwili co wolniejszy.
    [ClientRpc]
    void RpcStartDecreePhase(int seed, int round)
    {
        CleanupBoardLocal();
        MultiplayerCameraSwitcher.Instance?.ResetCameraToOwnBoard();
        GenerateBothMaps(seed, round);
        GameplayUIManager.Instance?.ShowDecreePhaseMP(round);
    }

    [Server]
    public void ResetForNextRound()
    {
        _actionPhaseStarted = false;
        _p1QueueIndices     = null;
        _p2QueueIndices     = null;
        _earlyStartCounting = false;
        _ghostsSpawned      = false;
        _waveFinishedCount  = 0;
        _p1WaveEscaped      = -1;
        _p2WaveEscaped      = -1;
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

        // Duchy pojawiają się na mapie WŁAŚCICIELA pojazdów (tam gdzie jego własne prawdziwe pojazdy).
        // ownerPlayerIdx 1 → vehicleSpawnerMap1 / finishLine1, 2 → vehicleSpawnerMap2 / finishLine2.
        VehicleSpawner ghostSpawner = ownerPlayerIdx == 1 ? vehicleSpawnerMap1 : vehicleSpawnerMap2;
        FinishLine     ghostFinish  = ownerPlayerIdx == 1 ? finishLine1        : finishLine2;

        if (ghostSpawner != null && ghostFinish != null)
            ghostSpawner.StartSpawningGhosts(vehicleIndices, ghostFinish);
    }
}
