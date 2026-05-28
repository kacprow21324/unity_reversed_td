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

    bool       _lobbyShown;
    float      _lobbyRefreshTimer;
    int        _localPlayerFrameCount;
    GameObject _uiCanvasRoot; // root Canvas zawierający lobbyPanel / waitingPanel / multiplayerPanel

    // ── Unity lifecycle ────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // Duplikat — wyłącz i pomiń (oryginalny DDOL obiekt nadal działa).
            transform.root.gameObject.SetActive(false);
            return;
        }

        Instance = this;
        _lobbyShown = false;

        // LobbyManager (ten obiekt) → DDOL
        DontDestroyOnLoad(transform.root.gameObject);

        // Canvas z panelami UI → również DDOL.
        // Dzięki temu referencje inspektora LobbyPanelUI (playerListRoot, dropdown itp.)
        // nigdy nie stają się stale po przeładowaniu sceny.
        if (lobbyPanel != null)
        {
            _uiCanvasRoot = lobbyPanel.transform.root.gameObject;
            if (_uiCanvasRoot != transform.root.gameObject)
                DontDestroyOnLoad(_uiCanvasRoot);
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
        NicknameManager.EnsureExists();
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
            transform.root.gameObject.SetActive(true);

            // Po przeładowaniu menu scena tworzy duplikat Canvas — zniszcz go.
            // Nasz DDOL Canvas zawiera właściwe referencje inspektora.
            DestroyDuplicateCanvas(scene);
            _uiCanvasRoot?.SetActive(true);

            _lobbyShown = false;
            _localPlayerFrameCount = 0;
            if (waitingPanel) waitingPanel.SetActive(false);
            if (lobbyPanel)   lobbyPanel.SetActive(false);
            ShowMultiplayerPanel();
        }
        else
        {
            // Scena gry — ukryj całe UI lobby.
            transform.root.gameObject.SetActive(false);
            _uiCanvasRoot?.SetActive(false);
        }
    }

    void DestroyDuplicateCanvas(Scene menuScene)
    {
        if (_uiCanvasRoot == null) return;
        foreach (var root in menuScene.GetRootGameObjects())
        {
            if (root != _uiCanvasRoot && root.name == _uiCanvasRoot.name)
            {
                Destroy(root);
                return;
            }
        }
    }

    void Start()
    {
        BudujPrzyciskiWstecz();
    }

    void Update()
    {
        // ── Wykryj nieoczekiwane rozłączenie ──────────────────────────────
        bool inNetworkMode = _lobbyShown || (waitingPanel != null && waitingPanel.activeSelf);
        if (inNetworkMode && !NetworkClient.active && !NetworkServer.active)
        {
            _lobbyShown = false;
            _localPlayerFrameCount = 0;
            ShowMultiplayerPanel();
            SetStatus("Rozłączono.");
            return;
        }

        // ── Timer odświeżania listy graczy w lobby ────────────────────────
        if (_lobbyShown)
        {
            _lobbyRefreshTimer += Time.unscaledDeltaTime;
            if (_lobbyRefreshTimer >= 1f)
            {
                _lobbyRefreshTimer = 0f;
                LobbyPanelUI.Instance?.RefreshPlayerList();
            }
            return;
        }

        // ── Przejście waiting → lobby gdy localPlayer gotowy ──────────────
        if (!NetworkClient.active && !NetworkServer.active) { _localPlayerFrameCount = 0; return; }
        if (NetworkClient.localPlayer == null)              { _localPlayerFrameCount = 0; return; }

        // Czekaj kilka klatek — Mirror musi dostarczyć początkowe SyncVary
        // (nick, playerIndex) zanim odświeżymy listę graczy.
        _localPlayerFrameCount++;
        if (_localPlayerFrameCount < 5) return;

        ShowLobbyPanel();
    }

    // ── Przyciski nawigacyjne ──────────────────────────────────────────────

    void BudujPrzyciskiWstecz()
    {
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

    IEnumerator StartHostDelayed()
    {
        var nm = NetworkManager.singleton;
        if (nm == null) yield break;

        nm.offlineScene = string.Empty;
        nm.onlineScene  = string.Empty;

        if (NetworkServer.active)
        {
            nm.StopHost();
            // KCP (UDP) potrzebuje czasu na zamknięcie socketu — jedna klatka to za mało
            yield return new WaitForSecondsRealtime(0.25f);
        }
        else if (NetworkClient.active)
        {
            nm.StopClient();
            yield return new WaitForSecondsRealtime(0.25f);
        }

        yield return new WaitForSecondsRealtime(0.1f);

        nm.StartHost();
        ShowWaitingPanel();
        SetStatus("Hosting... czekam na gracza.");
    }

    IEnumerator StartClientDelayed(string ip)
    {
        var nm = NetworkManager.singleton;
        if (nm == null) yield break;

        nm.offlineScene = string.Empty;
        nm.onlineScene  = string.Empty;

        if (NetworkServer.active)
        {
            nm.StopHost();
            yield return new WaitForSecondsRealtime(0.25f);
        }
        else if (NetworkClient.active)
        {
            nm.StopClient();
            yield return new WaitForSecondsRealtime(0.25f);
        }

        yield return new WaitForSecondsRealtime(0.1f);

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
        _localPlayerFrameCount = 0;
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
        _lobbyRefreshTimer = 0f;

        if (waitingPanel)     waitingPanel.SetActive(false);
        if (multiplayerPanel) multiplayerPanel.SetActive(false);

        // Fallback jeśli referencja inspektora jest stale/null
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
        _localPlayerFrameCount = 0;
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
