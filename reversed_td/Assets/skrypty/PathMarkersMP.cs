using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// Markery START/META dla trybu multiplayer — dwie mapy, cztery punkty.
/// Dodaj jeden obiekt do sceny MP i przypisz wszystkie cztery pola w Inspektorze.
public class PathMarkersMP : MonoBehaviour
{
    [Header("Mapa Gracza 1")]
    public Transform startGracz1;
    public Transform metaGracz1;

    [Header("Mapa Gracza 2")]
    public Transform startGracz2;
    public Transform metaGracz2;

    [Header("Animacja")]
    [SerializeField] private float bounceHeight = 2.4f;
    [SerializeField] private float bounceSpeed  = 2.2f;
    [SerializeField] private float baseHeight   = 15f;

    [Header("Kolory")]
    [SerializeField] private Color colorStart1 = new Color(0.2f, 1f,    0.3f,  1f);
    [SerializeField] private Color colorMeta1  = new Color(1f,   0.25f, 0.25f, 1f);
    [SerializeField] private Color colorStart2 = new Color(0.2f, 1f,    0.3f,  1f);
    [SerializeField] private Color colorMeta2  = new Color(1f,   0.25f, 0.25f, 1f);

    // ── Stan ──────────────────────────────────────────────────────────────

    private readonly List<(Transform pivot, MarkerData data)> _markers
        = new List<(Transform, MarkerData)>();

    private bool   _visible = true;
    private Image  _btnImg;
    private Button _btn;

    static readonly Color C_BTN_ON  = new Color(0.18f, 0.50f, 0.88f, 1f);
    static readonly Color C_BTN_OFF = new Color(0.14f, 0.20f, 0.35f, 1f);
    static readonly Color C_HOVER   = new Color(0.22f, 0.35f, 0.55f, 1f);
    static readonly Color C_PRESS   = new Color(0.06f, 0.10f, 0.18f, 1f);

    // ── Unity lifecycle ───────────────────────────────────────────────────

    void Start()
    {
        TryBuild("START", startGracz1, colorStart1);
        TryBuild("META",  metaGracz1,  colorMeta1);
        TryBuild("START", startGracz2, colorStart2);
        TryBuild("META",  metaGracz2,  colorMeta2);
        BuildUI();
        SetVisible(true);
    }

    void Update()
    {
        if (!_visible || Camera.main == null) return;

        float     offset = Mathf.Sin(Time.time * bounceSpeed) * bounceHeight;
        Quaternion cam   = Camera.main.transform.rotation;

        foreach (var (pivot, data) in _markers)
        {
            if (pivot == null) continue;
            Vector3 p = pivot.position;
            p.y = data.baseY + offset;
            pivot.position = p;
            pivot.rotation = cam;
        }
    }

    // ── Budowanie markera ─────────────────────────────────────────────────

    void TryBuild(string label, Transform source, Color color)
    {
        if (source == null)
        {
            Debug.LogWarning($"[PathMarkersMP] Nie przypisano: {label}");
            return;
        }

        float yBase  = baseHeight;
        var   pivot  = new GameObject("Marker_" + label);
        pivot.transform.position = new Vector3(source.position.x, yBase, source.position.z);

        var data   = pivot.AddComponent<MarkerData>();
        data.baseY = yBase;

        // Strzałka
        var arrGO           = new GameObject("Arrow");
        arrGO.transform.SetParent(pivot.transform, false);
        var lr              = arrGO.AddComponent<LineRenderer>();
        lr.useWorldSpace    = false;
        lr.positionCount    = 5;
        lr.widthMultiplier  = 0.32f;
        lr.loop             = false;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows   = false;
        lr.sortingOrder     = 5;

        Shader sh = Shader.Find("Sprites/Default")
                 ?? Shader.Find("Universal Render Pipeline/Particles/Unlit")
                 ?? Shader.Find("Unlit/Color");
        if (sh != null) lr.material = new Material(sh);
        lr.startColor = color;
        lr.endColor   = color;

        lr.SetPosition(0, new Vector3( 0f,    0f,  0f));
        lr.SetPosition(1, new Vector3( 0f,  -2.8f, 0f));
        lr.SetPosition(2, new Vector3( 1f,  -1.6f, 0f));
        lr.SetPosition(3, new Vector3( 0f,  -2.8f, 0f));
        lr.SetPosition(4, new Vector3(-1f,  -1.6f, 0f));

        // Napis
        var txtGO               = new GameObject("Label");
        txtGO.transform.SetParent(pivot.transform, false);
        txtGO.transform.localPosition = new Vector3(0f, 1.6f, 0f);
        var tmp                 = txtGO.AddComponent<TextMeshPro>();
        tmp.text                = label;
        tmp.fontSize            = 40f;
        tmp.fontStyle           = FontStyles.Bold;
        tmp.color               = color;
        tmp.alignment           = TextAlignmentOptions.Center;
        tmp.sortingOrder        = 5;

        // Tło napisu
        var bgGO                = new GameObject("BG");
        bgGO.transform.SetParent(pivot.transform, false);
        bgGO.transform.localPosition = new Vector3(0f, 1.44f, 0.01f);
        bgGO.transform.localScale    = new Vector3(13f, 4.4f, 1f);
        var bg                  = bgGO.AddComponent<SpriteRenderer>();
        bg.sprite               = MakeSprite();
        bg.color                = new Color(color.r * 0.15f, color.g * 0.15f, color.b * 0.15f, 0.75f);
        bg.sortingOrder         = 4;

        _markers.Add((pivot.transform, data));
    }

    Sprite MakeSprite()
    {
        var tex  = new Texture2D(4, 4);
        var cols = new Color[16];
        for (int i = 0; i < 16; i++) cols[i] = Color.white;
        tex.SetPixels(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
    }

    // ── Toggle ────────────────────────────────────────────────────────────

    void SetVisible(bool v)
    {
        _visible = v;
        foreach (var (pivot, _) in _markers)
            if (pivot != null) pivot.gameObject.SetActive(v);

        Color c = v ? C_BTN_ON : C_BTN_OFF;
        if (_btnImg != null)
        {
            _btnImg.color            = c;
            _btn.targetGraphic.color = c;
            var bc                   = _btn.colors;
            bc.normalColor           = c;
            _btn.colors              = bc;
        }
    }

    // ── Przycisk UI ───────────────────────────────────────────────────────

    void BuildUI()
    {
        var cGO = new GameObject("PathMarkersMPCanvas");
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
        rt.anchoredPosition = new Vector2(184f, 14f);
        rt.sizeDelta        = new Vector2(160f, 40f);

        _btnImg       = btnGO.AddComponent<Image>();
        _btnImg.color = C_BTN_ON;

        _btn               = btnGO.AddComponent<Button>();
        _btn.targetGraphic = _btnImg;
        var bc             = _btn.colors;
        bc.normalColor      = C_BTN_ON;
        bc.highlightedColor = C_HOVER;
        bc.pressedColor     = C_PRESS;
        _btn.colors         = bc;
        _btn.onClick.AddListener(() => SetVisible(!_visible));

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
