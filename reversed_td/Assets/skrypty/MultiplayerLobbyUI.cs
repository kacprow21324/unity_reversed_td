using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
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

    // ── Singleton ──────────────────────────────────────────────────────────

    public static MultiplayerLobbyUI Instance { get; private set; }

    // ── Stan wewnętrzny ────────────────────────────────────────────────────

    bool _lobbyShown;

    // ── Unity lifecycle ────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // Scena załadowała duplikat Canvas — ukryj cały nowy root.
            // Oryginalna instancja DDOL nadal działa i zachowuje wszystkie referencje.
            transform.root.gameObject.SetActive(false);
            return;
        }

        Instance = this;
        _lobbyShown = false;

        // Uczyń cały Canvas (panele, pola IP/nick, itp.) trwałym między scenami.
        // Referencje inspektora nigdy nie znikną — obiekty nie są niszczone przy przeładowaniu.
        DontDestroyOnLoad(transform.root.gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;

        NicknameManager.EnsureExists();
        StartCoroutine(CleanupStaleNetworkManager());
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "menu")
        {
            // Powrót do menu — pokaż Canvas.
            // NIE nawigujemy ręcznie do żadnego panelu: DDOL zachował stan z poprzedniej wizyty.
            // Chowamy tylko panele sieciowe, które nie mają sensu po rozłączeniu.
            transform.root.gameObject.SetActive(true);
            _lobbyShown = false;
            if (waitingPanel) waitingPanel.SetActive(false);
            if (lobbyPanel)   lobbyPanel.SetActive(false);
            StartCoroutine(CleanupStaleNetworkManager());
        }
        else
        {
            // Wejście do sceny gry — ukryj cały Canvas lobby.
            transform.root.gameObject.SetActive(false);
        }
    }

    System.Collections.IEnumerator CleanupStaleNetworkManager()
    {
        // Czekamy jedną klatkę — Mirror kończy swoje callbacki po powrocie offlineScene
        yield return null;

        var nm = NetworkManager.singleton;
        if (nm == null) yield break;

        // Mirror z dontDestroyOnLoad=true (domyślne) zawsze umieszcza NM w DontDestroyOnLoad,
        // więc sprawdzanie "isInActiveScene" byłoby zawsze false i zniszczyłoby świeży NM.
        // Zamiast niszczyć: wystarczy zatrzymać aktywne połączenie jeśli istnieje
        // (edge case: Mirror offlineScene przerzucił nas do menu z aktywną siecią).
        // NM sam w sobie jest nadal zdatny do użycia po StopHost/StopClient.
        if (NetworkServer.active)
        {
            nm.StopHost();
            yield return null; // klatka na zamknięcie socketu
        }
        else if (NetworkClient.active)
        {
            nm.StopClient();
            yield return null;
        }
    }

    void Start()
    {
        BudujPrzyciskiWstecz();
    }

    void Update()
    {
        // Przejście WaitingPanel → LobbyPanel gdy lokalny NetworkPlayer jest gotowy
        if (_lobbyShown) return;
        if (!NetworkClient.active && !NetworkServer.active) return;
        if (NetworkClient.localPlayer == null) return;

        ShowLobbyPanel();
    }

    // ── Przyciski nawigacyjne ──────────────────────────────────────────────

    /// Tworzy przyciski "Wstecz" i "Anuluj" na panelach lobby jeśli jeszcze nie istnieją.
    void BudujPrzyciskiWstecz()
    {
        // multiplayerPanel → "← Wstecz" (wróć do głównego menu)
        if (multiplayerPanel != null &&
            multiplayerPanel.transform.Find("Btn_← Wstecz") == null)
        {
            var btn = UIHelper.MakeButton(
                multiplayerPanel.transform, "← Wstecz",
                new Color(0.18f, 0.18f, 0.22f), new Color(0.26f, 0.26f, 0.32f),
                new Color(0.10f, 0.10f, 0.12f), new Color(0.88f, 0.88f, 0.91f),
                OnBackFromMultiplayerPanel);

            var rt = btn.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0f, 1f);
            rt.anchorMax        = new Vector2(0f, 1f);
            rt.pivot            = new Vector2(0f, 1f);
            rt.sizeDelta        = new Vector2(130f, 42f);
            rt.anchoredPosition = new Vector2(10f, -10f);
        }

        // waitingPanel → "Anuluj" (rozłącz i wróć)
        if (waitingPanel != null &&
            waitingPanel.transform.Find("Btn_Anuluj") == null)
        {
            var btn = UIHelper.MakeButton(
                waitingPanel.transform, "Anuluj",
                new Color(0.30f, 0.10f, 0.10f), new Color(0.44f, 0.14f, 0.14f),
                new Color(0.10f, 0.10f, 0.12f), new Color(0.88f, 0.88f, 0.91f),
                Disconnect);

            var rt = btn.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.5f, 0f);
            rt.anchorMax        = new Vector2(0.5f, 0f);
            rt.pivot            = new Vector2(0.5f, 0f);
            rt.sizeDelta        = new Vector2(160f, 44f);
            rt.anchoredPosition = new Vector2(0f, 20f);
        }
    }

    void OnBackFromMultiplayerPanel()
    {
        FindFirstObjectByType<MainMenuLogic>()?.OnClickBack();
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

        // Fallback: jeśli referencja inspektora jest stale/null, szukaj w scenie
        if (lobbyPanel == null)
            lobbyPanel = FindFirstObjectByType<LobbyPanelUI>(FindObjectsInactive.Include)?.gameObject;

        if (lobbyPanelUI == null)
            lobbyPanelUI = FindFirstObjectByType<LobbyPanelUI>(FindObjectsInactive.Include);

        if (lobbyPanel != null)
        {
            lobbyPanel.SetActive(true);
            lobbyPanelUI?.OnLobbyOpened();
        }
        else
        {
            Debug.LogWarning("[MultiplayerLobbyUI] Brak referencji do lobbyPanel. Sprawdź hierarchię sceny.");
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
