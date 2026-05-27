using System.Collections;
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
        // Klient może wrócić do menu automatycznie przez Mirror's offlineScene
        // (gdy host rozłączy się) — bez przejścia przez GameManager.DisconnectAndLoad.
        // W takim razie NetworkManager jest wciąż DDOL z brudnym stanem.
        // Niszczymy go tutaj, żeby StartHostDelayed/StartClientDelayed dostały świeżą instancję
        // z własnej sceny menu.
        StartCoroutine(CleanupStaleNetworkManager());
    }

    System.Collections.IEnumerator CleanupStaleNetworkManager()
    {
        // Czekamy jedną klatkę — Mirror kończy swoje callbacki po powrocie offlineScene
        yield return null;

        var nm = NetworkManager.singleton;
        if (nm == null) yield break;

        // Jeśli sieć jest nieaktywna i NM pochodzi z poprzedniej sesji (jest DDOL),
        // niszczymy go żeby scena menu stworzyła świeżą instancję.
        // Rozpoznajemy "brudny" NM po tym, że jest DDOL (nie należy do aktywnej sceny).
        if (!NetworkServer.active && !NetworkClient.active)
        {
            bool isInActiveScene = nm.gameObject.scene ==
                UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!isInActiveScene)
            {
                // DDOL NetworkManager — zniszcz, żeby scena menu zainicjowała nowy
                Destroy(nm.gameObject);
                yield return null; // klatka na Destroy
            }
        }
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
        StartCoroutine(StartHostDelayed());
    }

    public void JoinGame(string ipAddress)
    {
        if (NetworkManager.singleton == null)
        {
            Debug.LogError("[MultiplayerLobbyUI] Brak NetworkManager w scenie!");
            return;
        }
        SaveNicknameFromField();
        string ip = string.IsNullOrWhiteSpace(ipAddress) ? "localhost" : ipAddress.Trim();
        StartCoroutine(StartClientDelayed(ip));
    }

    public void JoinGameFromInput()
    {
        string ip = ipInputField != null ? ipInputField.text : "localhost";
        JoinGame(ip);
    }

    // ── Coroutines startu sieci ────────────────────────────────────────────

    /// Startuje host po jednej klatce przerwy.
    /// NetworkManager jest zawsze świeży (zniszczony przez GameManager.DisconnectAndLoad),
    /// więc nie ma czego zatrzymywać — wystarczy zabezpieczenie na wypadek
    /// wielokrotnego kliknięcia przycisku.
    IEnumerator StartHostDelayed()
    {
        var nm = NetworkManager.singleton;
        if (nm == null)
        {
            Debug.LogError("[MultiplayerLobbyUI] NetworkManager jest NULL. " +
                           "Upewnij się że obiekt NetworkManager jest w scenie menu.");
            yield break;
        }

        nm.offlineScene = string.Empty; // lobby zarządza scenami ręcznie
        nm.onlineScene  = string.Empty;

        // Zabezpieczenie przed podwójnym kliknięciem
        if (NetworkServer.active)      { nm.StopHost();   yield return null; }
        else if (NetworkClient.active) { nm.StopClient(); yield return null; }

        yield return null;

        nm.StartHost();
        ShowWaitingPanel();
        SetStatus("Hosting... czekam na gracza.");
    }

    /// Analogicznie dla klienta.
    IEnumerator StartClientDelayed(string ip)
    {
        var nm = NetworkManager.singleton;
        if (nm == null)
        {
            Debug.LogError("[MultiplayerLobbyUI] NetworkManager jest NULL. " +
                           "Upewnij się że obiekt NetworkManager jest w scenie menu.");
            yield break;
        }

        nm.offlineScene = string.Empty;
        nm.onlineScene  = string.Empty;

        if (NetworkServer.active)      { nm.StopHost();   yield return null; }
        else if (NetworkClient.active) { nm.StopClient(); yield return null; }

        yield return null;

        nm.networkAddress = ip;
        nm.StartClient();
        ShowWaitingPanel();
        SetStatus($"Łączenie z {ip}...");
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
