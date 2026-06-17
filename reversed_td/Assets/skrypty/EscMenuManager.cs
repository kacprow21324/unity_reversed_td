using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// Menu ESC dostępne podczas rozgrywki.
/// SP  → pauzuje czas; MP → czas biegnie dalej (Mirror wymaga).
/// Dodaj ten komponent do dowolnego GameObject w scenie gry.
public class EscMenuManager : MonoBehaviour
{
    public static EscMenuManager Instance { get; private set; }

    /// true kiedy ESC-menu LUB Ustawienia są otwarte — kamera to sprawdza.
    public static bool IsAnyMenuOpen =>
        (Instance != null && Instance._menuOpen) ||
        (SettingsManager.Instance != null && SettingsManager.Instance.IsOpen);

    // ── Stan ──────────────────────────────────────────────────────────────

    bool _menuOpen;
    bool _isMultiplayer;
    float _prevTimeScale;

    // ── UI ────────────────────────────────────────────────────────────────

    Canvas _canvas;
    GameObject _overlay;   // ciemne tło
    GameObject _panel;     // białe pudełko menu

    // ── Kolory ────────────────────────────────────────────────────────────

    static readonly Color C_OVERLAY = new Color(0f, 0f, 0f, 0.72f);
    static readonly Color C_PANEL = new Color(0.11f, 0.11f, 0.13f, 1f);
    static readonly Color C_SEP = new Color(0.28f, 0.28f, 0.34f, 1f);
    static readonly Color C_BTN = new Color(0.18f, 0.18f, 0.22f, 1f);
    static readonly Color C_BTN_H = new Color(0.26f, 0.26f, 0.32f, 1f);
    static readonly Color C_BTN_P = new Color(0.10f, 0.10f, 0.12f, 1f);
    static readonly Color C_BTN_QUIT = new Color(0.30f, 0.10f, 0.10f, 1f);
    static readonly Color C_BTN_QUIT_H = new Color(0.44f, 0.14f, 0.14f, 1f);
    static readonly Color C_TEXT = new Color(0.88f, 0.88f, 0.91f, 1f);
    static readonly Color C_TITLE = new Color(0.68f, 0.74f, 0.86f, 1f);

    // ─────────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        _isMultiplayer = SceneManager.GetActiveScene().name.Contains("Multiplayer");
        BuildUI();
        SetOverlayActive(false);
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Jeśli otwarte są Ustawienia — zamknij je najpierw
            if (SettingsManager.Instance != null && SettingsManager.Instance.IsOpen)
            {
                SettingsManager.Instance.Close();
                return;
            }
            ToggleMenu();
        }
    }

    // ── Public API ─────────────────────────────────────────────────────────

    public bool IsOpen => _menuOpen;

    /// Zamknij menu i przywróć czas (SP).
    public void CloseMenu()
    {
        _menuOpen = false;
        SetOverlayActive(false);
        if (!_isMultiplayer)
            Time.timeScale = _prevTimeScale;
    }

    /// Pokaż panel bez zmiany czasu (wracamy z Ustawień).
    public void ShowPanelOnly()
    {
        _menuOpen = true;
        SetOverlayActive(true);
        if (!_isMultiplayer)
            Time.timeScale = 0f;
    }

    // ── Przełącznik ────────────────────────────────────────────────────────

    void ToggleMenu()
    {
        if (_menuOpen) { CloseMenu(); return; }

        _prevTimeScale = Time.timeScale;
        _menuOpen = true;
        SetOverlayActive(true);
        if (!_isMultiplayer) Time.timeScale = 0f;
    }

    void SetOverlayActive(bool v) => _overlay?.SetActive(v);

    // ── Akcje przycisków ───────────────────────────────────────────────────

    void OnResume() => CloseMenu();

    void OnSettings()
    {
        // Ukryj panel ESC, ale zachowaj pauzę — Ustawienia "przejmują" pauzę.
        _menuOpen = false;
        SetOverlayActive(false);
        SettingsManager.Instance?.OpenFromGame();
    }

    void OnQuitToMenu()
    {
        if (_isMultiplayer)
            ForfeitMultiplayer();
        else
        {
            CloseMenu();
            Time.timeScale = 1f;
            // TriggerDefeat pokazuje panel porażki z przyciskiem "Wyjście do menu".
            // Gracz sam klika — brak ukrytego timera 2.5s.
            // Nawigacja do menu odbywa się przez GameManager.GoToMainMenu() na przycisku.
            GameManager.Instance?.TriggerDefeat();
        }
    }

    void ForfeitMultiplayer()
    {
        CloseMenu();
        NetworkClient.localPlayer?.GetComponent<NetworkPlayer>()?.CmdForfeit();
    }

    // ── Budowanie UI ───────────────────────────────────────────────────────

    void BuildUI()
    {
        // Własny Canvas — wyższy sortOrder niż gameplay UI
        var cGO = new GameObject("EscMenuCanvas");
        cGO.transform.SetParent(transform, false);
        _canvas = cGO.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 100;

        var scaler = cGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        cGO.AddComponent<GraphicRaycaster>();

        // Overlay — blokuje kliknięcia w tło
        _overlay = UIHelper.MakeImage("Overlay", cGO.transform, C_OVERLAY);
        UIHelper.Stretch(_overlay.GetComponent<RectTransform>());

        // Panel
        _panel = UIHelper.MakeImage("EscPanel", _overlay.transform, C_PANEL);
        var pRT = _panel.GetComponent<RectTransform>();
        pRT.anchorMin = pRT.anchorMax = pRT.pivot = new Vector2(0.5f, 0.5f);
        pRT.sizeDelta = new Vector2(340f, 300f);

        var outline = _panel.AddComponent<UnityEngine.UI.Outline>();
        outline.effectColor = C_SEP;
        outline.effectDistance = new Vector2(1f, -1f);

        // Tytuł
        var titleRT = UIHelper.MakeText("Title", _panel.transform, "PAUZA", 22f, C_TITLE, FontStyles.Bold)
                              .GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0f, 1f);
        titleRT.anchorMax = new Vector2(1f, 1f);
        titleRT.pivot = new Vector2(0.5f, 1f);
        titleRT.anchoredPosition = new Vector2(0f, -22f);
        titleRT.sizeDelta = new Vector2(0f, 36f);

        // Separator
        var sepRT = UIHelper.MakeImage("Sep", _panel.transform, C_SEP).GetComponent<RectTransform>();
        sepRT.anchorMin = new Vector2(0.08f, 1f);
        sepRT.anchorMax = new Vector2(0.92f, 1f);
        sepRT.pivot = new Vector2(0.5f, 1f);
        sepRT.anchoredPosition = new Vector2(0f, -62f);
        sepRT.sizeDelta = new Vector2(0f, 2f);

        // Kontener przycisków
        var bc = new GameObject("Buttons");
        bc.transform.SetParent(_panel.transform, false);
        var bcRT = bc.AddComponent<RectTransform>();
        bcRT.anchorMin = new Vector2(0.5f, 0.5f);
        bcRT.anchorMax = new Vector2(0.5f, 0.5f);
        bcRT.pivot = new Vector2(0.5f, 0.5f);
        bcRT.anchoredPosition = new Vector2(0f, -22f);
        bcRT.sizeDelta = new Vector2(264f, 190f);

        var vlg = bc.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 12f;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;

        UIHelper.MakeButton(bc.transform, "Wznów grę", C_BTN, C_BTN_H, C_BTN_P, C_TEXT, OnResume);
        UIHelper.MakeButton(bc.transform, "Ustawienia", C_BTN, C_BTN_H, C_BTN_P, C_TEXT, OnSettings);
        UIHelper.MakeButton(bc.transform, "Wyjście do menu", C_BTN_QUIT, C_BTN_QUIT_H, C_BTN_P, C_TEXT, OnQuitToMenu);
    }
}