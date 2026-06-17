using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// Panel informacyjny wieży (hover + kliknięcie). Dodaj do osobnego GameObject w scenie.
public class TowerInfoPanel : MonoBehaviour
{
    public static TowerInfoPanel Instance { get; private set; }

    private WiezaBaza  _hovered;
    private WiezaBaza  _selected;
    private GameObject _panel;

    private TextMeshProUGUI _nazwa;
    private TextMeshProUGUI _zasieg;
    private TextMeshProUGUI _obrazenia;
    private TextMeshProUGUI _przeladowanie;
    private TextMeshProUGUI _opis;

    static readonly Color C_BG     = new Color(0.07f, 0.10f, 0.18f, 0.92f);
    static readonly Color C_HEADER = new Color(0.75f, 0.85f, 1.00f, 1f);
    static readonly Color C_SEP    = new Color(0.28f, 0.28f, 0.40f, 1f);
    static readonly Color C_LABEL  = new Color(0.60f, 0.70f, 0.85f, 1f);
    static readonly Color C_VALUE  = Color.white;
    static readonly Color C_SKILL  = new Color(1f, 0.85f, 0.30f, 1f);

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start() => BuildUI();

    void Update()
    {
        bool gameOver = GameManager.Instance != null && GameManager.Instance.IsGameOver;
        if (gameOver)
        {
            if (_hovered  != null) { _hovered.UkryjPodswietlenie();  _hovered  = null; }
            if (_selected != null) { _selected.UkryjZasieg(); _selected = null; }
            if (_panel    != null) _panel.SetActive(false);
            return;
        }

        HandleHover();
        HandleClick();
    }

    // ── Hover ─────────────────────────────────────────────────────────────

    void HandleHover()
    {
        if (Camera.main == null) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        WiezaBaza hit = null;
        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit rh, 300f))
            hit = rh.transform.GetComponentInParent<WiezaBaza>();

        if (hit == _hovered) return;

        if (_hovered != null)
        {
            _hovered.UkryjPodswietlenie();
            bool showAllActive = TowerRangeToggle.Instance != null && TowerRangeToggle.Instance.IsShowingAll;
            if (_hovered != _selected && !showAllActive)
                _hovered.UkryjZasieg();
        }

        _hovered = hit;

        if (_hovered != null)
        {
            _hovered.PokazPodswietlenie();
            bool showAllActive = TowerRangeToggle.Instance != null && TowerRangeToggle.Instance.IsShowingAll;
            if (!showAllActive) _hovered.PokazZasieg();
        }
    }

    // ── Kliknięcie ────────────────────────────────────────────────────────

    void HandleClick()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        if (Camera.main == null) return;

        WiezaBaza clicked = null;
        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit rh, 300f))
            clicked = rh.transform.GetComponentInParent<WiezaBaza>();

        if (clicked != null) Select(clicked);
        else                 Deselect();
    }

    void Select(WiezaBaza t)
    {
        bool showAll = TowerRangeToggle.Instance != null && TowerRangeToggle.Instance.IsShowingAll;
        if (_selected != null && _selected != t && !showAll)
            _selected.UkryjZasieg();

        _selected = t;
        _selected.PokazZasieg();
        Refresh(t);
        if (_panel != null) _panel.SetActive(true);
    }

    public void Deselect()
    {
        bool showAll = TowerRangeToggle.Instance != null && TowerRangeToggle.Instance.IsShowingAll;
        if (_selected != null && !showAll && _selected != _hovered)
            _selected.UkryjZasieg();

        _selected = null;
        if (_panel != null) _panel.SetActive(false);
    }

    // ── Dane wieży ────────────────────────────────────────────────────────

    struct Data { public string nazwa, zasieg, obrazenia, przeladowanie, opis; }

    Data GetData(WiezaBaza w)
    {
        if (w is WiezaPodstawowa wp)
            return new Data
            {
                nazwa         = "Wieża Podstawowa",
                zasieg        = $"{wp.zasieg} m",
                obrazenia     = $"{wp.obrazenia} HP / strzał",
                przeladowanie = $"{(1f / wp.szybkoscAtaku):F2} s",
                opis          = "Strzela do jednego wroga na raz.\nCeluje priorytetowo w pojazdy z tauntem."
            };

        if (w is WiezaArmatnia wa)
        {
            string dmg = "—";
            if (wa.prefabPocisku != null)
            {
                var p = wa.prefabPocisku.GetComponent<PociskArmatni>();
                if (p != null) dmg = $"{p.obrazenia} HP (prom. {p.promienWybuchu} m)";
            }
            return new Data
            {
                nazwa         = "Armata",
                zasieg        = $"{wa.zasieg} m",
                obrazenia     = dmg + ", przebija pancerz",
                przeladowanie = $"{wa.czasPrzeladowania} s",
                opis          = "Wolno strzela, ale pocisk eksploduje po trafieniu.\nIgnoruje pancerz i uszkadza pobliskie pojazdy."
            };
        }

        if (w is WiezaKolcowa wk)
            return new Data
            {
                nazwa         = "Wieża Kolcowa",
                zasieg        = $"{wk.promienRozmieszczenia} m",
                obrazenia     = $"{wk.obrazeniaKolca} HP / kolec",
                przeladowanie = $"{wk.cooldownPrzeladowania:F0} s (po zużyciu)",
                opis          = $"Rozkłada {wk.iloscKolcow} kolców na drodze przeciwnika.\nPo zużyciu wszystkich regeneruje je automatycznie."
            };

        if (w is WiezaPlazmowa wpl)
            return new Data
            {
                nazwa         = "Wieża Plazmowa",
                zasieg        = $"{wpl.zasieg} m",
                obrazenia     = $"{wpl.obrazeniaBazowe}–{wpl.obrazeniaBazowe * wpl.maxMnoznik:F0} HP / {wpl.tickRate:F2} s",
                przeladowanie = $"Co {wpl.tickRate:F2} s",
                opis          = $"Ciągła wiązka energii — im dłużej razi ten sam cel,\ntym mocniejsze obrażenia (do x{wpl.maxMnoznik})."
            };

        if (w is WiezaSonar ws)
            return new Data
            {
                nazwa         = "Sonar",
                zasieg        = $"{ws.zasieg} m",
                obrazenia     = "Brak bezpośrednich",
                przeladowanie = $"Co {ws.czestotliwoscSkanowania:F1} s",
                opis          = $"Wzmacnia obrażenia pobliskich wież o x{ws.mnoznikDebuffa}.\nOdkrywa i śledzi ukryte pojazdy na trasie."
            };

        return new Data
        {
            nazwa = w.gameObject.name.Replace("(Clone)", "").Trim(),
            zasieg = "—", obrazenia = "—", przeladowanie = "—", opis = "—"
        };
    }

    void Refresh(WiezaBaza w)
    {
        var d = GetData(w);
        if (_nazwa        != null) _nazwa.text        = d.nazwa;
        if (_zasieg       != null) _zasieg.text       = d.zasieg;
        if (_obrazenia    != null) _obrazenia.text    = d.obrazenia;
        if (_przeladowanie != null) _przeladowanie.text = d.przeladowanie;
        if (_opis         != null) _opis.text         = d.opis;
    }

    // ── Budowanie UI ──────────────────────────────────────────────────────

    void BuildUI()
    {
        var cGO = new GameObject("TowerInfoPanelCanvas");
        cGO.transform.SetParent(transform, false);

        var canvas          = cGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 41;

        var s                 = cGO.AddComponent<CanvasScaler>();
        s.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        s.referenceResolution = new Vector2(1920f, 1080f);
        s.matchWidthOrHeight  = 0.5f;

        cGO.AddComponent<GraphicRaycaster>();

        _panel = new GameObject("Panel");
        _panel.transform.SetParent(cGO.transform, false);

        var pRT              = _panel.AddComponent<RectTransform>();
        pRT.anchorMin        = new Vector2(0f, 0f);
        pRT.anchorMax        = new Vector2(0f, 0f);
        pRT.pivot            = new Vector2(0f, 0f);
        pRT.anchoredPosition = new Vector2(14f, 64f);
        pRT.sizeDelta        = new Vector2(270f, 10f);

        _panel.AddComponent<Image>().color = C_BG;

        var vlg                    = _panel.AddComponent<VerticalLayoutGroup>();
        vlg.spacing                = 4f;
        vlg.padding                = new RectOffset(12, 12, 10, 10);
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth      = true;
        vlg.childControlHeight     = true;

        var csf         = _panel.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Nagłówek: nazwa + przycisk X
        var header = new GameObject("Header");
        header.transform.SetParent(_panel.transform, false);
        header.AddComponent<LayoutElement>().preferredHeight = 28f;

        var hHlg                    = header.AddComponent<HorizontalLayoutGroup>();
        hHlg.spacing                = 4f;
        hHlg.childForceExpandWidth  = false;
        hHlg.childForceExpandHeight = true;
        hHlg.childControlWidth      = true;
        hHlg.childControlHeight     = true;
        hHlg.childAlignment         = TextAnchor.MiddleLeft;

        var nGO = new GameObject("Nazwa");
        nGO.transform.SetParent(header.transform, false);
        nGO.AddComponent<LayoutElement>().flexibleWidth = 1f;
        _nazwa              = nGO.AddComponent<TextMeshProUGUI>();
        _nazwa.text         = "—";
        _nazwa.fontSize     = 16f;
        _nazwa.fontStyle    = FontStyles.Bold;
        _nazwa.color        = C_HEADER;
        _nazwa.alignment    = TextAlignmentOptions.Left;
        _nazwa.raycastTarget = false;

        var xGO = new GameObject("X");
        xGO.transform.SetParent(header.transform, false);
        var xLE             = xGO.AddComponent<LayoutElement>();
        xLE.preferredWidth  = 24f;
        xLE.preferredHeight = 24f;
        xLE.flexibleWidth   = 0f;
        var xImg            = xGO.AddComponent<Image>();
        xImg.color          = new Color(0.35f, 0.12f, 0.12f, 1f);
        var xBtn            = xGO.AddComponent<Button>();
        xBtn.targetGraphic  = xImg;
        var xBc             = xBtn.colors;
        xBc.normalColor      = new Color(0.35f, 0.12f, 0.12f, 1f);
        xBc.highlightedColor = new Color(0.65f, 0.18f, 0.18f, 1f);
        xBc.pressedColor     = new Color(0.20f, 0.06f, 0.06f, 1f);
        xBtn.colors          = xBc;
        xBtn.onClick.AddListener(Deselect);

        var xTGO       = new GameObject("Lbl");
        xTGO.transform.SetParent(xGO.transform, false);
        var xTRT       = xTGO.AddComponent<RectTransform>();
        xTRT.anchorMin = Vector2.zero;
        xTRT.anchorMax = Vector2.one;
        xTRT.offsetMin = Vector2.zero;
        xTRT.offsetMax = Vector2.zero;
        var xTmp           = xTGO.AddComponent<TextMeshProUGUI>();
        xTmp.text          = "X";
        xTmp.fontSize      = 13f;
        xTmp.fontStyle     = FontStyles.Bold;
        xTmp.color         = Color.white;
        xTmp.alignment     = TextAlignmentOptions.Center;
        xTmp.raycastTarget = false;

        Sep();
        _zasieg        = Row("Zasięg:");
        _obrazenia     = Row("Obrażenia:");
        _przeladowanie = Row("Przeładowanie:");
        Sep();

        var skGO           = new GameObject("SkillLabel");
        skGO.transform.SetParent(_panel.transform, false);
        skGO.AddComponent<LayoutElement>().preferredHeight = 18f;
        var skTmp          = skGO.AddComponent<TextMeshProUGUI>();
        skTmp.text         = "Umiejętność specjalna:";
        skTmp.fontSize     = 12f;
        skTmp.fontStyle    = FontStyles.Bold;
        skTmp.color        = C_LABEL;
        skTmp.alignment    = TextAlignmentOptions.Left;
        skTmp.raycastTarget = false;

        var opisGO           = new GameObject("Opis");
        opisGO.transform.SetParent(_panel.transform, false);
        opisGO.AddComponent<LayoutElement>().preferredHeight = 40f;
        _opis                    = opisGO.AddComponent<TextMeshProUGUI>();
        _opis.text               = "—";
        _opis.fontSize           = 12f;
        _opis.color              = C_SKILL;
        _opis.alignment          = TextAlignmentOptions.Left;
        _opis.textWrappingMode   = TMPro.TextWrappingModes.Normal;
        _opis.raycastTarget      = false;

        _panel.SetActive(false);
    }

    TextMeshProUGUI Row(string label)
    {
        var row = new GameObject("Row");
        row.transform.SetParent(_panel.transform, false);
        row.AddComponent<LayoutElement>().preferredHeight = 20f;

        var hlg                    = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing                = 6f;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth      = true;
        hlg.childControlHeight     = true;

        var lGO            = new GameObject("L");
        lGO.transform.SetParent(row.transform, false);
        var lLE            = lGO.AddComponent<LayoutElement>();
        lLE.preferredWidth = 115f;
        lLE.flexibleWidth  = 0f;
        var lTmp           = lGO.AddComponent<TextMeshProUGUI>();
        lTmp.text          = label;
        lTmp.fontSize      = 12f;
        lTmp.fontStyle     = FontStyles.Bold;
        lTmp.color         = C_LABEL;
        lTmp.alignment     = TextAlignmentOptions.Left;
        lTmp.raycastTarget = false;

        var vGO = new GameObject("V");
        vGO.transform.SetParent(row.transform, false);
        vGO.AddComponent<LayoutElement>().flexibleWidth = 1f;
        var vTmp           = vGO.AddComponent<TextMeshProUGUI>();
        vTmp.text          = "—";
        vTmp.fontSize      = 12f;
        vTmp.color         = C_VALUE;
        vTmp.alignment     = TextAlignmentOptions.Left;
        vTmp.raycastTarget = false;
        return vTmp;
    }

    void Sep()
    {
        var go = new GameObject("Sep");
        go.transform.SetParent(_panel.transform, false);
        go.AddComponent<LayoutElement>().preferredHeight = 1f;
        var img            = go.AddComponent<Image>();
        img.color          = C_SEP;
        img.raycastTarget  = false;
    }
}
