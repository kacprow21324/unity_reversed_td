using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// Samouczek — pełny canvas z kartami pojazdów, wież i mechanik gry.
/// Otwierany przez TutorialManager.Instance.OpenTutorial() z głównego menu.
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    Canvas     _canvas;
    GameObject _overlay;
    GameObject _panel;
    string     _activeTab;

    readonly string[]                       _tabNames    = { "POJAZDY", "WIEŻE", "MECHANIKI" };
    readonly Dictionary<string, Button>     _tabBtns     = new Dictionary<string, Button>();
    readonly Dictionary<string, GameObject> _tabContents = new Dictionary<string, GameObject>();

    // ── Kolory ────────────────────────────────────────────────────────────
    static readonly Color C_BG        = new Color(0.04f, 0.04f, 0.07f, 0.97f);
    static readonly Color C_PANEL     = new Color(0.09f, 0.09f, 0.12f, 1f);
    static readonly Color C_CARD      = new Color(0.13f, 0.13f, 0.17f, 1f);
    static readonly Color C_SEP       = new Color(0.25f, 0.25f, 0.32f, 1f);
    static readonly Color C_TAB_ON    = new Color(0.22f, 0.26f, 0.38f, 1f);
    static readonly Color C_TAB_OFF   = new Color(0.13f, 0.13f, 0.17f, 1f);
    static readonly Color C_TAB_H     = new Color(0.18f, 0.20f, 0.28f, 1f);
    static readonly Color C_TAB_P     = new Color(0.08f, 0.08f, 0.10f, 1f);
    static readonly Color C_TEXT      = new Color(0.88f, 0.88f, 0.92f, 1f);
    static readonly Color C_TEXT_DIM  = new Color(0.55f, 0.55f, 0.60f, 1f);
    static readonly Color C_TITLE     = new Color(0.68f, 0.74f, 0.90f, 1f);
    static readonly Color C_PROS      = new Color(0.28f, 0.88f, 0.45f, 1f);
    static readonly Color C_CONS      = new Color(0.95f, 0.40f, 0.25f, 1f);
    static readonly Color C_STAT_VAL  = new Color(0.55f, 0.82f, 1.00f, 1f);
    static readonly Color C_CLOSE     = new Color(0.28f, 0.10f, 0.10f, 1f);
    static readonly Color C_CLOSE_H   = new Color(0.44f, 0.14f, 0.14f, 1f);
    static readonly Color C_CLOSE_P   = new Color(0.10f, 0.05f, 0.05f, 1f);

    // ── Dane kart ─────────────────────────────────────────────────────────
    struct StatRow  { public string Label; public string Value; }
    struct CardData
    {
        public string    Name;
        public string    Subtitle;
        public Color     Accent;
        public StatRow[] Stats;
        public string[]  Pros;
        public string[]  Cons;
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (Instance != this) return;
        BuildUI();
        _overlay.SetActive(false);
    }

    // ── Public API ────────────────────────────────────────────────────────

    public void OpenTutorial()
    {
        _overlay.SetActive(true);
        ShowTab(_tabNames[0]);
    }

    public void CloseTutorial()
    {
        _overlay.SetActive(false);
    }

    // ── Budowanie canvas ──────────────────────────────────────────────────

    void BuildUI()
    {
        var cGO = new GameObject("TutorialCanvas");
        DontDestroyOnLoad(cGO);
        _canvas = cGO.AddComponent<Canvas>();
        _canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 150;

        var sc = cGO.AddComponent<CanvasScaler>();
        sc.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920f, 1080f);
        sc.matchWidthOrHeight  = 0.45f;
        cGO.AddComponent<GraphicRaycaster>();

        // Ciemny overlay — cały ekran
        _overlay = UIHelper.MakeImage("TutorialOverlay", cGO.transform, C_BG);
        UIHelper.Stretch(_overlay.GetComponent<RectTransform>());

        // Panel główny — 94% szerokości, 92% wysokości
        _panel = UIHelper.MakeImage("TutorialPanel", _overlay.transform, C_PANEL);
        var pRT = _panel.GetComponent<RectTransform>();
        pRT.anchorMin = new Vector2(0.03f, 0.04f);
        pRT.anchorMax = new Vector2(0.97f, 0.96f);
        pRT.pivot     = new Vector2(0.5f, 0.5f);
        pRT.offsetMin = pRT.offsetMax = Vector2.zero;

        BuildHeader();
        BuildTabBar();
        BuildContentArea();
    }

    // ── Nagłówek ──────────────────────────────────────────────────────────

    void BuildHeader()
    {
        var bar = UIHelper.MakeImage("TitleBar", _panel.transform, new Color(0.07f, 0.07f, 0.10f));
        var bRT = bar.GetComponent<RectTransform>();
        bRT.anchorMin        = new Vector2(0f, 1f);
        bRT.anchorMax        = new Vector2(1f, 1f);
        bRT.pivot            = new Vector2(0.5f, 1f);
        bRT.anchoredPosition = Vector2.zero;
        bRT.sizeDelta        = new Vector2(0f, 58f);

        var hlg = bar.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment         = TextAnchor.MiddleLeft;
        hlg.childForceExpandHeight  = true;
        hlg.childControlHeight      = true;
        hlg.childForceExpandWidth   = false;
        hlg.childControlWidth       = true;
        hlg.padding = new RectOffset(22, 14, 0, 0);
        hlg.spacing = 14f;

        // Ikona (kolorowy pasek po lewej)
        var accentBar = new GameObject("AccentStrip");
        accentBar.transform.SetParent(bar.transform, false);
        var aLE = accentBar.AddComponent<LayoutElement>();
        aLE.preferredWidth = 5f; aLE.flexibleWidth = 0f;
        accentBar.AddComponent<Image>().color = C_TITLE;

        // Tytuł
        var titleGO = new GameObject("Title");
        titleGO.transform.SetParent(bar.transform, false);
        titleGO.AddComponent<LayoutElement>().flexibleWidth = 1f;
        var tTMP = titleGO.AddComponent<TextMeshProUGUI>();
        tTMP.text      = "SAMOUCZEK";
        tTMP.fontSize  = 24f;
        tTMP.color     = C_TITLE;
        tTMP.fontStyle = FontStyles.Bold;
        tTMP.alignment = TextAlignmentOptions.MidlineLeft;

        // Podtytuł
        var subGO = new GameObject("Sub");
        subGO.transform.SetParent(bar.transform, false);
        subGO.AddComponent<LayoutElement>().flexibleWidth = 2f;
        var sTMP = subGO.AddComponent<TextMeshProUGUI>();
        sTMP.text      = "Pojazdy · Wieże · Mechaniki gry";
        sTMP.fontSize  = 14f;
        sTMP.color     = C_TEXT_DIM;
        sTMP.fontStyle = FontStyles.Italic;
        sTMP.alignment = TextAlignmentOptions.MidlineRight;

        // Przycisk zamknij
        UIHelper.MakeButton(bar.transform, "✕  Zamknij", C_CLOSE, C_CLOSE_H, C_CLOSE_P,
            Color.white, CloseTutorial, height: 40f);

        // Separator poziomy
        var sep = UIHelper.MakeImage("HSep", _panel.transform, C_SEP);
        var sRT = sep.GetComponent<RectTransform>();
        sRT.anchorMin        = new Vector2(0f, 1f);
        sRT.anchorMax        = new Vector2(1f, 1f);
        sRT.pivot            = new Vector2(0.5f, 1f);
        sRT.anchoredPosition = new Vector2(0f, -58f);
        sRT.sizeDelta        = new Vector2(0f, 2f);
    }

    // ── Pasek zakładek ────────────────────────────────────────────────────

    void BuildTabBar()
    {
        var bar = new GameObject("TabBar");
        bar.transform.SetParent(_panel.transform, false);
        var bRT = bar.AddComponent<RectTransform>();
        bRT.anchorMin        = new Vector2(0f, 1f);
        bRT.anchorMax        = new Vector2(1f, 1f);
        bRT.pivot            = new Vector2(0.5f, 1f);
        bRT.anchoredPosition = new Vector2(0f, -60f);
        bRT.sizeDelta        = new Vector2(0f, 46f);

        var hlg = bar.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing               = 3f;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth      = true;
        hlg.childControlHeight     = true;
        hlg.padding = new RectOffset(14, 14, 4, 0);

        foreach (var t in _tabNames)
        {
            string cap = t;
            var btn = UIHelper.MakeButton(bar.transform, cap,
                C_TAB_OFF, C_TAB_H, C_TAB_P, C_TEXT,
                () => ShowTab(cap), height: 42f);
            _tabBtns[t] = btn;
        }

        // Separator pod zakładkami
        var sep = UIHelper.MakeImage("TabSep", _panel.transform, C_SEP);
        var sRT = sep.GetComponent<RectTransform>();
        sRT.anchorMin        = new Vector2(0f, 1f);
        sRT.anchorMax        = new Vector2(1f, 1f);
        sRT.pivot            = new Vector2(0.5f, 1f);
        sRT.anchoredPosition = new Vector2(0f, -106f);
        sRT.sizeDelta        = new Vector2(0f, 2f);
    }

    // ── Obszar treści ─────────────────────────────────────────────────────

    void BuildContentArea()
    {
        var area = new GameObject("TabArea");
        area.transform.SetParent(_panel.transform, false);
        var aRT = area.AddComponent<RectTransform>();
        aRT.anchorMin = new Vector2(0f, 0f);
        aRT.anchorMax = new Vector2(1f, 1f);
        aRT.offsetMin = new Vector2(0f, 0f);
        aRT.offsetMax = new Vector2(0f, -108f);

        BuildVehiclesTab(area.transform);
        BuildTowersTab(area.transform);
        BuildMechanicsTab(area.transform);
    }

    // ── Scroll wrapper ────────────────────────────────────────────────────

    Transform MakeScrollContent(string name, Transform parent)
    {
        var scrollGO = new GameObject(name + "_Scroll");
        scrollGO.transform.SetParent(parent, false);
        UIHelper.Stretch(scrollGO.AddComponent<RectTransform>());
        var scroll    = scrollGO.AddComponent<ScrollRect>();
        scroll.horizontal = false;

        var viewGO = new GameObject("Viewport");
        viewGO.transform.SetParent(scrollGO.transform, false);
        UIHelper.Stretch(viewGO.AddComponent<RectTransform>());
        viewGO.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f);
        viewGO.AddComponent<Mask>().showMaskGraphic = false;
        scroll.viewport = viewGO.GetComponent<RectTransform>();

        var contentGO = new GameObject("Content");
        contentGO.transform.SetParent(viewGO.transform, false);
        var cRT       = contentGO.AddComponent<RectTransform>();
        cRT.anchorMin = new Vector2(0f, 1f);
        cRT.anchorMax = new Vector2(1f, 1f);
        cRT.pivot     = new Vector2(0.5f, 1f);
        cRT.sizeDelta = Vector2.zero;
        scroll.content = cRT;

        var vlg = contentGO.AddComponent<VerticalLayoutGroup>();
        vlg.spacing               = 10f;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth      = true;
        vlg.childControlHeight     = true;
        vlg.padding = new RectOffset(16, 16, 14, 14);

        contentGO.AddComponent<ContentSizeFitter>().verticalFit =
            ContentSizeFitter.FitMode.PreferredSize;

        return contentGO.transform;
    }

    // ══ ZAKŁADKA: POJAZDY ═════════════════════════════════════════════════

    void BuildVehiclesTab(Transform area)
    {
        var tab = new GameObject("VehiclesTab");
        tab.transform.SetParent(area, false);
        UIHelper.Stretch(tab.AddComponent<RectTransform>());
        _tabContents[_tabNames[0]] = tab;

        var content = MakeScrollContent("Vehicles", tab.transform);

        BuildCard(content, new CardData
        {
            Name     = "Wóz Podstawowy",
            Subtitle = "Jednostka ogólnego przeznaczenia",
            Accent   = new Color(0.28f, 0.52f, 0.82f),
            Stats = new[]
            {
                new StatRow { Label = "Punkty życia",  Value = "120 HP" },
                new StatRow { Label = "Prędkość",      Value = "4.5 m/s" },
                new StatRow { Label = "Pancerz",       Value = "10" },
            },
            Pros = new[]
            {
                "Niedrogi — idealny do wypełnienia kolejki",
                "Solidne statystyki bez wyraźnych słabości",
                "Pancerz 10 skutecznie redukuje drobne obrażenia",
            },
            Cons = new[]
            {
                "Brak zdolności specjalnych",
                "Wieże zawsze go widzą — żadnej ukrytości",
                "Nie przyciąga uwagi wież od innych jednostek",
            },
        });

        BuildCard(content, new CardData
        {
            Name     = "Wóz Tank",
            Subtitle = "Pancerny prowokator — taunt skupia ogień wież na nim",
            Accent   = new Color(0.28f, 0.72f, 0.38f),
            Stats = new[]
            {
                new StatRow { Label = "Punkty życia",  Value = "200 HP" },
                new StatRow { Label = "Prędkość",      Value = "1.5 m/s" },
                new StatRow { Label = "Pancerz",       Value = "30" },
            },
            Pros = new[]
            {
                "Taunt: wieże MUSZĄ strzelać do Tanka zamiast innych",
                "Najwyższe HP i pancerz — bardzo wytrzymały",
                "Idealny ekran dla szybkich lub delikatnych jednostek",
            },
            Cons = new[]
            {
                "Wyjątkowo wolny (1.5 m/s) — sam nie pokona mapy",
                "Drogi — duże obciążenie dla złota",
                "Wieża Plazmowa miażdży powolne cele (×4 obrażeń)",
            },
        });

        BuildCard(content, new CardData
        {
            Name     = "Wóz Dalekosiężny",
            Subtitle = "Artyleria — jedyna jednostka aktywnie niszcząca wieże",
            Accent   = new Color(0.92f, 0.60f, 0.18f),
            Stats = new[]
            {
                new StatRow { Label = "Punkty życia",    Value = "60 HP" },
                new StatRow { Label = "Prędkość",        Value = "3.5 m/s" },
                new StatRow { Label = "Pancerz",         Value = "0" },
                new StatRow { Label = "Obrażenia",       Value = "40 / strzał" },
                new StatRow { Label = "Zasięg ataku",    Value = "22 m" },
                new StatRow { Label = "Cooldown",        Value = "2.5 s" },
            },
            Pros = new[]
            {
                "Aktywnie niszczy wieże — skraca mapę dla reszty",
                "Wbudowana odporność — otrzymuje zmniejszone obrażenia",
                "Strzela podczas ruchu — nie zatrzymuje się",
            },
            Cons = new[]
            {
                "Niskie HP (60) i brak pancerza — łatwy do zestrzelenia",
                "Wieża Sonar niweluje odporność na obrażenia",
                "Wymaga prefabu pocisku przypisanego w Inspektorze",
            },
        });

        BuildCard(content, new CardData
        {
            Name     = "Wóz Zasadzka",
            Subtitle = "Kamikaze — niewidoczny, wybucha przy kontakcie z wieżą",
            Accent   = new Color(0.85f, 0.26f, 0.26f),
            Stats = new[]
            {
                new StatRow { Label = "Punkty życia",       Value = "80 HP" },
                new StatRow { Label = "Prędkość",           Value = "8 m/s  (najszybszy!)" },
                new StatRow { Label = "Pancerz",            Value = "0" },
                new StatRow { Label = "Obrażenia wybuchu",  Value = "150 AoE" },
                new StatRow { Label = "Promień eksplozji",  Value = "6 m" },
            },
            Pros = new[]
            {
                "Niewidoczny dla wież gdy brak aktywnego Sonara",
                "Najszybsza jednostka w grze — 8 m/s",
                "Ogromny wybuch AoE niszczy sąsiadujące wieże",
            },
            Cons = new[]
            {
                "Jednorazowy — ginie przy wybuchu",
                "Wieża Sonar ujawnia go — traci ochronę stealth",
                "Bez pancerza i widoczny = szybko eliminowany",
            },
        });

        BuildCard(content, new CardData
        {
            Name     = "Wóz Lustrzany",
            Subtitle = "Lustro — odbija pociski Podstawowej i Plazmowej z powrotem",
            Accent   = new Color(0.55f, 0.35f, 0.88f),
            Stats = new[]
            {
                new StatRow { Label = "Punkty życia",  Value = "90 HP" },
                new StatRow { Label = "Prędkość",      Value = "3 m/s" },
                new StatRow { Label = "Pancerz",       Value = "5" },
            },
            Pros = new[]
            {
                "Odbija pociski Wieży Podstawowej i Plazmowej do wieży",
                "Niewidoczny bez aktywnego Sonara",
                "Zmienia własne obrażenia wroga w broń przeciw niemu",
            },
            Cons = new[]
            {
                "Wieża Sonar ujawnia go i dezaktywuje stealth",
                "Nie odbija AoE Armatniej ani kolców",
                "Niskie HP — gdy widoczny szybko ginie",
            },
        });
    }

    // ══ ZAKŁADKA: WIEŻE ═══════════════════════════════════════════════════

    void BuildTowersTab(Transform area)
    {
        var tab = new GameObject("TowersTab");
        tab.transform.SetParent(area, false);
        UIHelper.Stretch(tab.AddComponent<RectTransform>());
        _tabContents[_tabNames[1]] = tab;

        var content = MakeScrollContent("Towers", tab.transform);

        BuildCard(content, new CardData
        {
            Name     = "Wieża Podstawowa",
            Subtitle = "Pojedynczy cel — priorytetuje Tanka",
            Accent   = new Color(0.28f, 0.52f, 0.82f),
            Stats = new[]
            {
                new StatRow { Label = "Tryb ataku",   Value = "Pojedynczy cel" },
                new StatRow { Label = "Priorytet",    Value = "Taunt → Pierwszy w kolejce" },
                new StatRow { Label = "Typ pocisku",  Value = "Prosty — odbijany przez Lustro" },
            },
            Pros = new[]
            {
                "Niezawodna — strzela do każdego pojazdu",
                "Skupia się na Tanku (taunt) — chroni strategię prowokacji",
                "Tania i efektywna na wczesnych rundach",
            },
            Cons = new[]
            {
                "Jeden cel naraz — słaba gdy wiele pojazdów jednocześnie",
                "Pocisk odbijany przez Wóz Lustrzany — ryzykujesz DMG do wieży",
                "Taunt Tanka zmusza ją do ignorowania reszty kolejki",
            },
        });

        BuildCard(content, new CardData
        {
            Name     = "Wieża Sonar",
            Subtitle = "Radar i wzmacniacz — max 2 na mapie",
            Accent   = new Color(0.20f, 0.80f, 0.82f),
            Stats = new[]
            {
                new StatRow { Label = "Tryb",           Value = "Debuff / Support" },
                new StatRow { Label = "Wzmocnienie",    Value = "×1.5 obrażeń dla wież w zasięgu" },
                new StatRow { Label = "Ujawnianie",     Value = "Kamikaze i Lustro w zasięgu" },
                new StatRow { Label = "Limit na mapie", Value = "Maksymalnie 2 sztuki" },
            },
            Pros = new[]
            {
                "Odkrywa stealth — kontra na Kamikaze i Lustro",
                "Pasywny debuff ×1.5 dla wszystkich pobliskich wież",
                "Kluczowa wieża synergiczna — wartość rośnie z innymi wieżami",
            },
            Cons = new[]
            {
                "Nie zadaje obrażeń bezpośrednio",
                "Twardy limit: max 2 na całą mapę",
                "Bezwartościowa gdy rywal nie używa stealth",
            },
        });

        BuildCard(content, new CardData
        {
            Name     = "Wieża Armatnia",
            Subtitle = "Działo — obrażenia obszarowe (AoE)",
            Accent   = new Color(0.90f, 0.48f, 0.16f),
            Stats = new[]
            {
                new StatRow { Label = "Tryb ataku",   Value = "Obszarowy (AoE)" },
                new StatRow { Label = "Typ pocisku",  Value = "Pocisk armatni — wybuch" },
                new StatRow { Label = "Zasięg AoE",   Value = "Promień wybuchu przy trafieniu" },
            },
            Pros = new[]
            {
                "Trafia wszystkie pojazdy w promieniu wybuchu naraz",
                "Idealna na zgrupowane lub powolne kolejki",
                "Obrażenia AoE nie blokuje taunt Tanka",
            },
            Cons = new[]
            {
                "Wolny cooldown — marnuje się na pojedyncze szybkie cele",
                "Wysoki koszt zakupu",
                "Słaba gdy pojazdy są rozłożone w czasie",
            },
        });

        BuildCard(content, new CardData
        {
            Name     = "Wieża Plazmowa",
            Subtitle = "Wiązka — mnożnik obrażeń rośnie z czasem: ×1 → ×4",
            Accent   = new Color(0.65f, 0.26f, 0.90f),
            Stats = new[]
            {
                new StatRow { Label = "Tryb ataku",        Value = "Wiązka ciągła" },
                new StatRow { Label = "Mnożnik obrażeń",   Value = "×1.0 → ×4.0 (ładuje się)" },
                new StatRow { Label = "Typ pocisku",       Value = "Plazma — odbijana przez Lustro" },
            },
            Pros = new[]
            {
                "Miażdżące obrażenia vs wolnych celów (Tank!)",
                "Mnożnik ×4 po kilku sekundach ciągłego ognia",
                "Najlepsza synergia z Wózem Tank",
            },
            Cons = new[]
            {
                "Pocisk odbijany przez Lustro — obrażenia do własnej wieży!",
                "Słaba vs szybkich jednostek — nie zdąży naładować mnożnika",
                "Jeden cel — traci wartość gdy szybki pojazd ucieknie",
            },
        });

        BuildCard(content, new CardData
        {
            Name     = "Wieża Kolcowa",
            Subtitle = "Pułapki pasywne — kolce rozmieszczone na ścieżce NavMesh",
            Accent   = new Color(0.80f, 0.75f, 0.18f),
            Stats = new[]
            {
                new StatRow { Label = "Tryb",              Value = "Pasywna pułapka (bez celowania)" },
                new StatRow { Label = "Obrażenia / kolec", Value = "30 HP" },
                new StatRow { Label = "Ilość kolców",      Value = "6 na wieżę" },
                new StatRow { Label = "Przeładowanie",     Value = "8 s po zużyciu wszystkich" },
                new StatRow { Label = "Rozmieszczenie",    Value = "Losowo na ścieżce NavMesh" },
            },
            Pros = new[]
            {
                "Całkowicie pasywna — nie wymaga celu ani animacji",
                "Kolce umieszczone na realnej ścieżce — zawsze trafią",
                "Nie blokowana przez taunt ani stealth",
            },
            Cons = new[]
            {
                "Przeładowuje dopiero gdy WSZYSTKIE 6 kolców zostanie zużytych",
                "Kamikaze (szybki) aktywuje jeden kolec i przemija bez strat",
                "Zasięg rozmieszczenia ograniczony do pobliskiej ścieżki",
            },
        });
    }

    // ══ ZAKŁADKA: MECHANIKI ═══════════════════════════════════════════════

    void BuildMechanicsTab(Transform area)
    {
        var tab = new GameObject("MechanicsTab");
        tab.transform.SetParent(area, false);
        UIHelper.Stretch(tab.AddComponent<RectTransform>());
        _tabContents[_tabNames[2]] = tab;

        var content = MakeScrollContent("Mechanics", tab.transform);

        BuildCard(content, new CardData
        {
            Name     = "System Taunt",
            Subtitle = "Wóz Tank zmusza wieże do skupienia ognia na sobie",
            Accent   = new Color(0.28f, 0.72f, 0.38f),
            Stats = new[]
            {
                new StatRow { Label = "Dotyczy",    Value = "Wóz Tank" },
                new StatRow { Label = "Efekt",      Value = "Wieże MUSZĄ priorytetować Tanka" },
                new StatRow { Label = "Wyjątki",    Value = "Wieża Armatnia (AoE omija taunt)" },
            },
            Pros = new[]
            {
                "Chroni całą resztę kolejki przed ogniem wież",
                "Synergia z Tarczą: nietykalny Tank blokuje wieże przez 4s",
                "Wieże marnują DPS na wytrzymały cel zamiast słabszych",
            },
            Cons = new[]
            {
                "Gdy Tank zginie, pozostałe jednostki są odkryte",
                "Wieża Armatnia ignoruje taunt — AoE uderza w wszystkich",
                "Wieża Plazmowa ładuje ×4 na powolnym Tanku — niebezpieczne",
            },
        });

        BuildCard(content, new CardData
        {
            Name     = "System Niewidzialności (Stealth)",
            Subtitle = "Kamikaze i Lustro są niewidoczne bez aktywnego Sonara",
            Accent   = new Color(0.55f, 0.35f, 0.88f),
            Stats = new[]
            {
                new StatRow { Label = "Dotyczy",     Value = "Kamikaze + Lustro" },
                new StatRow { Label = "Kontra",      Value = "Wieża Sonar (ujawnia w zasięgu)" },
                new StatRow { Label = "Warunek",     Value = "Aktywny Sonar = brak stealth" },
            },
            Pros = new[]
            {
                "Wieże całkowicie ignorują niewidoczne jednostki",
                "Kamikaze może dobiec do wieży i wybuchnąć bez obrażeń",
                "Lustro może odbijać pociski bez narażania się",
            },
            Cons = new[]
            {
                "Jedna aktywna Wieża Sonar niweluje całą niewidzialność",
                "Po odkryciu jednostki tracą główną zaletę — stają się słabe",
            },
        });

        BuildCard(content, new CardData
        {
            Name     = "System Pancerza",
            Subtitle = "Skuteczne obrażenia = max(1,  DMG − Pancerz)",
            Accent   = new Color(0.28f, 0.52f, 0.82f),
            Stats = new[]
            {
                new StatRow { Label = "Wzór",          Value = "DMG_eff = max(1, DMG − ARM)" },
                new StatRow { Label = "Minimum DMG",   Value = "Zawsze co najmniej 1 obrażenie" },
                new StatRow { Label = "Piercing",      Value = "Przebijające ignorują pancerz" },
            },
            Pros = new[]
            {
                "Pancerz 30 (Tank) mocno redukuje każdy strzał wieży",
                "Skuteczny kontra szybko strzelające wieże z niskim DMG",
            },
            Cons = new[]
            {
                "Obrażenia przebijające (piercing) całkowicie omijają pancerz",
                "Przy niskim pancerzu (Lustro=5) efekt praktycznie zerowy",
            },
        });

        BuildCard(content, new CardData
        {
            Name     = "Dekrety",
            Subtitle = "3 losowe ulepszenia po każdej przeżytej rundzie",
            Accent   = new Color(0.90f, 0.75f, 0.18f),
            Stats = new[]
            {
                new StatRow { Label = "Częstotliwość",  Value = "Po każdej ukończonej rundzie" },
                new StatRow { Label = "Czas na wybór",  Value = "30 sekund (auto = pierwszy)" },
                new StatRow { Label = "HP / Pancerz",   Value = "Bonusy procentowe (kumulują się)" },
                new StatRow { Label = "Prędkość",       Value = "Bonusy płaskie (+m/s)" },
            },
            Pros = new[]
            {
                "Stale wzmacniają Twoje jednostki między rundami",
                "Dekrety zdolności zwiększają efektywność Nalotu, Tarczy, Boosta",
                "Pula filtrowana do jednostek w Twojej kolejce",
            },
            Cons = new[]
            {
                "Tylko 3 losowe opcje — nie zawsze idealne dla strategii",
                "Brak wyboru po 30s = auto-pierwszy dekret",
                "W trybie MP każdy gracz losuje niezależnie",
            },
        });

        BuildCard(content, new CardData
        {
            Name     = "Zdolności Taktyczne",
            Subtitle = "Aktywne umiejętności kosztujące złoto w trakcie fazy ataku",
            Accent   = new Color(0.85f, 0.26f, 0.55f),
            Stats = new[]
            {
                new StatRow { Label = "Nalot",   Value = "100 zł → 100 DMG AoE, promień 8 m" },
                new StatRow { Label = "Tarcza",  Value = "75 zł  → 4 s nietykalność, promień 10 m" },
                new StatRow { Label = "Boost",   Value = "50 zł  → +2 m/s przez 10 s, promień 12 m" },
                new StatRow { Label = "Cel",     Value = "Klik myszą — wymaga potwierdzenia LPM" },
            },
            Pros = new[]
            {
                "Nalot eliminuje zgrupowane lub trudne wieże mid-run",
                "Tarcza chroni kolejkę przez krytyczny odcinek mapy",
                "Boost przepycha powolnego Tanka przez zasięg wież",
            },
            Cons = new[]
            {
                "Kosztują złoto — rywalizacja z zakupem kolejnych pojazdów",
                "Wymagają precyzyjnego kliknięcia w odpowiedni moment",
                "Dekrety mogą wzmocnić zdolności ale wymagają inwestycji",
            },
        });

        BuildCard(content, new CardData
        {
            Name     = "Tryb Multiplayer",
            Subtitle = "Każdy gracz atakuje mapę przeciwnika niezależnie",
            Accent   = new Color(0.22f, 0.78f, 0.82f),
            Stats = new[]
            {
                new StatRow { Label = "Gracze",         Value = "2 — każdy ma własną mapę" },
                new StatRow { Label = "Punkt życia",    Value = "3 HP na gracza" },
                new StatRow { Label = "Utrata HP",      Value = "−1 za każdy pojazd który ucieknie" },
                new StatRow { Label = "Duchy",          Value = "Kolejka przeciwnika widoczna jako duchy" },
            },
            Pros = new[]
            {
                "Duchy pokazują strategię wroga przed atakiem",
                "Synchronizacja rund — obaj kończą przed przejściem dalej",
                "Wcześniejszy start: obaj gotowi → 10 s odliczanie",
            },
            Cons = new[]
            {
                "Dekrety losowane niezależnie — asymetria między graczami",
                "Forfeit (ESC → Wyjście) od razu przyznaje wygraną przeciwnikowi",
            },
        });
    }

    // ══ Budowanie karty ═══════════════════════════════════════════════════

    void BuildCard(Transform parent, CardData d)
    {
        var card = UIHelper.MakeImage("Card_" + d.Name, parent, C_CARD);
        var outline = card.AddComponent<UnityEngine.UI.Outline>();
        outline.effectColor    = new Color(d.Accent.r, d.Accent.g, d.Accent.b, 0.40f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        var cardLE = card.AddComponent<LayoutElement>();
        cardLE.flexibleWidth = 1f;

        var vlg = card.AddComponent<VerticalLayoutGroup>();
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth      = true;
        vlg.childControlHeight     = true;

        // ── Nagłówek karty (kolorowe tło) ───────────────────────────────
        var hdr = UIHelper.MakeImage("CardHeader", card.transform, d.Accent);
        hdr.AddComponent<LayoutElement>().preferredHeight = 40f;

        var hhlg = hdr.AddComponent<HorizontalLayoutGroup>();
        hhlg.childAlignment        = TextAnchor.MiddleLeft;
        hhlg.childForceExpandHeight = true;
        hhlg.childControlHeight     = true;
        hhlg.childForceExpandWidth  = false;
        hhlg.childControlWidth      = true;
        hhlg.padding = new RectOffset(16, 14, 0, 0);
        hhlg.spacing = 16f;

        var nameGO = new GameObject("Name");
        nameGO.transform.SetParent(hdr.transform, false);
        nameGO.AddComponent<LayoutElement>().flexibleWidth = 1f;
        var nTMP = nameGO.AddComponent<TextMeshProUGUI>();
        nTMP.text      = d.Name;
        nTMP.fontSize  = 17f;
        nTMP.color     = Color.white;
        nTMP.fontStyle = FontStyles.Bold;
        nTMP.alignment = TextAlignmentOptions.MidlineLeft;

        if (!string.IsNullOrEmpty(d.Subtitle))
        {
            var subGO = new GameObject("Subtitle");
            subGO.transform.SetParent(hdr.transform, false);
            subGO.AddComponent<LayoutElement>().flexibleWidth = 2f;
            var sTMP = subGO.AddComponent<TextMeshProUGUI>();
            sTMP.text      = d.Subtitle;
            sTMP.fontSize  = 12f;
            sTMP.color     = new Color(1f, 1f, 1f, 0.72f);
            sTMP.fontStyle = FontStyles.Italic;
            sTMP.alignment = TextAlignmentOptions.MidlineRight;
        }

        // ── Ciało karty: Statystyki | separator | Zalety | separator | Wady
        var body = new GameObject("Body");
        body.transform.SetParent(card.transform, false);
        body.AddComponent<LayoutElement>().flexibleWidth = 1f;

        var bhlg = body.AddComponent<HorizontalLayoutGroup>();
        bhlg.childForceExpandHeight = false;
        bhlg.childForceExpandWidth  = false;
        bhlg.childControlHeight     = true;
        bhlg.childControlWidth      = true;
        bhlg.padding = new RectOffset(8, 8, 10, 12);
        bhlg.spacing = 0f;

        var statsCol = MakeColumn(body.transform, "STATYSTYKI", 220f);
        foreach (var s in d.Stats) AddStatRow(statsCol, s.Label, s.Value);

        AddVDivider(body.transform);

        var prosCol = MakeColumn(body.transform, "✔  ZALETY", -1f);
        foreach (var p in d.Pros) AddBulletRow(prosCol, p, C_PROS);

        AddVDivider(body.transform);

        var consCol = MakeColumn(body.transform, "✘  WADY", -1f);
        foreach (var c in d.Cons) AddBulletRow(consCol, c, C_CONS);
    }

    GameObject MakeColumn(Transform parent, string header, float fixedW)
    {
        var col = new GameObject("Col_" + header);
        col.transform.SetParent(parent, false);
        var le = col.AddComponent<LayoutElement>();
        if (fixedW > 0f) { le.minWidth = fixedW; le.preferredWidth = fixedW; le.flexibleWidth = 0f; }
        else le.flexibleWidth = 1f;

        var vlg = col.AddComponent<VerticalLayoutGroup>();
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth      = true;
        vlg.childControlHeight     = true;
        vlg.padding = new RectOffset(10, 10, 4, 4);
        vlg.spacing = 5f;

        var hdrGO = new GameObject("ColHdr");
        hdrGO.transform.SetParent(col.transform, false);
        hdrGO.AddComponent<LayoutElement>().preferredHeight = 20f;
        var hTMP = hdrGO.AddComponent<TextMeshProUGUI>();
        hTMP.text      = header;
        hTMP.fontSize  = 11f;
        hTMP.color     = C_TEXT_DIM;
        hTMP.fontStyle = FontStyles.Bold;
        hTMP.alignment = TextAlignmentOptions.MidlineLeft;

        return col;
    }

    void AddStatRow(GameObject col, string label, string value)
    {
        var row = new GameObject("SR_" + label);
        row.transform.SetParent(col.transform, false);
        row.AddComponent<LayoutElement>().preferredHeight = 22f;
        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth      = true;
        hlg.childControlHeight     = true;

        var lGO = new GameObject("L");
        lGO.transform.SetParent(row.transform, false);
        lGO.AddComponent<LayoutElement>().flexibleWidth = 1f;
        var lTMP = lGO.AddComponent<TextMeshProUGUI>();
        lTMP.text      = label;
        lTMP.fontSize  = 13f;
        lTMP.color     = C_TEXT_DIM;
        lTMP.alignment = TextAlignmentOptions.MidlineLeft;

        var vGO = new GameObject("V");
        vGO.transform.SetParent(row.transform, false);
        vGO.AddComponent<LayoutElement>().flexibleWidth = 1.2f;
        var vTMP = vGO.AddComponent<TextMeshProUGUI>();
        vTMP.text      = value;
        vTMP.fontSize  = 13f;
        vTMP.color     = C_STAT_VAL;
        vTMP.fontStyle = FontStyles.Bold;
        vTMP.alignment = TextAlignmentOptions.MidlineRight;
    }

    void AddBulletRow(GameObject col, string text, Color bulletColor)
    {
        var row = new GameObject("BR");
        row.transform.SetParent(col.transform, false);
        var le = row.AddComponent<LayoutElement>();
        le.flexibleWidth = 1f;

        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = false;
        hlg.childControlWidth      = true;
        hlg.childControlHeight     = true;
        hlg.spacing        = 6f;
        hlg.childAlignment = TextAnchor.UpperLeft;

        var dotGO = new GameObject("Dot");
        dotGO.transform.SetParent(row.transform, false);
        var dLE = dotGO.AddComponent<LayoutElement>();
        dLE.minWidth = 14f; dLE.preferredWidth = 14f; dLE.flexibleWidth = 0f;
        dLE.minHeight = 20f;
        var dTMP = dotGO.AddComponent<TextMeshProUGUI>();
        dTMP.text      = "•";
        dTMP.fontSize  = 15f;
        dTMP.color     = bulletColor;
        dTMP.alignment = TextAlignmentOptions.TopLeft;

        var txtGO = new GameObject("T");
        txtGO.transform.SetParent(row.transform, false);
        txtGO.AddComponent<LayoutElement>().flexibleWidth = 1f;
        var tTMP = txtGO.AddComponent<TextMeshProUGUI>();
        tTMP.text               = text;
        tTMP.fontSize           = 13f;
        tTMP.color              = C_TEXT;
        tTMP.alignment          = TextAlignmentOptions.TopLeft;
        tTMP.textWrappingMode = TextWrappingModes.Normal;
    }

    void AddVDivider(Transform parent)
    {
        var div = new GameObject("VDiv");
        div.transform.SetParent(parent, false);
        var le = div.AddComponent<LayoutElement>();
        le.minWidth = 2f; le.preferredWidth = 2f; le.flexibleWidth = 0f;
        le.flexibleHeight = 1f;
        div.AddComponent<Image>().color = C_SEP;
    }

    // ── Przełączanie zakładek ─────────────────────────────────────────────

    void ShowTab(string tab)
    {
        _activeTab = tab;
        foreach (var kv in _tabContents)
            kv.Value.SetActive(kv.Key == tab);

        foreach (var kv in _tabBtns)
        {
            var img = kv.Value.GetComponent<Image>();
            if (img != null) img.color = kv.Key == tab ? C_TAB_ON : C_TAB_OFF;
        }
    }
}
