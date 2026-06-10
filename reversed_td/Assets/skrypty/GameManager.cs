using System.Text;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public const int VICTORY_ROUND = 10;

    [Header("Panele Konca Gry")]
    public GameObject victoryPanel;
    public GameObject defeatPanel;
    public GameObject drawPanel;

    [Header("Statystyki na Ekranie Konca")]
    public TextMeshProUGUI victoryStatsText;
    public TextMeshProUGUI defeatStatsText;
    public TextMeshProUGUI drawStatsText;

    [Header("Przyciski SP — Restart")]
    public Button victoryRestartButton;
    public Button defeatRestartButton;

    [Header("Przyciski — Powrot do Lobby (MP)")]
    public Button victoryLobbyButton;
    public Button defeatLobbyButton;
    public Button drawLobbyButton;

    [Header("Przyciski — Wyjscie do Menu")]
    public Button victoryMenuButton;
    public Button defeatMenuButton;
    public Button drawMenuButton;

    public bool IsGameOver { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        EnsureDecreeManager();
    }

    void Start()
    {
        AutoFindIfNull();

        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (defeatPanel  != null) defeatPanel.SetActive(false);
        if (drawPanel    != null) drawPanel.SetActive(false);
        IsGameOver = false;

        if (GameStatistics.Instance != null)
            GameStatistics.Instance.gameStartTime = Time.realtimeSinceStartup;

        bool isMP = SceneManager.GetActiveScene().name.Contains("Multiplayer");

        if (victoryRestartButton != null) victoryRestartButton.gameObject.SetActive(!isMP);
        if (defeatRestartButton  != null) defeatRestartButton.gameObject.SetActive(!isMP);

        if (victoryLobbyButton != null) victoryLobbyButton.gameObject.SetActive(isMP);
        if (defeatLobbyButton  != null) defeatLobbyButton.gameObject.SetActive(isMP);
        if (drawLobbyButton    != null) drawLobbyButton.gameObject.SetActive(isMP);

        WireIfEmpty(victoryRestartButton, RestartScene);
        WireIfEmpty(defeatRestartButton,  RestartScene);
        WireIfEmpty(victoryMenuButton,    GoToMainMenu);
        WireIfEmpty(defeatMenuButton,     GoToMainMenu);
        WireIfEmpty(drawMenuButton,       GoToMainMenu);
        WireIfEmpty(victoryLobbyButton,   ReturnToLobby);
        WireIfEmpty(defeatLobbyButton,    ReturnToLobby);
        WireIfEmpty(drawLobbyButton,      ReturnToLobby);
    }

    // Automatycznie wypełnia puste pola szukając obiektów po nazwie w scenie.
    // Działa na każdej scenie bez ręcznego przypisywania w Inspektorze.
    void AutoFindIfNull()
    {
        if (victoryPanel == null) victoryPanel = FindInScene("VictoryPanel");
        if (defeatPanel  == null) defeatPanel  = FindInScene("DefeatPanel");

        if (victoryPanel != null)
        {
            if (victoryRestartButton == null)
                victoryRestartButton = FindButtonIn(victoryPanel.transform, "BtnRestart");
            if (victoryMenuButton == null)
                victoryMenuButton    = FindButtonIn(victoryPanel.transform, "BtnMenu");
            if (victoryStatsText == null)
                victoryStatsText     = FindTMPIn(victoryPanel.transform, "StatsText");
        }

        if (defeatPanel != null)
        {
            if (defeatRestartButton == null)
                defeatRestartButton  = FindButtonIn(defeatPanel.transform, "BtnRestart");
            if (defeatMenuButton == null)
                defeatMenuButton     = FindButtonIn(defeatPanel.transform, "BtnMenu");
            if (defeatStatsText == null)
                defeatStatsText      = FindTMPIn(defeatPanel.transform, "StatsText");
        }
    }

    // Szuka GameObject po nazwie włącznie z nieaktywnymi obiektami sceny
    static GameObject FindInScene(string name)
    {
        foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (t.name == name && t.gameObject.scene.IsValid())
                return t.gameObject;
        return null;
    }

    static Button FindButtonIn(Transform root, string name)
    {
        var t = FindDeep(root, name);
        return t != null ? t.GetComponent<Button>() : null;
    }

    static TextMeshProUGUI FindTMPIn(Transform root, string name)
    {
        var t = FindDeep(root, name);
        return t != null ? t.GetComponent<TextMeshProUGUI>() : null;
    }

    static Transform FindDeep(Transform root, string name)
    {
        foreach (Transform child in root)
        {
            if (child.name == name) return child;
            var found = FindDeep(child, name);
            if (found != null) return found;
        }
        return null;
    }

    void EnsureDecreeManager()
    {
        if (DecreeManager.Instance == null)
            new GameObject("DecreeManager").AddComponent<DecreeManager>();
    }

    // ── Wyzwalacze konca gry ──────────────────────────────────────────────

    public void TriggerVictory()
    {
        if (IsGameOver) return;
        IsGameOver = true;
        Time.timeScale = 0f;
        GameplayUIManager.Instance?.LockForGameOver();
        if (victoryStatsText != null) victoryStatsText.text = BuildStatsText(won: true);
        if (victoryPanel     != null) victoryPanel.SetActive(true);
    }

    public void TriggerDefeat()
    {
        if (IsGameOver) return;
        IsGameOver = true;
        Time.timeScale = 0f;
        GameplayUIManager.Instance?.LockForGameOver();
        if (defeatStatsText != null) defeatStatsText.text = BuildStatsText(won: false);
        if (defeatPanel     != null) defeatPanel.SetActive(true);
    }

    public void TriggerDraw()
    {
        if (IsGameOver) return;
        IsGameOver = true;
        Time.timeScale = 0f;
        GameplayUIManager.Instance?.LockForGameOver();
        if (drawStatsText != null) drawStatsText.text = BuildStatsText(won: false);
        if (drawPanel     != null) drawPanel.SetActive(true);
    }

    // ── Nawigacja ──────────────────────────────────────────────────────────

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        IsGameOver = false;
        LobbySettings.Reset(); // usuń złoto MP — następna sesja SP użyje GameConfig
        if (GameStatistics.Instance    != null) Destroy(GameStatistics.Instance.gameObject);
        if (GameplayUIManager.Instance != null) Destroy(GameplayUIManager.Instance.gameObject);
        if (DecreeManager.Instance     != null) Destroy(DecreeManager.Instance.gameObject);
        StartCoroutine(DisconnectAndLoad(0));
    }

    public void RestartScene()
    {
        Time.timeScale = 1f;
        IsGameOver = false;
        int scene = SceneManager.GetActiveScene().buildIndex;
        if (GameStatistics.Instance    != null) Destroy(GameStatistics.Instance.gameObject);
        if (GameplayUIManager.Instance != null) Destroy(GameplayUIManager.Instance.gameObject);
        if (DecreeManager.Instance     != null) Destroy(DecreeManager.Instance.gameObject);
        Destroy(gameObject);
        SceneManager.LoadScene(scene, LoadSceneMode.Single);
    }

    public void ReturnToLobby()
    {
        Time.timeScale = 1f;
        IsGameOver = false;
        LobbySettings.Reset(); // wyczyść złoto MP przed powrotem do lobby
        if (GameStatistics.Instance    != null) Destroy(GameStatistics.Instance.gameObject);
        if (GameplayUIManager.Instance != null) Destroy(GameplayUIManager.Instance.gameObject);
        if (DecreeManager.Instance     != null) Destroy(DecreeManager.Instance.gameObject);
        StartCoroutine(DisconnectAndLoad(0));
    }

    /// Zatrzymuje sieć Mirror (jeśli aktywna), niszczy NetworkManager,
    /// a następnie ładuje wskazaną scenę.
    ///
    /// Dlaczego niszczymy NetworkManager:
    ///   NetworkManager ma DontDestroyOnLoad i żyje przez cały czas działania gry.
    ///   Po wielu sesjach (SP + MP, wielokrotny restart) jego wewnętrzny stan
    ///   transportu i Mirror staje się nieaktualny. Zniszczenie go przed powrotem
    ///   do menu gwarantuje, że scena menu zainicjuje świeżą instancję z inspektora
    ///   — bez resztek poprzedniej sesji. Działa też po SP (brak sieci → niszczy
    ///   tylko obiekt, Mirror nie był aktywny).
    ///
    /// Dlaczego dwa yield return null:
    ///   Pierwszy daje transport czas na faktyczne zamknięcie socketu OS.
    ///   Drugi zapewnia że Destroy(nm) z pierwszego yield-a w pełni się przetworzy
    ///   zanim LoadScene ruszy.
    System.Collections.IEnumerator DisconnectAndLoad(int sceneIndex)
    {
        var nm = NetworkManager.singleton;
        if (nm != null)
        {
            nm.offlineScene = string.Empty; // blokuj Mirror-owy auto-load sceny
            nm.onlineScene  = string.Empty;

            if (NetworkServer.active)      nm.StopHost();
            else if (NetworkClient.active) nm.StopClient();

            yield return null; // transport zamyka socket

            Destroy(nm.gameObject); // usuń NetworkManager — menu stworzy świeżą instancję
        }

        yield return null; // klatka po zniszczeniu NM

        // LoadScene kolejkujemy PRZED Destroy — coroutine działa na tym gameObject,
        // więc zniszczenie go przed kolejkowaniem sceny mogłoby przerwać wykonanie.
        SceneManager.LoadScene(sceneIndex, LoadSceneMode.Single);
        Destroy(gameObject); // GameManager
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    static void WireIfEmpty(Button btn, UnityEngine.Events.UnityAction action)
    {
        if (btn != null && btn.onClick.GetPersistentEventCount() == 0)
            btn.onClick.AddListener(action);
    }

    // ── Statystyki ─────────────────────────────────────────────────────────

    string BuildStatsText(bool won)
    {
        var gs = GameStatistics.Instance;
        var sb = new StringBuilder();

        float dur   = gs != null ? gs.GetGameDuration() : 0f;
        int minutes = (int)(dur / 60f);
        int seconds = (int)(dur % 60f);

        sb.AppendLine(won
            ? "Ukonczone rundy: " + VICTORY_ROUND
            : "Przetrwane rundy: " + (gs != null ? gs.wavesSurvived : 0));
        sb.AppendLine("Czas gry: " + minutes + " min " + seconds + " sek");
        sb.AppendLine();

        sb.AppendLine("ZNISZCZONE WIEZY");
        if (gs == null || gs.destroyedTowers.Count == 0)
            sb.AppendLine("  Brak");
        else
            foreach (var kv in gs.destroyedTowers)
                sb.AppendLine("  " + kv.Key + ": " + kv.Value + " szt.");
        sb.AppendLine("  Razem: " + (gs != null ? gs.totalDestroyedTowers : 0));
        sb.AppendLine();

        sb.AppendLine("EKONOMIA");
        sb.AppendLine("  Wydane zloto: " + (gs != null ? gs.totalGoldSpent : 0));
        sb.AppendLine();

        int nalot  = gs != null ? gs.airstrikeUsed : 0;
        int tarcza = gs != null ? gs.shieldUsed    : 0;
        int boost  = gs != null ? gs.boostUsed     : 0;
        sb.AppendLine("MOCE TAKTYCZNE");
        sb.AppendLine("  Nalot: "       + nalot);
        sb.AppendLine("  Tarcza: "      + tarcza);
        sb.AppendLine("  Doladowanie: " + boost);
        sb.AppendLine("  Razem: "       + (nalot + tarcza + boost));
        sb.AppendLine();

        sb.AppendLine("POJAZDY");
        sb.AppendLine("  Zrekrutowanych: "     + (gs != null ? gs.totalDeployedUnits : 0));
        sb.AppendLine("  Ulubiona jednostka: " + (gs != null ? gs.GetFavoriteUnit()  : "Brak"));

        return sb.ToString();
    }
}
