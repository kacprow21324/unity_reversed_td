using Mirror;
using UnityEngine;

/// Zarządza stanem meczu multiplayer: synchronizacja ziarna RNG i kontrola faz.
/// Umieszczaj tylko na scenach wieloosobowych (jako NetworkBehaviour z NetworkIdentity).
public class NetworkMatchManager : NetworkBehaviour
{
    public static NetworkMatchManager Instance { get; private set; }

    // ── SyncVars ──────────────────────────────────────────────────────────

    [SyncVar(hook = nameof(OnSeedReceived))]
    public int matchSeed;

    [SyncVar]
    private bool _actionPhaseStarted;

    // ── Flagi lokalne serwera ─────────────────────────────────────────────

    private bool _isMapGenerated = false;

    // ── Referencje bezpośrednie (omija TowerSpawner.Instance) ────────────

    [Header("Referencje Map (Multiplayer)")]
    public TowerSpawner spawnerMap1;
    public TowerSpawner spawnerMap2;

    // ── Unity / Mirror lifecycle ──────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public override void OnStartServer()
    {
        // Losujemy seed od razu, ale NIE generujemy jeszcze mapy.
        // Generacja nastąpi przez RpcGenerateMapsOnClients gdy obaj gracze są w scenie.
        matchSeed           = Random.Range(1000, 999999);
        _actionPhaseStarted = false;
        _isMapGenerated     = false;
        Debug.Log($"[NetworkMatchManager] Serwer wygenerował seed: {matchSeed}. Czekam na 2. gracza.");
    }

    // ── Hook SyncVar — tylko informacyjny, generacja przez RPC ───────────

    void OnSeedReceived(int oldSeed, int newSeed)
    {
        if (newSeed == 0) return;
        Debug.Log($"[NetworkMatchManager] Seed odebrany: {newSeed} (generacja map nastąpi przez RPC).");
    }

    // ── Update: serwer czeka na 2 połączenia, potem generuje mapy ─────────

    void Update()
    {
        if (!isServer) return;

        // PROBLEM 1 FIX: generuj mapy dopiero gdy OBAJ gracze są w scenie.
        if (!_isMapGenerated && NetworkServer.connections.Count == 2)
        {
            _isMapGenerated = true;
            Debug.Log($"[NetworkMatchManager] Dwóch graczy w scenie. Generuję mapy seedem {matchSeed}.");
            RpcGenerateMapsOnClients(matchSeed);
        }

        // Mechanizm gotowości (opcjonalny — uzupełnia CmdTryStartGame)
        if (!_actionPhaseStarted && NetworkServer.connections.Count == 2)
        {
            NetworkPlayer[] players = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None);
            if (players.Length == 2)
            {
                bool allReady = true;
                foreach (var p in players)
                    if (!p.isReady) { allReady = false; break; }

                if (allReady)
                {
                    foreach (var p in players) p.isReady = false;
                    ForceStartActionPhase();
                }
            }
        }
    }

    // ── RPC: generacja map po stronie każdego klienta ─────────────────────

    [ClientRpc]
    void RpcGenerateMapsOnClients(int seed)
    {
        Debug.Log($"[NetworkMatchManager] RPC: Generuję mapy seedem {seed}.");
        GenerateBothMaps(seed);
    }

    void GenerateBothMaps(int seed)
    {
        // Każdy spawner woła Random.InitState(seed) jako pierwszą linię —
        // obie mapy startują z identycznym stanem RNG → identyczny układ.
        spawnerMap1?.GenerateMapWithSeed(seed);
        spawnerMap2?.GenerateMapWithSeed(seed);
    }

    // ── RPC: start fazy ataku ─────────────────────────────────────────────

    [ClientRpc]
    public void RpcStartActionPhase()
    {
        Debug.Log("[NetworkMatchManager] RPC: Faza ataku rozpoczęta.");
        GameplayUIManager.Instance?.StartNetworkRound();
    }

    // ── API publiczne ─────────────────────────────────────────────────────

    /// Jedyny serwer-autoryzowany punkt startu fali. Wywołuje CmdTryStartGame przez NetworkPlayer.
    [Server]
    public void ForceStartActionPhase()
    {
        if (_actionPhaseStarted) return;
        _actionPhaseStarted = true;
        RpcStartActionPhase();
    }

    /// Most dla GameplayUIManager — trzyma Mirror z dala od skryptu singleplayerowego.
    public void RequestStartFromLocalPlayer()
    {
        NetworkClient.localPlayer?.GetComponent<NetworkPlayer>()?.CmdTryStartGame();
    }

    [Server]
    public void ResetForNextRound()
    {
        _actionPhaseStarted = false;
        Debug.Log("[NetworkMatchManager] Reset gotowości dla następnej rundy.");
    }
}
