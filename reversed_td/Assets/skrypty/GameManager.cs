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

    [Header("Przyciski SP")]
    public Button victoryRestartButton;
    public Button defeatRestartButton;

    [Header("Przyciski MP - Powrót do Lobby")]
    public Button victoryLobbyButton;
    public Button defeatLobbyButton;
    public Button drawLobbyButton;

    public bool IsGameOver { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        EnsureDecreeManager();
    }

    void Start()
    {
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (defeatPanel  != null) defeatPanel.SetActive(false);
        if (drawPanel    != null) drawPanel.SetActive(false);
        IsGameOver = false;

        if (GameStatistics.Instance != null)
            GameStatistics.Instance.gameStartTime = Time.realtimeSinceStartup;

        bool isMP = NetworkManager.singleton != null && NetworkManager.singleton.isNetworkActive;
        if (victoryRestartButton != null) victoryRestartButton.gameObject.SetActive(!isMP);
        if (defeatRestartButton  != null) defeatRestartButton.gameObject.SetActive(!isMP);
        if (victoryLobbyButton   != null) victoryLobbyButton.gameObject.SetActive(isMP);
        if (defeatLobbyButton    != null) defeatLobbyButton.gameObject.SetActive(isMP);
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

        if (victoryStatsText != null)
            victoryStatsText.text = BuildStatsText(won: true);

        if (victoryPanel != null)
            victoryPanel.SetActive(true);
    }

    public void TriggerDefeat()
    {
        if (IsGameOver) return;
        IsGameOver = true;
        Time.timeScale = 0f;

        GameplayUIManager.Instance?.LockForGameOver();

        if (defeatStatsText != null)
            defeatStatsText.text = BuildStatsText(won: false);

        if (defeatPanel != null)
            defeatPanel.SetActive(true);
    }

    public void TriggerDraw()
    {
        if (IsGameOver) return;
        IsGameOver = true;
        Time.timeScale = 0f;

        GameplayUIManager.Instance?.LockForGameOver();

        if (drawStatsText != null)
            drawStatsText.text = BuildStatsText(won: false);

        if (drawPanel != null)
            drawPanel.SetActive(true);
    }

    // ── Statystyki ─────────────────────────────────────────────────────────

    string BuildStatsText(bool won)
    {
        var gs = GameStatistics.Instance;
        var sb = new StringBuilder();

        float dur   = gs != null ? gs.GetGameDuration() : 0f;
        int minutes = (int)(dur / 60f);
        int seconds = (int)(dur % 60f);

        sb.AppendLine(won ? "Ukonczone rundy: " + VICTORY_ROUND : "Przetrwane rundy: " + (gs != null ? gs.wavesSurvived : 0));
        sb.AppendLine("Czas gry: " + minutes + " min " + seconds + " sek");
        sb.AppendLine();

        sb.AppendLine("ZNISZCZONE WIEZY");
        if (gs == null || gs.destroyedTowers.Count == 0)
        {
            sb.AppendLine("  Brak");
        }
        else
        {
            foreach (var kv in gs.destroyedTowers)
                sb.AppendLine("  " + kv.Key + ": " + kv.Value + " szt.");
        }
        sb.AppendLine("  Razem: " + (gs != null ? gs.totalDestroyedTowers : 0));
        sb.AppendLine();

        sb.AppendLine("EKONOMIA");
        sb.AppendLine("  Wydane zloto: " + (gs != null ? gs.totalGoldSpent : 0));
        sb.AppendLine();

        int nalot  = gs != null ? gs.airstrikeUsed : 0;
        int tarcza = gs != null ? gs.shieldUsed    : 0;
        int boost  = gs != null ? gs.boostUsed     : 0;
        sb.AppendLine("MOCE TAKTYCZNE");
        sb.AppendLine("  Nalot: " + nalot);
        sb.AppendLine("  Tarcza: " + tarcza);
        sb.AppendLine("  Doladowanie: " + boost);
        sb.AppendLine("  Razem: " + (nalot + tarcza + boost));
        sb.AppendLine();

        sb.AppendLine("POJAZDY");
        sb.AppendLine("  Zrekrutowanych: " + (gs != null ? gs.totalDeployedUnits : 0));
        sb.AppendLine("  Ulubiona jednostka: " + (gs != null ? gs.GetFavoriteUnit() : "Brak"));

        return sb.ToString();
    }

    // ── Nawigacja ──────────────────────────────────────────────────────────

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        IsGameOver = false;
        if (GameStatistics.Instance    != null) Destroy(GameStatistics.Instance.gameObject);
        if (GameplayUIManager.Instance != null) Destroy(GameplayUIManager.Instance.gameObject);
        if (DecreeManager.Instance     != null) Destroy(DecreeManager.Instance.gameObject);
        Destroy(gameObject);
        SceneManager.LoadScene(0, LoadSceneMode.Single);
    }

    public void RestartScene()
    {
        Time.timeScale = 1f;
        IsGameOver = false;
        int targetScene = SceneManager.GetActiveScene().buildIndex;
        if (GameStatistics.Instance    != null) Destroy(GameStatistics.Instance.gameObject);
        if (GameplayUIManager.Instance != null) Destroy(GameplayUIManager.Instance.gameObject);
        if (DecreeManager.Instance     != null) Destroy(DecreeManager.Instance.gameObject);
        Destroy(gameObject);
        SceneManager.LoadScene(targetScene, LoadSceneMode.Single);
    }

    public void ReturnToLobby()
    {
        Time.timeScale = 1f;
        IsGameOver = false;

        if (GameStatistics.Instance    != null) Destroy(GameStatistics.Instance.gameObject);
        if (GameplayUIManager.Instance != null) Destroy(GameplayUIManager.Instance.gameObject);
        if (DecreeManager.Instance     != null) Destroy(DecreeManager.Instance.gameObject);
        Destroy(gameObject);

        if (NetworkServer.active && NetworkClient.isConnected)
            NetworkManager.singleton.StopHost();
        else if (NetworkClient.isConnected)
            NetworkManager.singleton.StopClient();

        SceneManager.LoadScene(0, LoadSceneMode.Single);
    }
}
