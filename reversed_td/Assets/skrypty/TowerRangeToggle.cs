using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// Przycisk "Pokaż zasięg wież". Dodaj do osobnego GameObject w scenie.
public class TowerRangeToggle : MonoBehaviour
{
    public static TowerRangeToggle Instance { get; private set; }
    public bool IsShowingAll { get; private set; }

    private Image  _btnImg;
    private Button _btn;

    static readonly Color C_OFF   = new Color(0.14f, 0.20f, 0.35f, 1f);
    static readonly Color C_ON    = new Color(0.18f, 0.50f, 0.88f, 1f);
    static readonly Color C_HOVER = new Color(0.22f, 0.35f, 0.55f, 1f);
    static readonly Color C_PRESS = new Color(0.06f, 0.10f, 0.18f, 1f);

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start() => BuildUI();

    void Update()
    {
        bool gameOver = GameManager.Instance != null && GameManager.Instance.IsGameOver;
        if (_btn != null) _btn.interactable = !gameOver;
    }

    public void Toggle()
    {
        IsShowingAll = !IsShowingAll;
        foreach (var t in FindObjectsByType<WiezaBaza>(FindObjectsSortMode.None))
        {
            if (IsShowingAll) t.PokazZasieg();
            else              t.UkryjZasieg();
        }
        RefreshColor();
    }

    void RefreshColor()
    {
        Color c = IsShowingAll ? C_ON : C_OFF;
        _btnImg.color = c;
        _btn.targetGraphic.color = c;
        var bc = _btn.colors;
        bc.normalColor = c;
        _btn.colors = bc;
    }

    void BuildUI()
    {
        var cGO = new GameObject("TowerRangeToggleCanvas");
        cGO.transform.SetParent(transform, false);

        var canvas          = cGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 40;

        var s                  = cGO.AddComponent<CanvasScaler>();
        s.uiScaleMode          = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        s.referenceResolution  = new Vector2(1920f, 1080f);
        s.matchWidthOrHeight   = 0.5f;

        cGO.AddComponent<GraphicRaycaster>();

        var btnGO = new GameObject("Btn");
        btnGO.transform.SetParent(cGO.transform, false);

        var rt              = btnGO.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0f, 0f);
        rt.anchorMax        = new Vector2(0f, 0f);
        rt.pivot            = new Vector2(0f, 0f);
        rt.anchoredPosition = new Vector2(14f, 14f);
        rt.sizeDelta        = new Vector2(160f, 40f);

        _btnImg       = btnGO.AddComponent<Image>();
        _btnImg.color = C_OFF;

        _btn                = btnGO.AddComponent<Button>();
        _btn.targetGraphic  = _btnImg;
        var bc              = _btn.colors;
        bc.normalColor      = C_OFF;
        bc.highlightedColor = C_HOVER;
        bc.pressedColor     = C_PRESS;
        _btn.colors         = bc;
        _btn.onClick.AddListener(Toggle);

        var lGO       = new GameObject("Label");
        lGO.transform.SetParent(btnGO.transform, false);
        var lRT       = lGO.AddComponent<RectTransform>();
        lRT.anchorMin = Vector2.zero;
        lRT.anchorMax = Vector2.one;
        lRT.offsetMin = Vector2.zero;
        lRT.offsetMax = Vector2.zero;
        var tmp           = lGO.AddComponent<TextMeshProUGUI>();
        tmp.text          = "Pokaż zasięg wież";
        tmp.fontSize      = 13f;
        tmp.fontStyle     = FontStyles.Bold;
        tmp.color         = Color.white;
        tmp.alignment     = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
    }
}
