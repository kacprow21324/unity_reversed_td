using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// Panel Ustawień — dźwięk, grafika, sterowanie (rebinding).
/// DontDestroyOnLoad: persystuje przez sceny, stosuje ustawienia przy każdym załadowaniu.
/// Wywołaj OpenFromMainMenu() lub OpenFromGame().
public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    public bool IsOpen => _panel != null && _panel.activeSelf;

    // ── Skąd otwarto panel ────────────────────────────────────────────────

    enum Caller { MainMenu, Game }
    Caller _caller;

    // ── Wartości ustawień ─────────────────────────────────────────────────

    float _musicVol = 1f;
    float _sfxVol = 1f;
    float _brightness = 0f;
    bool _fullscreen;
    int _resIndex = -1;

    Resolution[] _resolutions;

    // ── Snapshot — stan przy otwarciu (Powrót przywraca) ──────────────────
    float _snapMusicVol;
    float _snapSfxVol;
    float _snapBrightness;
    bool  _snapFullscreen;
    int   _snapResIndex;
    Dictionary<string, KeyCode> _snapBindings;

    // ── UI refs ───────────────────────────────────────────────────────────

    Canvas _canvas;
    GameObject _panel;
    GameObject _overlay;   // ciemne tło (blokuje kliknięcia w tło)

    // Zakładki
    readonly string[] _tabNames = { "DŹWIĘK", "GRAFIKA", "STEROWANIE" };
    readonly Dictionary<string, Button> _tabBtns = new Dictionary<string, Button>();
    readonly Dictionary<string, GameObject> _tabContents = new Dictionary<string, GameObject>();
    string _activeTab;

    // Dźwięk
    Slider _musicSlider;
    Slider _sfxSlider;

    // Grafika
    TMP_Dropdown _resDropdown;
    Slider _brightnessSlider;
    Toggle _fullscreenToggle;

    // Sterowanie — lista (akcja → przycisk z etykietą)
    readonly Dictionary<string, TextMeshProUGUI> _bindLabels = new Dictionary<string, TextMeshProUGUI>();
    Coroutine _rebindCoroutine;
    string _rebindingAction;

    // Nakładka jasności — DontDestroyOnLoad
    Image _brightnessOverlay;

    // ── Kolory ────────────────────────────────────────────────────────────

    static readonly Color C_BG = new Color(0.08f, 0.08f, 0.10f, 0.96f);
    static readonly Color C_PANEL = new Color(0.12f, 0.12f, 0.15f, 1f);
    static readonly Color C_PANEL2 = new Color(0.16f, 0.16f, 0.20f, 1f);
    static readonly Color C_SEP = new Color(0.28f, 0.28f, 0.34f, 1f);
    static readonly Color C_TAB_ON = new Color(0.24f, 0.28f, 0.38f, 1f);
    static readonly Color C_TAB_OFF = new Color(0.15f, 0.15f, 0.19f, 1f);
    static readonly Color C_TAB_H = new Color(0.20f, 0.22f, 0.30f, 1f);
    static readonly Color C_BTN = new Color(0.20f, 0.20f, 0.24f, 1f);
    static readonly Color C_BTN_H = new Color(0.28f, 0.28f, 0.34f, 1f);
    static readonly Color C_BTN_P = new Color(0.10f, 0.10f, 0.12f, 1f);
    static readonly Color C_BIND_BTN = new Color(0.18f, 0.22f, 0.30f, 1f);
    static readonly Color C_BIND_ACT = new Color(0.55f, 0.30f, 0.12f, 1f);
    static readonly Color C_SAVE = new Color(0.15f, 0.30f, 0.18f, 1f);
    static readonly Color C_SAVE_H = new Color(0.20f, 0.42f, 0.24f, 1f);
    static readonly Color C_TEXT = new Color(0.88f, 0.88f, 0.91f, 1f);
    static readonly Color C_TEXT_DIM = new Color(0.62f, 0.62f, 0.66f, 1f);
    static readonly Color C_TITLE = new Color(0.68f, 0.74f, 0.86f, 1f);
    static readonly Color C_SLIDER_BG = new Color(0.20f, 0.20f, 0.24f, 1f);
    static readonly Color C_SLIDER_FILL = new Color(0.35f, 0.50f, 0.72f, 1f);

    // ─────────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // Niszczymy tylko KOMPONENT — nie cały GameObject.
            // Gdyby SettingsManager siedział na Canvas menu, Destroy(gameObject) zniszczyłby
            // cały Canvas wraz z multiPanel, lobbyPanel itd. → "Missing Game Object" w lobby.
            Destroy(this);
            return;
        }
        Instance = this;
        // Odepnij od rodzica zanim pójdziemy do DDOL — Canvas (ani żaden inny rodzic)
        // nie powinien trafiać do DontDestroyOnLoad razem z SettingsManagerem.
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (Instance != this) return; // duplikat zniszczony w Awake — nie twórz drugiego canvas DDOL
        BuildBrightnessOverlay();
        LoadAndApply();
        BuildUI();
        _panel.SetActive(false);
        _overlay.SetActive(false);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        // Zamknij panel przy zmianie sceny
        if (IsOpen) { _panel.SetActive(false); _overlay.SetActive(false); }
        ApplyBrightness(_brightness);
    }

    // ── Public API ─────────────────────────────────────────────────────────

    public void OpenFromMainMenu()
    {
        _caller = Caller.MainMenu;
        Open();
    }

    public void OpenFromGame()
    {
        _caller = Caller.Game;
        Open();
        // SP: upewnij się, że czas jest zatrzymany
        if (!SceneManager.GetActiveScene().name.Contains("Multiplayer"))
            Time.timeScale = 0f;
    }

    // Powrót — odrzuca zmiany i przywraca stan sprzed otwarcia
    public void Close()
    {
        if (_rebindCoroutine != null) { StopCoroutine(_rebindCoroutine); _rebindCoroutine = null; _rebindingAction = null; RefreshBindLabels(); }
        RestoreSnapshot();
        ClosePanel();
    }

    // ── Wewnętrzne ─────────────────────────────────────────────────────────

    void Open()
    {
        TakeSnapshot();
        SyncUIFromValues();
        _panel.SetActive(true);
        _overlay.SetActive(true);
        ShowTab(_tabNames[0]);
    }

    void ClosePanel()
    {
        _panel.SetActive(false);
        _overlay.SetActive(false);

        if (_caller == Caller.Game)
            EscMenuManager.Instance?.ShowPanelOnly();
        else
            ShowMainMenuPanel(true);
    }

    void TakeSnapshot()
    {
        _snapMusicVol   = _musicVol;
        _snapSfxVol     = _sfxVol;
        _snapBrightness = _brightness;
        _snapFullscreen = _fullscreen;
        _snapResIndex   = _resIndex;
        _snapBindings   = new Dictionary<string, KeyCode>();
        foreach (var a in InputBindings.AllActions)
            _snapBindings[a] = InputBindings.Get(a);
    }

    void RestoreSnapshot()
    {
        _musicVol   = _snapMusicVol;
        _sfxVol     = _snapSfxVol;
        _brightness = _snapBrightness;
        _fullscreen = _snapFullscreen;
        _resIndex   = _snapResIndex;

        if (_snapBindings != null)
            foreach (var kv in _snapBindings)
                InputBindings.Set(kv.Key, kv.Value);

        ApplyAudio();
        ApplyBrightness(_brightness);
        SyncUIFromValues();
    }

    void ShowMainMenuPanel(bool show)
    {
        var mm = FindFirstObjectByType<MainMenuLogic>();
        if (mm == null) return;
        if (show) mm.OnClickBack();
    }

    // ── Załaduj / Zastosuj ─────────────────────────────────────────────────

    void LoadAndApply()
    {
        _musicVol = PlayerPrefs.GetFloat("MusicVol", 1f);
        _sfxVol = PlayerPrefs.GetFloat("SFXVol", 1f);
        _brightness = PlayerPrefs.GetFloat("Brightness", 0f);
        _fullscreen = PlayerPrefs.GetInt("Fullscreen", Screen.fullScreen ? 1 : 0) == 1;
        _resIndex = PlayerPrefs.GetInt("ResIndex", -1);

        InputBindings.Load();
        ApplyAudio();
        ApplyBrightness(_brightness);
        if (_resIndex >= 0) ApplyResolution(_resIndex);
    }

    void ApplyAudio()
    {
        AudioListener.volume = Mathf.Max(_musicVol, _sfxVol);
        MusicManager.Instance?.UstawGlosnosc(_musicVol);
    }

    void ApplyBrightness(float v)
    {
        _brightness = Mathf.Clamp01(v);
        if (_brightnessOverlay != null)
            _brightnessOverlay.color = new Color(0f, 0f, 0f, _brightness * 0.85f);
    }

    void ApplyResolution(int idx)
    {
        if (_resolutions == null || idx < 0 || idx >= _resolutions.Length) return;
        var r = _resolutions[idx];
        Screen.SetResolution(r.width, r.height, Screen.fullScreenMode, r.refreshRateRatio);
    }

    void SaveSettings()
    {
        PlayerPrefs.SetFloat("MusicVol", _musicVol);
        PlayerPrefs.SetFloat("SFXVol", _sfxVol);
        PlayerPrefs.SetFloat("Brightness", _brightness);
        PlayerPrefs.SetInt("Fullscreen", _fullscreen ? 1 : 0);
        PlayerPrefs.SetInt("ResIndex", _resIndex);
        PlayerPrefs.Save();
        ApplyAudio();
        Screen.fullScreen = _fullscreen;
        if (_resIndex >= 0) ApplyResolution(_resIndex);
    }

    // ── Sync UI → wartości i odwrotnie ────────────────────────────────────

    void SyncUIFromValues()
    {
        if (_musicSlider != null) _musicSlider.value = _musicVol;
        if (_sfxSlider != null) _sfxSlider.value = _sfxVol;
        if (_brightnessSlider != null) _brightnessSlider.value = _brightness;
        if (_fullscreenToggle != null) _fullscreenToggle.isOn = _fullscreen;
        if (_resDropdown != null && _resIndex >= 0) _resDropdown.value = _resIndex;
        RefreshBindLabels();
    }

    void RefreshBindLabels()
    {
        foreach (var kv in _bindLabels)
        {
            if (kv.Key == _rebindingAction)
                kv.Value.text = "Wciśnij klawisz...";
            else
                kv.Value.text = InputBindings.KeyName(InputBindings.Get(kv.Key));
        }
    }

    // ── Rebinding ─────────────────────────────────────────────────────────

    void StartRebind(string action)
    {
        if (_rebindCoroutine != null) StopCoroutine(_rebindCoroutine);
        _rebindingAction = action;
        RefreshBindLabels();
        _rebindCoroutine = StartCoroutine(CaptureKey(action));
    }

    IEnumerator CaptureKey(string action)
    {
        yield return new WaitForSecondsRealtime(0.12f);

        while (true)
        {
            if (Input.anyKeyDown)
            {
                foreach (KeyCode kc in System.Enum.GetValues(typeof(KeyCode)))
                {
                    if (!Input.GetKeyDown(kc)) continue;
                    if (kc == KeyCode.Escape) break; // anuluj
                    InputBindings.Set(action, kc);
                    break;
                }
                break;
            }
            yield return null;
        }

        _rebindCoroutine = null;
        _rebindingAction = null;
        RefreshBindLabels();
    }

    // ══ BUDOWANIE UI ═══════════════════════════════════════════════════════

    static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;
        var esGO = new GameObject("EventSystem");
        DontDestroyOnLoad(esGO);
        esGO.AddComponent<EventSystem>();
        esGO.AddComponent<StandaloneInputModule>();
    }

    void BuildUI()
    {
        EnsureEventSystem();
        if (TryAdoptPrebuiltUI()) return;

        var cGO = new GameObject("SettingsCanvas");
        DontDestroyOnLoad(cGO);
        _canvas = cGO.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 200;

        var sc = cGO.AddComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920f, 1080f);
        sc.matchWidthOrHeight = 0.45f;

        cGO.AddComponent<GraphicRaycaster>();

        // Tło — blokuje kliknięcia w grę
        _overlay = UIHelper.MakeImage("SettingsOverlay", cGO.transform, C_BG);
        UIHelper.Stretch(_overlay.GetComponent<RectTransform>());

        // Panel główny — responsywny, 90% szerokości × 88% wysokości ekranu
        _panel = UIHelper.MakeImage("SettingsPanel", _overlay.transform, C_PANEL);
        var pRT = _panel.GetComponent<RectTransform>();
        pRT.anchorMin = new Vector2(0.05f, 0.06f);
        pRT.anchorMax = new Vector2(0.95f, 0.94f);
        pRT.pivot     = new Vector2(0.5f, 0.5f);
        pRT.offsetMin = pRT.offsetMax = Vector2.zero;

        BuildHeader();
        BuildTabBar();
        BuildContentArea();
        BuildFooter();
    }

    // ── Przejmowanie Canvas wygenerowanego przez narzędzie edytora ─────────

    bool TryAdoptPrebuiltUI()
    {
        var canvasGO = GameObject.Find("SettingsCanvas");
        if (canvasGO == null) return false;

        var canvas = canvasGO.GetComponent<Canvas>();
        if (canvas == null) return false;

        _canvas = canvas;
        DontDestroyOnLoad(canvasGO);

        _overlay = canvasGO.transform.Find("SettingsOverlay")?.gameObject;
        if (_overlay == null) return false;

        _panel = _overlay.transform.Find("SettingsPanel")?.gameObject;
        if (_panel == null) return false;

        // Zakładki — przyciski (nazwy GO muszą być ASCII — patrz UIGeneratorEditor.TAB_IDS)
        string[] tabIds = { "Sound", "Graphics", "Controls" };
        var tabBar = _panel.transform.Find("TabBar");
        if (tabBar != null)
        {
            for (int i = 0; i < _tabNames.Length; i++)
            {
                var btn = tabBar.Find("Btn_" + tabIds[i])?.GetComponent<Button>();
                if (btn == null) continue;
                _tabBtns[_tabNames[i]] = btn;
                string cap = _tabNames[i];
                btn.onClick.AddListener(() => ShowTab(cap));
            }
        }

        var area = _panel.transform.Find("ContentArea");

        // Zakładka DŹWIĘK
        var soundTab = area?.Find("SoundTab");
        if (soundTab != null)
        {
            _tabContents[_tabNames[0]] = soundTab.gameObject;
            _musicSlider = soundTab.Find("Row_MusicVol/Slider")?.GetComponent<Slider>();
            _sfxSlider   = soundTab.Find("Row_SFXVol/Slider")?.GetComponent<Slider>();
            if (_musicSlider != null) _musicSlider.onValueChanged.AddListener(v => { _musicVol = v; ApplyAudio(); });
            if (_sfxSlider   != null) _sfxSlider.onValueChanged.AddListener(v   => { _sfxVol   = v; ApplyAudio(); });
        }

        // Zakładka GRAFIKA
        var graphicsTab = area?.Find("GraphicsTab");
        if (graphicsTab != null)
        {
            _tabContents[_tabNames[1]] = graphicsTab.gameObject;
            _resDropdown      = graphicsTab.Find("RowRes/ResDropdown")?.GetComponent<TMP_Dropdown>();
            _fullscreenToggle = graphicsTab.Find("Row_Fullscreen/Toggle")?.GetComponent<Toggle>();
            _brightnessSlider = graphicsTab.Find("Row_Brightness/Slider")?.GetComponent<Slider>();
            if (_resDropdown      != null) AdoptAndPopulateDropdown();
            if (_fullscreenToggle != null) _fullscreenToggle.onValueChanged.AddListener(v => _fullscreen = v);
            if (_brightnessSlider != null) _brightnessSlider.onValueChanged.AddListener(v => ApplyBrightness(v));
        }

        // Zakładka STEROWANIE
        var controlsTab = area?.Find("ControlsTab");
        if (controlsTab != null)
        {
            _tabContents[_tabNames[2]] = controlsTab.gameObject;
            var content = controlsTab.Find("ControlsScroll/Viewport/Content");
            _bindLabels.Clear();
            if (content != null)
            {
                foreach (var action in InputBindings.AllActions)
                {
                    string cap = action;
                    var keyLbl  = content.Find("Row_" + cap + "/BindBtn/KeyLabel")?.GetComponent<TextMeshProUGUI>();
                    if (keyLbl != null) _bindLabels[cap] = keyLbl;
                    var bindBtn = content.Find("Row_" + cap + "/BindBtn")?.GetComponent<Button>();
                    if (bindBtn != null)
                        bindBtn.onClick.AddListener(() =>
                        {
                            if (_rebindingAction == cap) return;
                            if (_rebindCoroutine != null)
                            {
                                StopCoroutine(_rebindCoroutine);
                                _rebindCoroutine = null; _rebindingAction = null;
                                RefreshBindLabels();
                            }
                            foreach (var kv in _bindLabels)
                            {
                                if (kv.Value.transform.parent.TryGetComponent<Image>(out var img))
                                    img.color = kv.Key == cap ? C_BIND_ACT : C_BIND_BTN;
                            }
                            StartRebind(cap);
                        });
                }
            }
            controlsTab.Find("ResetBtn")?.GetComponent<Button>()
                ?.onClick.AddListener(() => { InputBindings.ResetAll(); RefreshBindLabels(); });
        }

        // Stopka
        var footer = _panel.transform.Find("Footer");
        footer?.Find("Btn_Zapisz")?.GetComponent<Button>()?.onClick.AddListener(OnSave);
        footer?.Find("Btn_Back")?.GetComponent<Button>()?.onClick.AddListener(Close);

        return true;
    }

    void AdoptAndPopulateDropdown()
    {
        _resolutions = Screen.resolutions;
        _resDropdown.ClearOptions();
        var opts = new List<TMP_Dropdown.OptionData>();
        int currentIdx = 0;
        for (int i = 0; i < _resolutions.Length; i++)
        {
            var r = _resolutions[i];
            opts.Add(new TMP_Dropdown.OptionData($"{r.width}×{r.height} {(int)r.refreshRateRatio.value}Hz"));
            if (r.width == Screen.currentResolution.width && r.height == Screen.currentResolution.height)
                currentIdx = i;
        }
        _resDropdown.AddOptions(opts);
        if (_resIndex < 0) _resIndex = currentIdx;
        _resDropdown.value = _resIndex;
        _resDropdown.onValueChanged.AddListener(v => _resIndex = v);
    }

    void BuildHeader()
    {
        var hRT = UIHelper.MakeText("Title", _panel.transform, "USTAWIENIA", 24f, C_TITLE, FontStyles.Bold)
                          .GetComponent<RectTransform>();
        hRT.anchorMin = new Vector2(0f, 1f);
        hRT.anchorMax = new Vector2(1f, 1f);
        hRT.pivot = new Vector2(0.5f, 1f);
        hRT.anchoredPosition = new Vector2(0f, -20f);
        hRT.sizeDelta = new Vector2(0f, 42f);

        var sepRT = UIHelper.MakeImage("HSep", _panel.transform, C_SEP).GetComponent<RectTransform>();
        sepRT.anchorMin = new Vector2(0.04f, 1f);
        sepRT.anchorMax = new Vector2(0.96f, 1f);
        sepRT.pivot = new Vector2(0.5f, 1f);
        sepRT.anchoredPosition = new Vector2(0f, -64f);
        sepRT.sizeDelta = new Vector2(0f, 2f);
    }

    void BuildTabBar()
    {
        var bar = new GameObject("TabBar");
        bar.transform.SetParent(_panel.transform, false);
        var barRT = bar.AddComponent<RectTransform>();
        barRT.anchorMin = new Vector2(0.04f, 1f);
        barRT.anchorMax = new Vector2(0.96f, 1f);
        barRT.pivot = new Vector2(0f, 1f);
        barRT.anchoredPosition = new Vector2(0f, -68f);
        barRT.sizeDelta = new Vector2(0f, 40f);

        var hlg = bar.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 6f;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;

        foreach (var tab in _tabNames)
        {
            string captured = tab;
            var btn = UIHelper.MakeButton(bar.transform, tab, C_TAB_OFF, C_TAB_H, C_BTN_P, C_TEXT,
                () => ShowTab(captured), height: 40f);
            _tabBtns[tab] = btn;
        }
    }

    void BuildContentArea()
    {
        // Wspólny kontener treści zakładek
        var area = new GameObject("ContentArea");
        area.transform.SetParent(_panel.transform, false);
        var aRT = area.AddComponent<RectTransform>();
        aRT.anchorMin = new Vector2(0.04f, 0.12f);
        aRT.anchorMax = new Vector2(0.96f, 0.82f);
        aRT.offsetMin = Vector2.zero;
        aRT.offsetMax = Vector2.zero;

        BuildSoundTab(area.transform);
        BuildGraphicsTab(area.transform);
        BuildControlsTab(area.transform);
    }

    // ── ZAKŁADKA: DŹWIĘK ──────────────────────────────────────────────────

    void BuildSoundTab(Transform parent)
    {
        var p = MakeTabPanel("SoundTab", parent);
        _tabContents["DŹWIĘK"] = p;

        var vlg = p.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 20f;
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.padding = new RectOffset(0, 0, 10, 0);

        _musicSlider = AddSliderRow(p.transform, "Głośność muzyki", 0f, 1f, _musicVol,
            v => { _musicVol = v; ApplyAudio(); });

        _sfxSlider = AddSliderRow(p.transform, "Głośność efektów", 0f, 1f, _sfxVol,
            v => { _sfxVol = v; ApplyAudio(); });

    }

    // ── ZAKŁADKA: GRAFIKA ─────────────────────────────────────────────────

    void BuildGraphicsTab(Transform parent)
    {
        var p = MakeTabPanel("GraphicsTab", parent);
        _tabContents["GRAFIKA"] = p;

        var vlg = p.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 20f;
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.padding = new RectOffset(0, 0, 10, 0);

        // Rozdzielczość
        var rowRes = UIHelper.MakeRow("RowRes", p.transform, 56f);
        AddRowLabel(rowRes.transform, "Rozdzielczość");
        _resDropdown = BuildResDropdown(rowRes.transform);

        // Pełny ekran
        _fullscreenToggle = AddToggleRow(p.transform, "Pełny ekran", _fullscreen, v => _fullscreen = v);

        // Jasność
        _brightnessSlider = AddSliderRow(p.transform, "Przyciemnienie ekranu", 0f, 1f, _brightness,
            v => ApplyBrightness(v));

    }

    TMP_Dropdown BuildResDropdown(Transform parent)
    {
        var go = new GameObject("ResDropdown");
        go.transform.SetParent(parent, false);

        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = 280f;
        le.preferredHeight = 44f;
        le.flexibleWidth = 1f;

        var img = go.AddComponent<Image>();
        img.color = C_PANEL2;

        var dd = go.AddComponent<TMP_Dropdown>();
        dd.targetGraphic = img;

        // Label
        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(go.transform, false);
        var lRT = labelGO.AddComponent<RectTransform>();
        lRT.anchorMin = new Vector2(0.05f, 0f);
        lRT.anchorMax = new Vector2(0.85f, 1f);
        lRT.offsetMin = lRT.offsetMax = Vector2.zero;
        var lTMP = labelGO.AddComponent<TextMeshProUGUI>();
        lTMP.color = C_TEXT;
        lTMP.fontSize = 17f;
        lTMP.alignment = TextAlignmentOptions.MidlineLeft;
        dd.captionText = lTMP;

        // Arrow placeholder
        var arrowGO = new GameObject("Arrow");
        arrowGO.transform.SetParent(go.transform, false);
        var aRT = arrowGO.AddComponent<RectTransform>();
        aRT.anchorMin = new Vector2(0.85f, 0.1f);
        aRT.anchorMax = new Vector2(1f, 0.9f);
        aRT.offsetMin = aRT.offsetMax = Vector2.zero;
        var aTMP = arrowGO.AddComponent<TextMeshProUGUI>();
        aTMP.text = "▼";
        aTMP.color = C_TEXT_DIM;
        aTMP.fontSize = 15f;
        aTMP.alignment = TextAlignmentOptions.Center;

        // Template (wymagane przez TMP_Dropdown)
        var tGO = new GameObject("Template");
        tGO.transform.SetParent(go.transform, false);
        tGO.SetActive(false);
        var tImg = tGO.AddComponent<Image>();
        tImg.color = C_PANEL;
        var tRT = tGO.GetComponent<RectTransform>();
        tRT.anchorMin = new Vector2(0f, 0f);
        tRT.anchorMax = new Vector2(1f, 0f);
        tRT.pivot = new Vector2(0.5f, 1f);
        tRT.anchoredPosition = new Vector2(0f, 2f);
        tRT.sizeDelta = new Vector2(0f, 150f);

        var scrollGO = new GameObject("Scroll");
        scrollGO.transform.SetParent(tGO.transform, false);
        var scrollRect = scrollGO.AddComponent<ScrollRect>();
        UIHelper.Stretch(scrollGO.GetComponent<RectTransform>());

        var viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollGO.transform, false);
        UIHelper.Stretch(viewport.AddComponent<RectTransform>());
        viewport.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f);
        viewport.AddComponent<Mask>().showMaskGraphic = false;
        scrollRect.viewport = viewport.GetComponent<RectTransform>();

        var content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        var contentRT = content.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.pivot = new Vector2(0.5f, 1f);
        contentRT.sizeDelta = new Vector2(0f, 28f);
        scrollRect.content = contentRT;
        var cvlg = content.AddComponent<VerticalLayoutGroup>();
        cvlg.childForceExpandWidth = true;
        cvlg.childControlWidth = true;
        cvlg.childControlHeight = true;

        // Item template
        var itemGO = new GameObject("Item");
        itemGO.transform.SetParent(content.transform, false);
        itemGO.AddComponent<Image>().color = C_PANEL2;
        var itemLE = itemGO.AddComponent<LayoutElement>();
        itemLE.minHeight = 28f;
        var itemToggle = itemGO.AddComponent<Toggle>();
        itemToggle.targetGraphic = itemGO.GetComponent<Image>();
        var itemLabelGO = new GameObject("ItemLabel");
        itemLabelGO.transform.SetParent(itemGO.transform, false);
        UIHelper.Stretch(itemLabelGO.AddComponent<RectTransform>());
        var itemTMP = itemLabelGO.AddComponent<TextMeshProUGUI>();
        itemTMP.color = C_TEXT;
        itemTMP.fontSize = 13f;
        itemTMP.alignment = TextAlignmentOptions.MidlineLeft;
        var itemLabelRT = itemLabelGO.GetComponent<RectTransform>();
        itemLabelRT.offsetMin = new Vector2(8f, 0f);
        dd.itemText = itemTMP;
        itemToggle.graphic = itemGO.GetComponent<Image>();

        dd.template = tGO.GetComponent<RectTransform>();

        // Wypełnij opcjami
        _resolutions = Screen.resolutions;
        dd.ClearOptions();
        var opts = new List<TMP_Dropdown.OptionData>();
        int currentIdx = 0;
        for (int i = 0; i < _resolutions.Length; i++)
        {
            var r = _resolutions[i];
            opts.Add(new TMP_Dropdown.OptionData($"{r.width}×{r.height} {(int)r.refreshRateRatio.value}Hz"));
            if (r.width == Screen.currentResolution.width && r.height == Screen.currentResolution.height)
                currentIdx = i;
        }
        dd.AddOptions(opts);
        if (_resIndex < 0) _resIndex = currentIdx;
        dd.value = _resIndex;
        dd.onValueChanged.AddListener(v => _resIndex = v);

        return dd;
    }

    // ── ZAKŁADKA: STEROWANIE ──────────────────────────────────────────────

    void BuildControlsTab(Transform parent)
    {
        var p = MakeTabPanel("ControlsTab", parent);
        _tabContents["STEROWANIE"] = p;

        // Przycisk reset — pasek na dole zakładki, poza obszarem scroll
        var resetGO = new GameObject("ResetBtn");
        resetGO.transform.SetParent(p.transform, false);
        var rRT = resetGO.AddComponent<RectTransform>();
        rRT.anchorMin = new Vector2(0.15f, 0f);
        rRT.anchorMax = new Vector2(0.85f, 0f);
        rRT.pivot = new Vector2(0.5f, 0f);
        rRT.anchoredPosition = new Vector2(0f, 4f);
        rRT.sizeDelta = new Vector2(0f, 32f);
        var rImg = resetGO.AddComponent<Image>();
        rImg.color = C_BTN;
        var rBtn = resetGO.AddComponent<Button>();
        rBtn.targetGraphic = rImg;
        var rc = rBtn.colors;
        rc.normalColor = C_BTN; rc.highlightedColor = C_BTN_H; rc.pressedColor = C_BTN_P;
        rBtn.colors = rc;
        rBtn.onClick.AddListener(() => { InputBindings.ResetAll(); RefreshBindLabels(); });
        var rLblGO = new GameObject("Label");
        rLblGO.transform.SetParent(resetGO.transform, false);
        UIHelper.Stretch(rLblGO.AddComponent<RectTransform>());
        var rTMP = rLblGO.AddComponent<TextMeshProUGUI>();
        rTMP.text = "Przywróć domyślne"; rTMP.fontSize = 13f; rTMP.color = C_TEXT;
        rTMP.fontStyle = FontStyles.Bold; rTMP.alignment = TextAlignmentOptions.Center;
        rTMP.raycastTarget = false;

        // ScrollRect — zostawia dolny pasek dla przycisku reset
        var scrollGO = new GameObject("ControlsScroll");
        scrollGO.transform.SetParent(p.transform, false);
        var scrollRT = scrollGO.AddComponent<RectTransform>();
        scrollRT.anchorMin = Vector2.zero;
        scrollRT.anchorMax = Vector2.one;
        scrollRT.offsetMin = new Vector2(0f, 42f);
        scrollRT.offsetMax = Vector2.zero;
        var scroll = scrollGO.AddComponent<ScrollRect>();
        scroll.horizontal = false;

        var viewGO = new GameObject("Viewport");
        viewGO.transform.SetParent(scrollGO.transform, false);
        UIHelper.Stretch(viewGO.AddComponent<RectTransform>());
        viewGO.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f);
        viewGO.AddComponent<Mask>().showMaskGraphic = false;
        scroll.viewport = viewGO.GetComponent<RectTransform>();

        var contentGO = new GameObject("Content");
        contentGO.transform.SetParent(viewGO.transform, false);
        var cRT = contentGO.AddComponent<RectTransform>();
        cRT.anchorMin = new Vector2(0f, 1f);
        cRT.anchorMax = new Vector2(1f, 1f);
        cRT.pivot = new Vector2(0.5f, 1f);
        cRT.sizeDelta = new Vector2(0f, 0f);
        scroll.content = cRT;

        var vlg = contentGO.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 6f;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.padding = new RectOffset(0, 0, 4, 4);

        contentGO.AddComponent<ContentSizeFitter>().verticalFit =
            ContentSizeFitter.FitMode.PreferredSize;

        _bindLabels.Clear();
        foreach (var action in InputBindings.AllActions)
        {
            string cap = action;
            AddBindRow(contentGO.transform, action, cap);
        }
    }

    void AddBindRow(Transform parent, string action, string captured)
    {
        var row = new GameObject("Row_" + action);
        row.transform.SetParent(parent, false);

        var le = row.AddComponent<LayoutElement>();
        le.preferredHeight = 38f;
        le.flexibleWidth = 1f;

        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childForceExpandHeight = true;
        hlg.childControlHeight = true;
        hlg.padding = new RectOffset(4, 4, 2, 2);
        hlg.spacing = 8f;

        // Etykieta akcji
        var lblGO = new GameObject("ActionLabel");
        lblGO.transform.SetParent(row.transform, false);
        var lblLE = lblGO.AddComponent<LayoutElement>();
        lblLE.preferredWidth = 320f;
        lblLE.flexibleWidth = 1f;
        var lblTMP = lblGO.AddComponent<TextMeshProUGUI>();
        lblTMP.text = InputBindings.GetDisplayName(action);
        lblTMP.fontSize = 14f;
        lblTMP.color = C_TEXT;
        lblTMP.alignment = TextAlignmentOptions.MidlineLeft;

        // Przycisk rebind
        var btnGO = new GameObject("BindBtn");
        btnGO.transform.SetParent(row.transform, false);
        var btnLE = btnGO.AddComponent<LayoutElement>();
        btnLE.preferredWidth = 130f;
        btnLE.flexibleWidth = 0f;
        btnLE.preferredHeight = 34f;

        var img = btnGO.AddComponent<Image>();
        img.color = C_BIND_BTN;
        var btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = img;
        var c = btn.colors;
        c.normalColor = C_BIND_BTN;
        c.highlightedColor = C_BTN_H;
        c.pressedColor = C_BTN_P;
        btn.colors = c;

        var keyLblGO = new GameObject("KeyLabel");
        keyLblGO.transform.SetParent(btnGO.transform, false);
        UIHelper.Stretch(keyLblGO.AddComponent<RectTransform>());
        var keyTMP = keyLblGO.AddComponent<TextMeshProUGUI>();
        keyTMP.text = InputBindings.KeyName(InputBindings.Get(action));
        keyTMP.fontSize = 13f;
        keyTMP.color = C_TEXT;
        keyTMP.fontStyle = FontStyles.Bold;
        keyTMP.alignment = TextAlignmentOptions.Center;
        keyTMP.raycastTarget = false;

        _bindLabels[action] = keyTMP;
        btn.onClick.AddListener(() =>
        {
            if (_rebindingAction == captured) return;
            // Anuluj poprzedni rebind
            if (_rebindCoroutine != null) { StopCoroutine(_rebindCoroutine); _rebindCoroutine = null; _rebindingAction = null; RefreshBindLabels(); }
            // Zakoloruj aktywny przycisk
            foreach (var kv in _bindLabels)
            {
                var bGO = kv.Value.transform.parent.gameObject;
                bGO.GetComponent<Image>().color = kv.Key == captured ? C_BIND_ACT : C_BIND_BTN;
            }
            StartRebind(captured);
        });
    }

    // ── STOPKA (Zapisz / Powrót) ──────────────────────────────────────────

    void BuildFooter()
    {
        var foot = new GameObject("Footer");
        foot.transform.SetParent(_panel.transform, false);
        var fRT = foot.AddComponent<RectTransform>();
        fRT.anchorMin = new Vector2(0.04f, 0f);
        fRT.anchorMax = new Vector2(0.96f, 0.12f);
        fRT.offsetMin = fRT.offsetMax = Vector2.zero;

        var hlg = foot.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 20f;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childForceExpandHeight = true;
        hlg.childForceExpandWidth  = false;
        hlg.childControlHeight = true;
        hlg.childControlWidth  = true;
        hlg.padding = new RectOffset(0, 0, 6, 6);

        var saveBtn = UIHelper.MakeButton(foot.transform, "Zapisz", C_SAVE, C_SAVE_H, C_BTN_P, C_TEXT, OnSave, height: 46f);
        var saveLE  = saveBtn.gameObject.GetComponent<LayoutElement>();
        saveLE.preferredWidth = 200f;
        saveLE.flexibleWidth  = 0f;

        var backBtn = UIHelper.MakeButton(foot.transform, "Powrót", C_BTN, C_BTN_H, C_BTN_P, C_TEXT, Close, height: 46f);
        var backLE  = backBtn.gameObject.GetComponent<LayoutElement>();
        backLE.preferredWidth = 200f;
        backLE.flexibleWidth  = 0f;
    }

    void OnSave() { SaveSettings(); ClosePanel(); }

    // ── Zakładki ───────────────────────────────────────────────────────────

    void ShowTab(string tab)
    {
        _activeTab = tab;
        foreach (var kv in _tabContents)
            kv.Value.SetActive(kv.Key == tab);

        foreach (var kv in _tabBtns)
        {
            bool active = kv.Key == tab;
            var img = kv.Value.GetComponent<Image>();
            if (img != null) img.color = active ? C_TAB_ON : C_TAB_OFF;
        }
    }

    // ── Helpers budowania UI ──────────────────────────────────────────────

    static GameObject MakeTabPanel(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        UIHelper.Stretch(go.AddComponent<RectTransform>());
        return go;
    }

    Slider AddSliderRow(Transform parent, string label, float min, float max, float val,
        System.Action<float> onChange)
    {
        var row = UIHelper.MakeRow("Row_" + label, parent, 48f);

        AddRowLabel(row.transform, label);

        // Wartość procentowa
        var pctGO = new GameObject("Pct");
        pctGO.transform.SetParent(row.transform, false);
        var pctLE = pctGO.AddComponent<LayoutElement>();
        pctLE.preferredWidth = 56f;
        var pctTMP = pctGO.AddComponent<TextMeshProUGUI>();
        pctTMP.fontSize = 16f;
        pctTMP.color = C_TEXT_DIM;
        pctTMP.alignment = TextAlignmentOptions.MidlineRight;

        // Slider
        var sGO = new GameObject("Slider");
        sGO.transform.SetParent(row.transform, false);
        var sLE = sGO.AddComponent<LayoutElement>();
        sLE.preferredWidth = 220f;
        sLE.preferredHeight = 10f;
        sLE.flexibleWidth = 1f;
        sGO.AddComponent<RectTransform>();

        // Background
        var bg = new GameObject("BG");
        bg.transform.SetParent(sGO.transform, false);
        UIHelper.Stretch(bg.AddComponent<RectTransform>());
        bg.AddComponent<Image>().color = C_SLIDER_BG;

        // Fill area
        var fillArea = new GameObject("FillArea");
        fillArea.transform.SetParent(sGO.transform, false);
        var faRT = fillArea.AddComponent<RectTransform>();
        faRT.anchorMin = new Vector2(0f, 0.25f);
        faRT.anchorMax = new Vector2(1f, 0.75f);
        faRT.offsetMin = new Vector2(5f, 0f);
        faRT.offsetMax = new Vector2(-5f, 0f);

        var fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        UIHelper.Stretch(fill.AddComponent<RectTransform>());
        fill.AddComponent<Image>().color = C_SLIDER_FILL;

        // Handle
        var handleArea = new GameObject("HandleArea");
        handleArea.transform.SetParent(sGO.transform, false);
        UIHelper.Stretch(handleArea.AddComponent<RectTransform>());

        var handle = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform, false);
        var hRT = handle.AddComponent<RectTransform>();
        hRT.sizeDelta = new Vector2(16f, 22f);
        handle.AddComponent<Image>().color = Color.white;

        var slider = sGO.AddComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.value = val;
        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.handleRect = hRT;
        slider.targetGraphic = handle.GetComponent<Image>();
        slider.direction = Slider.Direction.LeftToRight;

        pctTMP.text = Mathf.RoundToInt(val * 100f) + "%";
        slider.onValueChanged.AddListener(v =>
        {
            onChange?.Invoke(v);
            pctTMP.text = Mathf.RoundToInt(v * 100f) + "%";
        });

        return slider;
    }

    Toggle AddToggleRow(Transform parent, string label, bool initVal, System.Action<bool> onChange)
    {
        var row = UIHelper.MakeRow("Row_" + label, parent, 60f);
        AddRowLabel(row.transform, label);

        var tGO = new GameObject("Toggle");
        tGO.transform.SetParent(row.transform, false);
        var tLE = tGO.AddComponent<LayoutElement>();
        tLE.preferredWidth  = 40f;
        tLE.preferredHeight = 40f;
        tLE.flexibleWidth   = 0f;
        tLE.flexibleHeight  = 0f;

        var bgImg = tGO.AddComponent<Image>();
        bgImg.color = C_SLIDER_BG;
        var outline = tGO.AddComponent<Outline>();
        outline.effectColor    = C_SEP;
        outline.effectDistance = new Vector2(2f, -2f);

        var toggle = tGO.AddComponent<Toggle>();
        toggle.targetGraphic = bgImg;
        toggle.isOn = initVal;

        // Checkmark wypełnia wnętrze kwadratu
        var checkGO = new GameObject("Check");
        checkGO.transform.SetParent(tGO.transform, false);
        var cRT = checkGO.AddComponent<RectTransform>();
        cRT.anchorMin = new Vector2(0.15f, 0.15f);
        cRT.anchorMax = new Vector2(0.85f, 0.85f);
        cRT.offsetMin = cRT.offsetMax = Vector2.zero;
        var checkImg = checkGO.AddComponent<Image>();
        checkImg.color = C_SLIDER_FILL;
        toggle.graphic = checkImg;

        var cols = toggle.colors;
        cols.normalColor = C_SLIDER_BG;
        cols.highlightedColor = new Color(0.26f, 0.26f, 0.32f);
        cols.pressedColor = new Color(0.14f, 0.14f, 0.18f);
        toggle.colors = cols;

        toggle.onValueChanged.AddListener(v => onChange?.Invoke(v));
        return toggle;
    }

    static void AddRowLabel(Transform parent, string text)
    {
        var lGO = new GameObject("Lbl");
        lGO.transform.SetParent(parent, false);
        var lLE = lGO.AddComponent<LayoutElement>();
        lLE.preferredWidth = 360f;
        lLE.flexibleWidth  = 0f;
        var lTMP = lGO.AddComponent<TextMeshProUGUI>();
        lTMP.text                = text;
        lTMP.fontSize            = 18f;
        lTMP.color               = new Color(0.88f, 0.88f, 0.91f);
        lTMP.alignment           = TextAlignmentOptions.MidlineLeft;
        lTMP.overflowMode        = TextOverflowModes.Overflow;
        lTMP.textWrappingMode = TextWrappingModes.NoWrap;
    }

    // ── Nakładka jasności (DontDestroyOnLoad) ─────────────────────────────

    void BuildBrightnessOverlay()
    {
        var cGO = new GameObject("BrightnessCanvas");
        DontDestroyOnLoad(cGO);
        var c = cGO.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 500;
        cGO.AddComponent<CanvasScaler>();
        cGO.AddComponent<GraphicRaycaster>();

        var go = new GameObject("BrightnessOverlay");
        go.transform.SetParent(cGO.transform, false);
        UIHelper.Stretch(go.AddComponent<RectTransform>());
        _brightnessOverlay = go.AddComponent<Image>();
        _brightnessOverlay.color = new Color(0f, 0f, 0f, 0f);
        _brightnessOverlay.raycastTarget = false;
    }
}