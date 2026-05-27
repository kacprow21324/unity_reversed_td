using UnityEngine;
using UnityEngine.SceneManagement;

/// Utrzymuje FullMenuCanvas między przeładowaniami scen (DontDestroyOnLoad).
/// Canvas jest widoczny tylko gdy aktywna scena to "menu".
/// Dodaj ten komponent bezpośrednio do obiektu FullMenuCanvas w scenie menu.
public class FullMenuCanvas : MonoBehaviour
{
    public static FullMenuCanvas Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // Duplikat z przeładowania sceny — zniszcz nową kopię, oryginał DDOL działa.
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Przenieś do DontDestroyOnLoad — canvas i wszystkie jego dzieci przeżywają przeładowania.
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Widoczność dopasowana do aktualnej sceny (pierwsze uruchomienie).
        gameObject.SetActive(SceneManager.GetActiveScene().name == "menu");
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
        // Pokaż canvas tylko w scenie menu, ukryj przy każdej innej (gra SP/MP).
        gameObject.SetActive(scene.name == "menu");
    }
}
