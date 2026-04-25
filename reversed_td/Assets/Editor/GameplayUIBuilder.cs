using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

/// Narzędzie edytorskie – generuje kompletny interfejs rozgrywki.
/// Menu: Narzędzia AI → Wygeneruj Interfejs Rozgrywki
public class GameplayUIBuilder : EditorWindow
{
    // ── Paleta kolorów ────────────────────────────────────────────────────
    static readonly Color C_Panel      = new Color(0.10f, 0.12f, 0.16f, 0.97f);
    static readonly Color C_PanelLight = new Color(0.15f, 0.18f, 0.24f, 1.00f);
    static readonly Color C_Gold       = new Color(1.00f, 0.82f, 0.20f, 1.00f);
    static readonly Color C_Green      = new Color(0.18f, 0.78f, 0.35f, 1.00f);
    static readonly Color C_StartBtn   = new Color(0.12f, 0.65f, 0.28f, 1.00f);
    static readonly Color C_Red        = new Color(0.90f, 0.15f, 0.15f, 1.00f);
    static readonly Color C_BtnNormal  = new Color(0.22f, 0.27f, 0.36f, 1.00f);
    static readonly Color C_Text       = new Color(0.92f, 0.93f, 0.95f, 1.00f);

    [MenuItem("Narzędzia AI/Wygeneruj Interfejs Rozgrywki")]
    public static void Build()
    {
        // Usuń stary canvas jeśli istnieje
        var old = GameObject.Find("GameplayCanvas");
        if (old != null) DestroyImmediate(old);

        // ── Korzień Canvas ──────────────────────────────────────────────
        var canvasGO = new GameObject("GameplayCanvas");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight  = 0.5f;  // balans między W i H → lepsze na różnych proporcjach

        canvasGO.AddComponent<GraphicRaycaster>();

        // ── UIManager ────────────────────────────────────────────────────
        var uiManager = canvasGO.AddComponent<GameplayUIManager>();
        uiManager.config = LoadOrCreateConfig();

        // ════════════════════════════════════════════════════════════════
        //  DOLNY PANEL (20% wysokości)
        // ════════════════════════════════════════════════════════════════
        var bottomGO = MakePanel("BottomPanel", canvasGO.transform,
            new Vector2(0f, 0f), new Vector2(1f, 0.20f), C_Panel);

        // ── Strefa Sklepu (0-54%) ────────────────────────────────────────
        var shopGO = MakeArea("ShopArea", bottomGO.transform,
            new Vector2(0f, 0f), new Vector2(0.54f, 1f),
            new RectOffset(12, 8, 10, 10));

        var shopHLG = shopGO.AddComponent<HorizontalLayoutGroup>();
        shopHLG.spacing             = 10f;
        shopHLG.childControlWidth   = true;
        shopHLG.childControlHeight  = true;
        shopHLG.childForceExpandWidth  = true;
        shopHLG.childForceExpandHeight = true;

        for (int i = 0; i < 5; i++)
            uiManager.vehicleSlots[i] = MakeVehicleButton(shopGO.transform, i, uiManager.config);

        // ── Panel Kolejki (54-79%) ────────────────────────────────────────
        var queuePanelGO = MakeArea("QueuePanel", bottomGO.transform,
            new Vector2(0.54f, 0f), new Vector2(0.79f, 1f),
            new RectOffset(6, 6, 6, 6));

        var queuePanelBG = queuePanelGO.AddComponent<Image>();
        queuePanelBG.color = C_PanelLight;

        // Tytuł kolejki
        var queueTitle = MakeText("QueueTitle", queuePanelGO.transform,
            "KOLEJKA ATAKU", 13f, C_Gold, TextAlignmentOptions.Center, FontStyles.Bold);
        var titleRt = queueTitle.GetComponent<RectTransform>();
        titleRt.anchorMin  = new Vector2(0f, 0.82f);
        titleRt.anchorMax  = new Vector2(1f, 1.00f);
        titleRt.offsetMin  = new Vector2(4f, 0f);
        titleRt.offsetMax  = new Vector2(-4f, -2f);

        // ScrollRect
        var scrollGO = new GameObject("QueueScroll");
        scrollGO.transform.SetParent(queuePanelGO.transform, false);
        var scrollRt = scrollGO.AddComponent<RectTransform>();
        scrollRt.anchorMin = new Vector2(0f, 0f);
        scrollRt.anchorMax = new Vector2(1f, 0.82f);
        scrollRt.offsetMin = new Vector2(4f, 4f);
        scrollRt.offsetMax = new Vector2(-4f, 0f);
        scrollGO.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.3f);

        var scroll = scrollGO.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical   = true;
        scroll.scrollSensitivity = 20f;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        // Viewport
        var viewportGO = new GameObject("Viewport");
        viewportGO.transform.SetParent(scrollGO.transform, false);
        var viewportRt = viewportGO.AddComponent<RectTransform>();
        viewportRt.anchorMin = Vector2.zero;
        viewportRt.anchorMax = Vector2.one;
        viewportRt.offsetMin = Vector2.zero;
        viewportRt.offsetMax = Vector2.zero;
        viewportGO.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
        viewportGO.AddComponent<Mask>().showMaskGraphic = false;
        scroll.viewport = viewportRt;

        // Content (VerticalLayoutGroup + ContentSizeFitter)
        var contentGO = new GameObject("Content");
        contentGO.transform.SetParent(viewportGO.transform, false);
        var contentRt = contentGO.AddComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot     = new Vector2(0.5f, 1f);
        contentRt.offsetMin = Vector2.zero;
        contentRt.offsetMax = Vector2.zero;
        var vlg = contentGO.AddComponent<VerticalLayoutGroup>();
        vlg.spacing            = 4f;
        vlg.padding            = new RectOffset(3, 3, 3, 3);
        vlg.childControlWidth  = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        var csf = contentGO.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = contentRt;

        uiManager.queueContent = contentRt;

        // ── Panel Statystyk + Start (79-100%) ────────────────────────────
        var statsGO = MakeArea("StatsPanel", bottomGO.transform,
            new Vector2(0.79f, 0f), new Vector2(1f, 1f),
            new RectOffset(10, 12, 8, 8));

        var statsVLG = statsGO.AddComponent<VerticalLayoutGroup>();
        statsVLG.spacing             = 5f;
        statsVLG.childControlHeight  = true;
        statsVLG.childForceExpandHeight = true;
        statsVLG.childControlWidth   = true;
        statsVLG.childForceExpandWidth  = true;
        statsVLG.childAlignment      = TextAnchor.MiddleCenter;

        uiManager.phaseText = MakeStatText("PhaseText",  statsGO.transform, "◉ PLANOWANIE", 16f, C_Green);
        uiManager.roundText = MakeStatText("RoundText",  statsGO.transform, "Runda: 1",     15f, C_Text);
        uiManager.goldText  = MakeStatText("GoldText",   statsGO.transform, "Złoto: 200",   18f, C_Gold);

        // Start Button
        var startGO = new GameObject("StartButton");
        startGO.transform.SetParent(statsGO.transform, false);
        var startLE = startGO.AddComponent<LayoutElement>();
        startLE.preferredHeight  = 46f;
        startLE.flexibleHeight   = 0.5f;
        var startImg = startGO.AddComponent<Image>();
        startImg.color = C_StartBtn;
        var startBtn = startGO.AddComponent<Button>();
        startBtn.targetGraphic = startImg;
        var startColors = startBtn.colors;
        startColors.highlightedColor = new Color(0.22f, 0.85f, 0.40f);
        startColors.pressedColor     = new Color(0.08f, 0.45f, 0.18f);
        startColors.disabledColor    = new Color(0.22f, 0.32f, 0.26f);
        startBtn.colors = startColors;

        var startTxtGO = new GameObject("StartText");
        startTxtGO.transform.SetParent(startGO.transform, false);
        var startTxtRt = startTxtGO.AddComponent<RectTransform>();
        startTxtRt.anchorMin = Vector2.zero;
        startTxtRt.anchorMax = Vector2.one;
        startTxtRt.offsetMin = Vector2.zero;
        startTxtRt.offsetMax = Vector2.zero;
        var startTxt = startTxtGO.AddComponent<TextMeshProUGUI>();
        startTxt.text      = "START";
        startTxt.fontSize  = 18f;
        startTxt.fontStyle = FontStyles.Bold;
        startTxt.color     = Color.white;
        startTxt.alignment = TextAlignmentOptions.Center;

        uiManager.startButton     = startBtn;
        uiManager.startButtonText = startTxt;

        // ════════════════════════════════════════════════════════════════
        //  OVERLAY – Brak Złota (środek ekranu, nad panelem)
        // ════════════════════════════════════════════════════════════════
        var noMoneyGO = new GameObject("NoMoney_Overlay");
        noMoneyGO.transform.SetParent(canvasGO.transform, false);
        var nmRt = noMoneyGO.AddComponent<RectTransform>();
        nmRt.anchorMin = new Vector2(0.15f, 0.20f);
        nmRt.anchorMax = new Vector2(0.85f, 0.32f);
        nmRt.offsetMin = Vector2.zero;
        nmRt.offsetMax = Vector2.zero;

        var nmBg = noMoneyGO.AddComponent<Image>();
        nmBg.color = new Color(0.55f, 0.05f, 0.05f, 0.88f);

        var nmTxtGO = new GameObject("NoMoneyText");
        nmTxtGO.transform.SetParent(noMoneyGO.transform, false);
        var nmTxtRt = nmTxtGO.AddComponent<RectTransform>();
        nmTxtRt.anchorMin = Vector2.zero;
        nmTxtRt.anchorMax = Vector2.one;
        nmTxtRt.offsetMin = new Vector2(8f, 4f);
        nmTxtRt.offsetMax = new Vector2(-8f, -4f);
        var nmTxt = nmTxtGO.AddComponent<TextMeshProUGUI>();
        nmTxt.text      = "✕  Brak złota!";
        nmTxt.fontSize  = 22f;
        nmTxt.fontStyle = FontStyles.Bold;
        nmTxt.color     = Color.white;
        nmTxt.alignment = TextAlignmentOptions.Center;

        uiManager.noMoneyText = nmTxt;
        noMoneyGO.SetActive(false);

        // ── Zapisz ──────────────────────────────────────────────────────
        Selection.activeGameObject = canvasGO;
        Debug.Log("[GameplayUIBuilder] Interfejs wygenerowany pomyślnie!");
    }

    // ══ HELPERS ═══════════════════════════════════════════════════════════

    static GameConfig LoadOrCreateConfig()
    {
        var cfg = AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/UstawieniaEkonomii.asset");
        if (cfg != null) return cfg;

        cfg = ScriptableObject.CreateInstance<GameConfig>();
        cfg.startingGold         = 200;
        cfg.goldPerWin           = 100;
        cfg.goldPerEscapedVehicle = 10;
        cfg.vehicles = new VehicleConfig[5]
        {
            new VehicleConfig { vehicleName = "Czołg",       cost = 60 },
            new VehicleConfig { vehicleName = "Artyleria",   cost = 40 },
            new VehicleConfig { vehicleName = "Lustrzany",   cost = 50 },
            new VehicleConfig { vehicleName = "Kamikaze",    cost = 35 },
            new VehicleConfig { vehicleName = "Podstawowy",  cost = 25 },
        };
        AssetDatabase.CreateAsset(cfg, "Assets/UstawieniaEkonomii.asset");
        AssetDatabase.SaveAssets();
        return cfg;
    }

    static GameObject MakePanel(string name, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        go.AddComponent<Image>().color = color;
        return go;
    }

    static GameObject MakeArea(string name, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax, RectOffset padding)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = new Vector2(padding.left,   padding.bottom);
        rt.offsetMax = new Vector2(-padding.right, -padding.top);
        return go;
    }

    static GameplayUIManager.VehicleUISlot MakeVehicleButton(
        Transform parent, int index, GameConfig cfg)
    {
        var btnGO = new GameObject($"Slot_{index}");
        btnGO.transform.SetParent(parent, false);
        var bg  = btnGO.AddComponent<Image>();
        bg.color = C_BtnNormal;
        var btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = bg;
        var btnColors = btn.colors;
        btnColors.highlightedColor = new Color(0.30f, 0.38f, 0.52f);
        btnColors.pressedColor     = new Color(0.12f, 0.15f, 0.22f);
        btn.colors = btnColors;

        // Ikona
        var iconGO = new GameObject("Icon");
        iconGO.transform.SetParent(btnGO.transform, false);
        var iconRt = iconGO.AddComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0.08f, 0.38f);
        iconRt.anchorMax = new Vector2(0.92f, 0.95f);
        iconRt.offsetMin = iconRt.offsetMax = Vector2.zero;
        var iconImg = iconGO.AddComponent<Image>();
        iconImg.color = new Color(0.45f, 0.48f, 0.55f);
        iconImg.preserveAspect = true;

        // Nazwa
        var nameGO = new GameObject("Name");
        nameGO.transform.SetParent(btnGO.transform, false);
        var nameRt = nameGO.AddComponent<RectTransform>();
        nameRt.anchorMin = new Vector2(0f, 0.18f);
        nameRt.anchorMax = new Vector2(1f, 0.38f);
        nameRt.offsetMin = new Vector2(3f, 0f);
        nameRt.offsetMax = new Vector2(-3f, 0f);
        var nameTxt = nameGO.AddComponent<TextMeshProUGUI>();
        nameTxt.text             = cfg != null && index < cfg.vehicles.Length
                                   ? cfg.vehicles[index].vehicleName : $"Slot {index + 1}";
        nameTxt.fontSize         = 13f;
        nameTxt.enableAutoSizing = true;
        nameTxt.fontSizeMin      = 8f;
        nameTxt.fontSizeMax      = 13f;
        nameTxt.color            = C_Text;
        nameTxt.alignment        = TextAlignmentOptions.Center;
        nameTxt.overflowMode     = TextOverflowModes.Ellipsis;

        // Koszt
        var costGO = new GameObject("Cost");
        costGO.transform.SetParent(btnGO.transform, false);
        var costRt = costGO.AddComponent<RectTransform>();
        costRt.anchorMin = new Vector2(0f, 0f);
        costRt.anchorMax = new Vector2(1f, 0.18f);
        costRt.offsetMin = new Vector2(3f, 2f);
        costRt.offsetMax = new Vector2(-3f, 0f);
        var costTxt = costGO.AddComponent<TextMeshProUGUI>();
        costTxt.text      = cfg != null && index < cfg.vehicles.Length
                            ? $"{cfg.vehicles[index].cost} Z" : "? Z";
        costTxt.fontSize  = 13f;
        costTxt.color     = C_Gold;
        costTxt.fontStyle = FontStyles.Bold;
        costTxt.alignment = TextAlignmentOptions.Center;

        return new GameplayUIManager.VehicleUISlot
        {
            button    = btn,
            iconImage = iconImg,
            nameText  = nameTxt,
            costText  = costTxt,
        };
    }

    static TextMeshProUGUI MakeStatText(string name, Transform parent,
        string text, float size, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var le = go.AddComponent<LayoutElement>();
        le.flexibleHeight = 1f;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.color     = color;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        return tmp;
    }

    static TextMeshProUGUI MakeText(string name, Transform parent,
        string text, float size, Color color,
        TextAlignmentOptions align, FontStyles style = FontStyles.Normal)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.color     = color;
        tmp.fontStyle = style;
        tmp.alignment = align;
        return tmp;
    }
}
