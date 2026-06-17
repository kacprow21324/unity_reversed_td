using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// Animowane markery START/META + przycisk UI do ich pokazywania/ukrywania.
/// Dodaj do osobnego GameObject w scenie.
public class PathMarkers : MonoBehaviour
{
    [Header("Obiekty trasy — przeciągnij z hierarchii")]
    public Transform pozycjaStartu;
    public Transform pozycjaKonca;

    [Header("Animacja")]
    [SerializeField] private float bounceHeight     = 2.4f;
    [SerializeField] private float bounceSpeed      = 2.2f;
    [SerializeField] private float baseHeight       = 15f;
    [SerializeField] private float startExtraHeight = 0f;

    [Header("Wygląd")]
    [SerializeField] private Color colorStart = new Color(0.2f, 1f,    0.3f, 1f);
    [SerializeField] private Color colorMeta  = new Color(1f,   0.25f, 0.25f, 1f);

    // ── Stan ──────────────────────────────────────────────────────────────

    private Transform _startPivot;
    private Transform _metaPivot;
    private bool      _visible = true;

    // ── Przycisk ──────────────────────────────────────────────────────────

    private Image  _btnImg;
    private Button _btn;

    static readonly Color C_OFF   = new Color(0.14f, 0.20f, 0.35f, 1f);
    static readonly Color C_ON    = new Color(0.18f, 0.50f, 0.88f, 1f);
    static readonly Color C_HOVER = new Color(0.22f, 0.35f, 0.55f, 1f);
    static readonly Color C_PRESS = new Color(0.06f, 0.10f, 0.18f, 1f);

    // ── Unity lifecycle ───────────────────────────────────────────────────

    void Start()
    {
        if (pozycjaStartu != null)
            _startPivot = BuildMarker("START", pozycjaStartu.position, colorStart,
                                      baseHeight + startExtraHeight).transform;
        else
            Debug.LogWarning("[PathMarkers] Nie przypisano Pozycja Startu.");

        if (pozycjaKonca != null)
            _metaPivot = BuildMarker("META", pozycjaKonca.position, colorMeta,
                                     baseHeight).transform;
        else
            Debug.LogWarning("[PathMarkers] Nie przypisano Pozycja Konca.");

        BuildUI();
        SetVisible(true);
    }

    void Update()
    {
        if (!_visible) return;

        float offset  = Mathf.Sin(Time.time * bounceSpeed) * bounceHeight;
        Quaternion camRot = Camera.main != null ? Camera.main.transform.rotation : Quaternion.identity;

        if (_startPivot != null)
        {
            Vector3 p = _startPivot.position;
            p.y = _startPivot.GetComponent<MarkerData>().baseY + offset;
            _startPivot.position = p;
            _startPivot.rotation = camRot;
        }
        if (_metaPivot != null)
        {
            Vector3 p = _metaPivot.position;
            p.y = _metaPivot.GetComponent<MarkerData>().baseY + offset;
            _metaPivot.position = p;
            _metaPivot.rotation = camRot;
        }
    }

    // ── Toggle ────────────────────────────────────────────────────────────

    void Toggle()
    {
        SetVisible(!_visible);
    }

    void SetVisible(bool v)
    {
        _visible = v;
        if (_startPivot != null) _startPivot.gameObject.SetActive(v);
        if (_metaPivot  != null) _metaPivot.gameObject.SetActive(v);

        Color c = v ? C_ON : C_OFF;
        if (_btnImg != null)
        {
            _btnImg.color = c;
            _btn.targetGraphic.color = c;
            var bc = _btn.colors;
            bc.normalColor = c;
            _btn.colors = bc;
        }
    }

    // ── Budowanie markera 3D ──────────────────────────────────────────────

    GameObject BuildMarker(string label, Vector3 groundPos, Color color, float height)
    {
        var pivot = new GameObject("Marker_" + label);
        pivot.transform.position = new Vector3(groundPos.x, height, groundPos.z);
        var data  = pivot.AddComponent<MarkerData>();
        data.baseY = height;

        // Strzałka (LineRenderer)
        var arrowGO          = new GameObject("Arrow");
        arrowGO.transform.SetParent(pivot.transform, false);
        var lr               = arrowGO.AddComponent<LineRenderer>();
        lr.useWorldSpace     = false;
        lr.positionCount     = 5;
        lr.widthMultiplier   = 0.32f;
        lr.loop              = false;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows    = false;
        lr.sortingOrder      = 5;

        Shader sh = Shader.Find("Sprites/Default")
                 ?? Shader.Find("Universal Render Pipeline/Particles/Unlit")
                 ?? Shader.Find("Unlit/Color");
        if (sh != null) lr.material = new Material(sh);
        lr.startColor = color;
        lr.endColor   = color;

        float aw = 1f;
        lr.SetPosition(0, new Vector3(0f,    0f,   0f));
        lr.SetPosition(1, new Vector3(0f,  -2.8f,  0f));
        lr.SetPosition(2, new Vector3( aw, -1.6f,  0f));
        lr.SetPosition(3, new Vector3(0f,  -2.8f,  0f));
        lr.SetPosition(4, new Vector3(-aw, -1.6f,  0f));

        // Napis
        var textGO = new GameObject("Label");
        textGO.transform.SetParent(pivot.transform, false);
        textGO.transform.localPosition = new Vector3(0f, 1.6f, 0f);
        var tmp           = textGO.AddComponent<TextMeshPro>();
        tmp.text          = label;
        tmp.fontSize      = 40f;
        tmp.fontStyle     = FontStyles.Bold;
        tmp.color         = color;
        tmp.alignment     = TextAlignmentOptions.Center;
        tmp.sortingOrder  = 5;

        // Tło pod napisem
        var bgGO = new GameObject("BG");
        bgGO.transform.SetParent(pivot.transform, false);
        bgGO.transform.localPosition = new Vector3(0f, 1.44f, 0.01f);
        bgGO.transform.localScale    = new Vector3(11.2f, 4.4f, 1f);
        var bg          = bgGO.AddComponent<SpriteRenderer>();
        bg.sprite       = MakeSquareSprite();
        bg.color        = new Color(color.r * 0.15f, color.g * 0.15f, color.b * 0.15f, 0.75f);
        bg.sortingOrder = 4;

        return pivot;
    }

    Sprite MakeSquareSprite()
    {
        var tex  = new Texture2D(4, 4);
        var cols = new Color[16];
        for (int i = 0; i < 16; i++) cols[i] = Color.white;
        tex.SetPixels(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
    }

    // ── Budowanie przycisku UI ────────────────────────────────────────────

    void BuildUI()
    {
        var cGO = new GameObject("PathMarkersCanvas");
        cGO.transform.SetParent(transform, false);

        var canvas          = cGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 40;

        var s                 = cGO.AddComponent<CanvasScaler>();
        s.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        s.referenceResolution = new Vector2(1920f, 1080f);
        s.matchWidthOrHeight  = 0.5f;

        cGO.AddComponent<GraphicRaycaster>();

        var btnGO = new GameObject("Btn");
        btnGO.transform.SetParent(cGO.transform, false);

        var rt              = btnGO.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0f, 0f);
        rt.anchorMax        = new Vector2(0f, 0f);
        rt.pivot            = new Vector2(0f, 0f);
        rt.anchoredPosition = new Vector2(184f, 14f);   // obok przycisku TowerRangeToggle
        rt.sizeDelta        = new Vector2(160f, 40f);

        _btnImg       = btnGO.AddComponent<Image>();
        _btnImg.color = C_ON;

        _btn               = btnGO.AddComponent<Button>();
        _btn.targetGraphic = _btnImg;
        var bc             = _btn.colors;
        bc.normalColor      = C_ON;
        bc.highlightedColor = C_HOVER;
        bc.pressedColor     = C_PRESS;
        _btn.colors        = bc;
        _btn.onClick.AddListener(Toggle);

        var lGO       = new GameObject("Label");
        lGO.transform.SetParent(btnGO.transform, false);
        var lRT       = lGO.AddComponent<RectTransform>();
        lRT.anchorMin = Vector2.zero;
        lRT.anchorMax = Vector2.one;
        lRT.offsetMin = Vector2.zero;
        lRT.offsetMax = Vector2.zero;
        var tmp           = lGO.AddComponent<TextMeshProUGUI>();
        tmp.text          = "Pokaż START / META";
        tmp.fontSize      = 12f;
        tmp.fontStyle     = FontStyles.Bold;
        tmp.color         = Color.white;
        tmp.alignment     = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
    }

    class MarkerData : MonoBehaviour { public float baseY; }
}
