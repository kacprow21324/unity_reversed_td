using UnityEngine;
using UnityEngine.SceneManagement;

/// Utrzymuje obiekt LobbyOverlay (i całe jego drzewo) między przeładowaniami scen.
/// Dodaj ten komponent bezpośrednio do GameObject "LobbyOverlay" w scenie menu.
public class LobbyOverlay : MonoBehaviour
{
    public static LobbyOverlay Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // Duplikat z przeładowania sceny — zniszcz go, oryginał DDOL nadal działa.
            Destroy(transform.root.gameObject);
            return;
        }

        Instance = this;

        // Cały root (LobbyOverlay + wszystkie dzieci) przeżywa przeładowania scen.
        DontDestroyOnLoad(transform.root.gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Ukryj od razu — pokazywany dopiero przez MultiplayerLobbyUI gdy potrzebny.
        transform.root.gameObject.SetActive(false);
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
        // Przy wejściu do sceny gry zawsze ukryj — bezpiecznik gdyby coś zostawiło go aktywnym.
        if (scene.name != "menu")
            transform.root.gameObject.SetActive(false);
    }

    // ── Publiczne API ──────────────────────────────────────────────────────

    public void Show() => transform.root.gameObject.SetActive(true);
    public void Hide() => transform.root.gameObject.SetActive(false);
}
