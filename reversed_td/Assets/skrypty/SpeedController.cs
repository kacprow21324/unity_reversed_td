using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// Kontroler prędkości gry — tylko singleplayer.
/// Dodaj do dowolnego GameObject w scenach SP (teren1/2/3).
/// W Inspektorze przypisz speedIcon: Assets/Grafiki2d/SkymonIconPackFree/skymon-icons-white/select-icon2
public class SpeedController : MonoBehaviour
{
    public static SpeedController Instance { get; private set; }

    [Header("Ikona (przypisz select-icon2.png z Assets/Grafiki2d/SkymonIconPackFree/skymon-icons-white)")]
    public Sprite speedIcon;

    [Header("Auto-przyspieszenie gdy brak wież na mapie")]
    [SerializeField] private float autoBoostMultiplier = 3f;
    [SerializeField] private float towerCheckInterval  = 1f;

    // ── Stan ──────────────────────────────────────────────────────────────

    private float _multiplier      = 1f;
    private bool  _wasPlanning     = true;
    private bool  _autoBoost       = false;
    private float _towerCheckTimer = 0f;

    // ── UI ────────────────────────────────────────────────────────────────

    private GameObject _outerContainer;
    private Button[]   _speedButtons;

    static readonly float[]  SPEEDS = { 1f, 2f, 3f, 5f };
    static readonly string[] LABELS = { "1x", "2x", "3x", "5x" };

    static readonly Color C_BG     = new Color(0.07f, 0.10f, 0.18f, 0.88f);
    static readonly Color C_NORMAL = new Color(0.14f, 0.20f, 0.35f, 1f);
    static readonly Color C_ACTIVE = new Color(0.18f, 0.50f, 0.88f, 1f);
    static readonly Color C_HOVER  = new Color(0.22f, 0.35f, 0.55f, 1f);
    static readonly Color C_PRESS  = new Color(0.06f, 0.10f, 0.18f, 1f);
    static readonly Color C_LABEL  = new Color(0.75f, 0.85f, 1.00f, 1f);

    // ── Unity lifecycle ───────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (SceneManager.GetActiveScene().name.Contains("Multiplayer"))
        {
            Destroy(gameObject);
            return;
        }
        BuildUI();
        _outerContainer.SetActive(false);
    }

    void Update()
    {
        if (_outerContainer == null) return;

        bool gameOver = GameManager.Instance != null && GameManager.Instance.IsGameOver;
        var  uiMgr    = GameplayUIManager.Instance;
        bool planning = uiMgr == null || uiMgr.IsPlanning;

        // Przejście do fazy ataku — resetuj auto-boost i zastosuj wybrany mnożnik
        if (_wasPlanning && !planning && !gameOver)
        {
            _autoBoost       = false;
            _towerCheckTimer = towerCheckInterval;
            Time.timeScale   = _multiplier;
        }

        _wasPlanning = planning;

        // Sprawdzaj wieże co sekundę podczas fazy ataku
        if (!planning && !gameOver && !_autoBoost)
        {
            _towerCheckTimer -= Time.unscaledDeltaTime;
            if (_towerCheckTimer <= 0f)
            {
                _towerCheckTimer = towerCheckInterval;
                CheckTowersAndAutoBoost();
            }
        }

        bool show = !planning && !gameOver;
        if (_outerContainer.activeSelf != show)
            _outerContainer.SetActive(show);
    }

    // ── Auto-boost ────────────────────────────────────────────────────────

    void CheckTowersAndAutoBoost()
    {
        if (FindObjectsByType<WiezaBaza>(FindObjectsSortMode.None).Length > 0) return;

        _autoBoost     = true;
        Time.timeScale = Mathf.Max(_multiplier, autoBoostMultiplier);
    }

    // ── Prędkość ──────────────────────────────────────────────────────────

    void SetMultiplier(float m)
    {
        _multiplier = m;
        var uiMgr = GameplayUIManager.Instance;
        if (uiMgr != null && !uiMgr.IsPlanning)
            Time.timeScale = _autoBoost ? Mathf.Max(_multiplier, autoBoostMultiplier) : _multiplier;
        RefreshColors();
    }

    void RefreshColors()
    {
        if (_speedButtons == null) return;
        for (int i = 0; i < _speedButtons.Length; i++)
        {
            if (_speedButtons[i] == null) continue;
            bool  sel    = Mathf.Approximately(SPEEDS[i], _multiplier);
            Color target = sel ? C_ACTIVE : C_NORMAL;

            // Natychmiastowa zmiana koloru + aktualizacja stanu normalnego
            _speedButtons[i].targetGraphic.color = target;
            var bc = _speedButtons[i].colors;
            bc.normalColor = target;
            _speedButtons[i].colors = bc;
        }
    }

    // ── Budowanie UI ──────────────────────────────────────────────────────

    void BuildUI()
    {
        // Canvas
        var cGO = new GameObject("SpeedControllerCanvas");
        cGO.transform.SetParent(transform, false);

        var canvas          = cGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        var scaler                 = cGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight  = 0.5f;

        cGO.AddComponent<GraphicRaycaster>();

        // Zewnętrzny kontener — prawy górny róg, pionowy układ (napis + rząd przycisków)
        _outerContainer = new GameObject("SpeedOuterContainer");
        _outerContainer.transform.SetParent(cGO.transform, false);

        var oRT              = _outerContainer.AddComponent<RectTransform>();
        oRT.anchorMin        = new Vector2(1f, 1f);
        oRT.anchorMax        = new Vector2(1f, 1f);
        oRT.pivot            = new Vector2(1f, 1f);
        oRT.anchoredPosition = new Vector2(-14f, -14f);

        var oBg   = _outerContainer.AddComponent<Image>();
        oBg.color = C_BG;

        var oVlg                    = _outerContainer.AddComponent<VerticalLayoutGroup>();
        oVlg.spacing                = 4f;
        oVlg.padding                = new RectOffset(8, 8, 6, 6);
        oVlg.childForceExpandWidth  = true;
        oVlg.childForceExpandHeight = false;
        oVlg.childControlWidth      = true;
        oVlg.childControlHeight     = true;
        oVlg.childAlignment         = TextAnchor.UpperCenter;

        var oCsf           = _outerContainer.AddComponent<ContentSizeFitter>();
        oCsf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        oCsf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

        // Napis "Zmiana prędkości"
        var labelGO = new GameObject("SpeedLabel");
        labelGO.transform.SetParent(_outerContainer.transform, false);

        var lLE             = labelGO.AddComponent<LayoutElement>();
        lLE.preferredHeight = 20f;

        var lTmp           = labelGO.AddComponent<TextMeshProUGUI>();
        lTmp.text          = "Zmiana prędkości";
        lTmp.fontSize      = 12f;
        lTmp.fontStyle     = FontStyles.Bold;
        lTmp.color         = C_LABEL;
        lTmp.alignment     = TextAlignmentOptions.Center;
        lTmp.raycastTarget = false;

        // Rząd: ikona + przyciski
        var rowGO = new GameObject("SpeedButtonsRow");
        rowGO.transform.SetParent(_outerContainer.transform, false);

        var rowLE             = rowGO.AddComponent<LayoutElement>();
        rowLE.preferredHeight = 42f;

        var rowHlg                    = rowGO.AddComponent<HorizontalLayoutGroup>();
        rowHlg.spacing                = 4f;
        rowHlg.childForceExpandWidth  = false;
        rowHlg.childForceExpandHeight = true;
        rowHlg.childControlWidth      = true;
        rowHlg.childControlHeight     = true;
        rowHlg.childAlignment         = TextAnchor.MiddleCenter;

        var rowCsf           = rowGO.AddComponent<ContentSizeFitter>();
        rowCsf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Ikona (dekoracyjna)
        var iconGO             = new GameObject("SpeedIcon");
        iconGO.transform.SetParent(rowGO.transform, false);
        var iconLE             = iconGO.AddComponent<LayoutElement>();
        iconLE.preferredWidth  = 42f;
        iconLE.preferredHeight = 42f;
        var iconImg            = iconGO.AddComponent<Image>();
        iconImg.color          = Color.white;
        iconImg.sprite         = speedIcon;
        iconImg.preserveAspect = true;
        iconImg.raycastTarget  = false;

        // Przyciski prędkości
        _speedButtons = new Button[SPEEDS.Length];
        for (int i = 0; i < SPEEDS.Length; i++)
        {
            float  speed  = SPEEDS[i];
            string label  = LABELS[i];
            bool   active = Mathf.Approximately(speed, _multiplier);

            var btnGO = new GameObject("SpeedBtn_" + label);
            btnGO.transform.SetParent(rowGO.transform, false);

            var le             = btnGO.AddComponent<LayoutElement>();
            le.preferredWidth  = 46f;
            le.preferredHeight = 42f;

            var btnImg   = btnGO.AddComponent<Image>();
            btnImg.color = active ? C_ACTIVE : C_NORMAL;

            var btn           = btnGO.AddComponent<Button>();
            btn.targetGraphic = btnImg;
            var bc            = btn.colors;
            bc.normalColor      = active ? C_ACTIVE : C_NORMAL;
            bc.highlightedColor = C_HOVER;
            bc.pressedColor     = C_PRESS;
            bc.selectedColor    = C_ACTIVE;
            bc.colorMultiplier  = 1f;
            btn.colors = bc;

            float capturedSpeed = speed;
            btn.onClick.AddListener(() => SetMultiplier(capturedSpeed));

            var txtGO         = new GameObject("Label");
            txtGO.transform.SetParent(btnGO.transform, false);
            var txtRT         = txtGO.AddComponent<RectTransform>();
            txtRT.anchorMin   = Vector2.zero;
            txtRT.anchorMax   = Vector2.one;
            txtRT.offsetMin   = Vector2.zero;
            txtRT.offsetMax   = Vector2.zero;
            var tmp           = txtGO.AddComponent<TextMeshProUGUI>();
            tmp.text          = label;
            tmp.fontSize      = 17f;
            tmp.fontStyle     = FontStyles.Bold;
            tmp.color         = Color.white;
            tmp.alignment     = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;

            _speedButtons[i] = btn;
        }
    }
}
