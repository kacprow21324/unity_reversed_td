using Mirror;
using UnityEngine;

public class NetworkPlayer : NetworkBehaviour
{
    // ── Gra ───────────────────────────────────────────────────────────────

    [SyncVar] public int playerGold  = 1000;
    [SyncVar] public int playerLives = 5;

    [SyncVar(hook = nameof(OnReadyChanged))]
    public bool isReady = false;

    [SyncVar] public int playerIndex;

    // ── Lobby ─────────────────────────────────────────────────────────────

    [SyncVar(hook = nameof(OnNicknameChanged))]
    public string playerNickname = "Gracz";

    [SyncVar(hook = nameof(OnLobbyReadyChanged))]
    public bool isLobbyReady = false;

    // Ustawienia wybierane przez Hosta (playerIndex == 1), widoczne u wszystkich
    [SyncVar(hook = nameof(OnMapIndexChanged))]
    public int selectedMapIndex = 0;

    [SyncVar(hook = nameof(OnStartGoldChanged))]
    public int lobbyStartGold = 500;

    // ── Lifecycle ─────────────────────────────────────────────────────────

    public override void OnStartServer()
    {
        // connectionId == 0 → własne połączenie hosta
        playerIndex = connectionToClient?.connectionId == 0 ? 1 : 2;
    }

    public override void OnStartLocalPlayer()
    {
        NicknameManager.EnsureExists();
        CmdSetNickname(NicknameManager.LocalNickname);
    }

    // ── Haki SyncVar ──────────────────────────────────────────────────────

    void OnReadyChanged(bool _, bool __)       => Debug.Log($"[NetworkPlayer] Gracz {playerIndex} isReady={isReady}");
    void OnNicknameChanged(string _, string __)
    {
        LobbyPanelUI.Instance?.RefreshPlayerList();
        MatchHPUI.Instance?.RefreshHP();
    }
    void OnLobbyReadyChanged(bool _, bool __)   => LobbyPanelUI.Instance?.RefreshPlayerList();
    void OnMapIndexChanged(int _, int newVal)   => LobbyPanelUI.Instance?.OnHostMapChanged(newVal);
    void OnStartGoldChanged(int _, int newVal)  => LobbyPanelUI.Instance?.OnHostGoldChanged(newVal);

    // ── Commands: lobby ───────────────────────────────────────────────────

    [Command]
    public void CmdSetNickname(string nick)
    {
        nick = string.IsNullOrWhiteSpace(nick) ? "Gracz" : nick.Trim();
        if (nick.Length > 24) nick = nick.Substring(0, 24);
        playerNickname = nick;
    }

    [Command]
    public void CmdSetLobbyReady(bool state) => isLobbyReady = state;

    [Command]
    public void CmdSetMapIndex(int index)
    {
        if (playerIndex == 1) selectedMapIndex = index;
    }

    [Command]
    public void CmdSetStartGold(int gold)
    {
        if (playerIndex == 1) lobbyStartGold = Mathf.Max(0, gold);
    }

    /// Żądanie startu meczu: walidacja → sync golda na wszystkich klientach → ServerChangeScene.
    [Command]
    public void CmdTryStartMatch(string sceneName, int startGold)
    {
        if (playerIndex != 1) return; // tylko Host

        var players = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None);
        if (players.Length < 2) return;

        foreach (var p in players)
            if (!p.isLobbyReady) return;

        LobbySettings.StartGold = startGold;
        RpcSyncStartGold(startGold); // klient (nie-host) dostaje gold przed zmianą sceny
        NetworkManager.singleton.ServerChangeScene(sceneName);
    }

    [ClientRpc]
    void RpcSyncStartGold(int gold)
    {
        LobbySettings.StartGold = gold;
    }

    // ── Commands: gra ─────────────────────────────────────────────────────

    [Command]
    public void CmdSetReady(bool state) => isReady = state;

    [Command]
    public void CmdTryStartGame() => isReady = true;

    /// Wysyła kolejkę pojazdów do serwera i oznacza gracza jako gotowego do wczesnego startu.
    [Command]
    public void CmdSubmitQueueAndReady(int[] vehicleIndices)
    {
        isReady = true;
        NetworkMatchManager.Instance?.StorePlayerQueue(playerIndex, vehicleIndices);
    }

    /// Wysyła kolejkę po starcie fazy ataku (ekspiracja timera) bez zmiany flagi gotowości.
    [Command]
    public void CmdSubmitQueue(int[] vehicleIndices)
    {
        NetworkMatchManager.Instance?.StorePlayerQueue(playerIndex, vehicleIndices);
    }

    /// Klient raportuje koniec fali natychmiast po jej zakończeniu.
    /// Serwer czeka na obydwu graczy, przetwarza HP, po czym wysyła RpcStartDecreePhase do obu.
    [Command]
    public void CmdReportWaveFinished(int escaped)
    {
        NetworkMatchManager.Instance?.OnWaveFinishedReceived(playerIndex, escaped);
    }

    /// Stara komenda — zachowana dla kompatybilności.
    [Command]
    public void CmdReportRoundResult(int escaped)
    {
        NetworkMatchManager.Instance?.OnRoundResultReceived(playerIndex, escaped);
    }

    // ── Synchronizacja Mocy Specjalnych ───────────────────────────────────

    /// Centralny hub Mocy Specjalnych dla trybu Multiplayer.
    /// Serwer rozsyła efekt ClientRpc do obu klientów — gwarantuje identyczny wynik bez NetworkIdentity.
    ///
    /// powerType  : "airstrike" | "shield_towers" | "boost_towers"
    /// isOnOwnMap : true = własna plansza, false = plansza wroga
    /// paramA     : promień (wszystkie moce)
    /// paramB     : obrażenia (airstrike) | czas trwania (shield) | mnożnik prędkości (boost)
    /// paramC     : czas trwania (boost); nieużywany przez pozostałe
    [Command]
    public void CmdApplySpecialPower(
        string powerType, Vector3 position, bool isOnOwnMap,
        float paramA, float paramB, float paramC)
    {
        var nmm = NetworkMatchManager.Instance;
        if (nmm == null) return;

        switch (powerType)
        {
            case "airstrike":
                // paramA = radius, paramB = damage — niszczy wieże na obu klientach
                nmm.RpcApplyAirstrikeOnTowers(position, paramA, paramB);
                break;

            case "airstrike_vehicles":
                // paramA = radius, paramB = damage — niszczy pojazdy (+ duchy) na obu klientach
                nmm.RpcApplyAirstrikeOnVehicles(position, paramA, paramB);
                break;

            case "shield_towers":
                // paramA = radius, paramB = duration
                nmm.RpcApplyShieldOnTowers(position, paramA, paramB);
                break;

            case "boost_towers":
                // paramA = radius, paramB = speedMult, paramC = duration
                nmm.RpcApplyBoostOnTowers(position, paramA, paramB, paramC);
                break;

            case "boost_vehicles":
                // paramA = radius, paramB = flat bonus, paramC = duration
                nmm.RpcApplyBoostOnVehicles(position, paramA, paramB, paramC);
                break;
        }
    }
}
