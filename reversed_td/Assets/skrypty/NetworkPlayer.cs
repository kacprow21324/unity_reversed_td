using Mirror;
using UnityEngine;

public class NetworkPlayer : NetworkBehaviour
{
    [SyncVar] public int playerGold = 1000;
    [SyncVar] public int playerLives = 5;

    [SyncVar(hook = nameof(OnReadyChanged))]
    public bool isReady = false;

    [SyncVar] public int playerIndex;

    public override void OnStartServer()
    {
        // Host dostaje indeks 1, każdy kolejny klient dostaje 2
        playerIndex = NetworkServer.connections.Count == 1 ? 1 : 2;
    }

    void OnReadyChanged(bool oldValue, bool newValue)
    {
        Debug.Log($"[NetworkPlayer] Gracz {playerIndex} zmienił gotowość: {newValue}");
    }

    [Command]
    public void CmdSetReady(bool state)
    {
        isReady = state;
    }

    /// Jedyny sieciowy punkt wejścia do startu fali.
    /// Wywoływany przez GameplayUIManager → NetworkMatchManager.RequestStartFromLocalPlayer().
    /// Wymóg: dokładnie 2 połączone klienty — bez sprawdzania isReady.
    [Command]
    public void CmdTryStartGame()
    {
        Debug.Log($"[NetworkPlayer] Próba startu: podłączonych graczy = {NetworkServer.connections.Count}");

        if (NetworkServer.connections.Count < 2)
        {
            Debug.Log("[NetworkPlayer] CmdTryStartGame: czekamy na drugiego gracza.");
            return;
        }

        NetworkMatchManager.Instance?.ForceStartActionPhase();
    }
}
