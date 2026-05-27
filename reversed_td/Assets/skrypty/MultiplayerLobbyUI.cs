using Mirror;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MultiplayerLobbyUI : MonoBehaviour
{
    // ── Panele ─────────────────────────────────────────────────────────────

    [Header("Panele")]
    public GameObject multiplayerPanel;
    public GameObject waitingPanel;
    public GameObject lobbyPanel;

    // ── Połączenie ─────────────────────────────────────────────────────────

    [Header("Pole IP i Nick")]
    public TMP_InputField ipInputField;
    public TMP_InputField nicknameInputField;

    [Header("Status (WaitingPanel)")]
    public TextMeshProUGUI statusLabel;

    // ── Referencja do LobbyPanelUI ─────────────────────────────────────────

    [Header("Lobby Panel UI")]
    public LobbyPanelUI lobbyPanelUI;

    // ── Stan wewnętrzny ────────────────────────────────────────────────────

    bool _lobbyShown;

    // ── Unity lifecycle ────────────────────────────────────────────────────

    void Awake()
    {
        NicknameManager.EnsureExists();
    }

    void Update()
    {
        // Przejście WaitingPanel → LobbyPanel gdy lokalny NetworkPlayer jest gotowy
        if (_lobbyShown) return;
        if (!NetworkClient.active && !NetworkServer.active) return;
        if (NetworkClient.localPlayer == null) return;

        ShowLobbyPanel();
    }

    // ── Publiczne akcje ────────────────────────────────────────────────────

    public void HostGame()
    {
        if (NetworkManager.singleton == null)
        {
            Debug.LogError("[MultiplayerLobbyUI] Brak NetworkManager w scenie!");
            return;
        }

        SaveNicknameFromField();

        // Lobby zarządza zmianą sceny ręcznie przez CmdTryStartMatch → ServerChangeScene.
        // Jeśli onlineScene jest ustawiona, Mirror załaduje ją natychmiast po połączeniu
        // z pominięciem całego lobby — dlatego czyścimy ją przed startem.
        NetworkManager.singleton.onlineScene = string.Empty;

        // Zabezpieczenie: rozłącz stare połączenie (np. powrót z poprzedniej gry MP)
        if (NetworkServer.active)
            NetworkManager.singleton.StopHost();
        else if (NetworkClient.active)
            NetworkManager.singleton.StopClient();

        NetworkManager.singleton.StartHost();
        ShowWaitingPanel();
        SetStatus("Hosting... czekam na gracza.");
    }

    public void JoinGame(string ipAddress)
    {
        if (NetworkManager.singleton == null)
        {
            Debug.LogError("[MultiplayerLobbyUI] Brak NetworkManager w scenie!");
            return;
        }

        SaveNicknameFromField();

        // Analogicznie jak HostGame — blokujemy auto-scenę.
        NetworkManager.singleton.onlineScene = string.Empty;

        // Zabezpieczenie: rozłącz stare połączenie (np. powrót z poprzedniej gry MP)
        if (NetworkServer.active)
            NetworkManager.singleton.StopHost();
        else if (NetworkClient.active)
            NetworkManager.singleton.StopClient();

        string ip = string.IsNullOrWhiteSpace(ipAddress) ? "localhost" : ipAddress.Trim();
        NetworkManager.singleton.networkAddress = ip;
        NetworkManager.singleton.StartClient();
        ShowWaitingPanel();
        SetStatus($"Łączenie z {ip}...");
    }

    public void JoinGameFromInput()
    {
        string ip = ipInputField != null ? ipInputField.text : "localhost";
        JoinGame(ip);
    }

    public void Disconnect()
    {
        if (NetworkServer.active)
            NetworkManager.singleton.StopHost();
        else if (NetworkClient.active)
            NetworkManager.singleton.StopClient();

        _lobbyShown = false;
        ShowMultiplayerPanel();
        SetStatus("");
    }

    // ── Prywatne helpers ───────────────────────────────────────────────────

    void SaveNicknameFromField()
    {
        if (nicknameInputField == null) return;
        NicknameManager.Instance?.SaveNickname(nicknameInputField.text);
    }

    void ShowLobbyPanel()
    {
        _lobbyShown = true;

        if (waitingPanel)     waitingPanel.SetActive(false);
        if (multiplayerPanel) multiplayerPanel.SetActive(false);
        if (lobbyPanel)
        {
            lobbyPanel.SetActive(true);
            lobbyPanelUI?.OnLobbyOpened();
        }
        else
        {
            Debug.LogWarning("[MultiplayerLobbyUI] Brak referencji do lobbyPanel. Uruchom Tools/Generuj UI Lobby.");
        }
    }

    void ShowWaitingPanel()
    {
        _lobbyShown = false;
        if (multiplayerPanel) multiplayerPanel.SetActive(false);
        if (waitingPanel)     waitingPanel.SetActive(true);
        if (lobbyPanel)       lobbyPanel.SetActive(false);
    }

    void ShowMultiplayerPanel()
    {
        if (multiplayerPanel) multiplayerPanel.SetActive(true);
        if (waitingPanel)     waitingPanel.SetActive(false);
        if (lobbyPanel)       lobbyPanel.SetActive(false);
    }

    void SetStatus(string message)
    {
        if (statusLabel) statusLabel.text = message;
    }
}
