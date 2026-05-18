using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// Generuje pełną hierarchię Canvas dla SettingsManager i EscMenuManager
/// bezpośrednio w scenie — widoczną i edytowalną w Hierarchy/Inspektorze.
/// Menu Unity → Generator → …
public static class UIGeneratorEditor
{
    // ── Kolory (identyczne z SettingsManager / EscMenuManager) ────────────

    static readonly Color C_BG        = new Color(0.08f, 0.08f, 0.10f, 0.96f);
    static readonly Color C_PANEL      = new Color(0.12f, 0.12f, 0.15f, 1f);
    static readonly Color C_PANEL2     = new Color(0.16f, 0.16f, 0.20f, 1f);
    static readonly Color C_SEP        = new Color(0.28f, 0.28f, 0.34f, 1f);
    static readonly Color C_TAB_OFF    = new Color(0.15f, 0.15f, 0.19f, 1f);
    static readonly Color C_TAB_H      = new Color(0.20f, 0.22f, 0.30f, 1f);
    static readonly Color C_BTN        = new Color(0.20f, 0.20f, 0.24f, 1f);
    static readonly Color C_BTN_H      = new Color(0.28f, 0.28f, 0.34f, 1f);
    static readonly Color C_BTN_P      = new Color(0.10f, 0.10f, 0.12f, 1f);
    static readonly Color C_BIND_BTN   = new Color(0.18f, 0.22f, 0.30f, 1f);
    static readonly Color C_SAVE       = new Color(0.15f, 0.30f, 0.18f, 1f);
    static readonly Color C_SAVE_H     = new Color(0.20f, 0.42f, 0.24f, 1f);
    static readonly Color C_TEXT       = new Color(0.88f, 0.88f, 0.91f, 1f);
    static readonly Color C_TEXT_DIM   = new Color(0.62f, 0.62f, 0.66f, 1f);
    static readonly Color C_TITLE      = new Color(0.68f, 0.74f, 0.86f, 1f);
    static readonly Color C_SLIDER_BG  = new Color(0.20f, 0.20f, 0.24f, 1f);
    static readonly Color C_SLIDER_FILL= new Color(0.35f, 0.50f, 0.72f, 1f);

    static readonly Color C_OVERLAY_ESC   = new Color(0f, 0f, 0f, 0.72f);
    static readonly Color C_PANEL_ESC     = new Color(0.11f, 0.11f, 0.13f, 1f);
    static readonly Color C_BTN_ESC       = new Color(0.18f, 0.18f, 0.22f, 1f);
    static readonly Color C_BTN_H_ESC     = new Color(0.26f, 0.26f, 0.32f, 1f);
    static readonly Color C_BTN_QUIT      = new Color(0.30f, 0.10f, 0.10f, 1f);
    static readonly Color C_BTN_QUIT_H    = new Color(0.44f, 0.14f, 0.14f, 1f);

    static readonly string[] TAB_NAMES = { "DŹWIĘK", "GRAFIKA", "STEROWANIE" };

    // ════════════════════════════════════════════════════════════════════════
    //  MENU ITEMS
    // ════════════════════════════════════════════════════════════════════════

    [MenuItem("Generator/Generuj Canvas — Ustawienia")]
    static void GenSettings()
    {
        if (!ConfirmReplace("SettingsCanvas")) return;
        EnsureEditorEventSystem();
        var go = BuildSettingsCanvas();
        Undo.RegisterCreatedObjectUndo(go, "Generuj Canvas Ustawień");
        Selection.activeGameObject = go;
        EditorSceneManager.MarkSceneDirty(go.scene);
        Debug.Log("[Generator] SettingsCanvas wygenerowany w scenie.");
    }

    [MenuItem("Generator/Generuj Canvas — Ustawienia", true)]
    static bool GenSettingsV() => !Application.isPlaying;

    [MenuItem("Generator/Generuj Canvas — ESC Menu")]
    static void GenEsc()
    {
        if (!ConfirmReplace("EscMenuCanvas")) return;
        var go = BuildEscMenuCanvas();
        Undo.RegisterCreatedObjectUndo(go, "Generuj Canvas ESC Menu");
        Selection.activeGameObject = go;
        EditorSceneManager.MarkSceneDirty(go.scene);
        Debug.Log("[Generator] EscMenuCanvas wygenerowany w scenie.");
    }

    [MenuItem("Generator/Generuj Canvas — ESC Menu", true)]
    static bool GenEscV() => !Application.isPlaying;

    [MenuItem("Generator/Generuj Oba Canvas")]
    static void GenBoth() { GenSettings(); GenEsc(); }

    [MenuItem("Generator/Generuj Oba Canvas", true)]
    static bool GenBothV() => !Application.isPlaying;

    // ════════════════════════════════════════════════════════════════════════
    //  SETTINGS CANVAS
    // ════════════════════════════════════════════════════════════════════════

    static GameObject BuildSettingsCanvas()
    {
        var root = new GameObject("SettingsCanvas");

        var c = root.AddComponent<Canvas>();
        c.renderMode   = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 200;

        var cs = root.AddComponent<CanvasScaler>();
        cs.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920f, 1080f);
        cs.matchWidthOrHeight  = 0.5f;

        root.AddComponent<GraphicRaycaster>();

        // Ciemne tło (overlay)
        var overlay = MkImg("SettingsOverlay", root.transform, C_BG);
        Stretch(overlay.GetComponent<RectTransform>());

        // Główny panel
        var panel = MkImg("SettingsPanel", overlay.transform, C_PANEL);
        var pRT   = panel.GetComponent<RectTransform>();
        pRT.anchorMin = pRT.anchorMax = pRT.pivot = new Vector2(0.5f, 0.5f);
        pRT.sizeDelta = new Vector2(900f, 660f);

        BuildSHeader(panel.transform);
        BuildSTabBar(panel.transform);
        BuildSContent(panel.transform);
        BuildSFooter(panel.transform);

        return root;
    }

    // ── Nagłówek ───────────────────────────────────────────────────────────

    static void BuildSHeader(Transform panel)
    {
        var t = MkTxt("Title", panel, "USTAWIENIA", 24f, C_TITLE, FontStyles.Bold)
                    .GetComponent<RectTransform>();
        t.anchorMin = new Vector2(0f, 1f); t.anchorMax = new Vector2(1f, 1f);
        t.pivot     = new Vector2(0.5f, 1f);
        t.anchoredPosition = new Vector2(0f, -20f);
        t.sizeDelta = new Vector2(0f, 42f);

        var s = MkImg("HSep", panel, C_SEP).GetComponent<RectTransform>();
        s.anchorMin = new Vector2(0.04f, 1f); s.anchorMax = new Vector2(0.96f, 1f);
        s.pivot     = new Vector2(0.5f, 1f);
        s.anchoredPosition = new Vector2(0f, -64f);
        s.sizeDelta = new Vector2(0f, 2f);
    }

    // ── Pasek zakładek ─────────────────────────────────────────────────────

    static readonly string[] TAB_IDS = { "Sound", "Graphics", "Controls" };

    static void BuildSTabBar(Transform panel)
    {
        var bar = new GameObject("TabBar");
        bar.transform.SetParent(panel, false);
        var bRT = bar.AddComponent<RectTransform>();
        bRT.anchorMin = new Vector2(0.04f, 1f); bRT.anchorMax = new Vector2(0.96f, 1f);
        bRT.pivot     = new Vector2(0f, 1f);
        bRT.anchoredPosition = new Vector2(0f, -68f);
        bRT.sizeDelta = new Vector2(0f, 40f);

        var hlg = bar.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 6f;
        hlg.childForceExpandWidth = hlg.childForceExpandHeight = true;
        hlg.childControlWidth = hlg.childControlHeight = true;

        for (int i = 0; i < TAB_NAMES.Length; i++)
            MkBtn(bar.transform, "Btn_" + TAB_IDS[i], TAB_NAMES[i], C_TAB_OFF, C_TAB_H, C_BTN_P, C_TEXT, 40f);
    }

    // ── Obszar treści ──────────────────────────────────────────────────────

    static void BuildSContent(Transform panel)
    {
        var area = new GameObject("ContentArea");
        area.transform.SetParent(panel, false);
        var aRT = area.AddComponent<RectTransform>();
        aRT.anchorMin = new Vector2(0.04f, 0.12f);
        aRT.anchorMax = new Vector2(0.96f, 0.82f);
        aRT.offsetMin = aRT.offsetMax = Vector2.zero;

        BuildSoundTab(area.transform);
        BuildGraphicsTab(area.transform);
        BuildControlsTab(area.transform);
    }

    // ── Zakładka DŹWIĘK ────────────────────────────────────────────────────

    static void BuildSoundTab(Transform parent)
    {
        var p = MkTabPanel("SoundTab", parent);

        var vlg = p.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 14f;
        vlg.childAlignment    = TextAnchor.UpperLeft;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.padding = new RectOffset(0, 0, 10, 0);

        MkSliderRow(p.transform, "MusicVol",  "Głośność muzyki",  1f);
        MkSliderRow(p.transform, "SFXVol",    "Głośność efektów", 1f);
    }

    // ── Zakładka GRAFIKA ───────────────────────────────────────────────────

    static void BuildGraphicsTab(Transform parent)
    {
        var p = MkTabPanel("GraphicsTab", parent);
        p.SetActive(false);

        var vlg = p.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 14f;
        vlg.childAlignment    = TextAnchor.UpperLeft;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.padding = new RectOffset(0, 0, 10, 0);

        // Rozdzielczość (pełny TMP_Dropdown — opcje ładowane w Play Mode przez AdoptAndPopulateDropdown)
        var rowRes = MkRow("RowRes", p.transform, 54f);
        MkRowLbl(rowRes.transform, "Rozdzielczość");
        BuildEditorDropdown(rowRes.transform);

        // Pełny ekran (checkbox kwadratowy)
        MkToggleRow(p.transform, "Fullscreen", "Pełny ekran");

        // Jasność
        MkSliderRow(p.transform, "Brightness", "Przyciemnienie ekranu", 0f);
    }

    // ── Zakładka STEROWANIE ────────────────────────────────────────────────

    static void BuildControlsTab(Transform parent)
    {
        var p = MkTabPanel("ControlsTab", parent);
        p.SetActive(false);

        // Przycisk reset na dole
        var rGO = new GameObject("ResetBtn");
        rGO.transform.SetParent(p.transform, false);
        var rRT = rGO.AddComponent<RectTransform>();
        rRT.anchorMin = new Vector2(0.15f, 0f);
        rRT.anchorMax = new Vector2(0.85f, 0f);
        rRT.pivot     = new Vector2(0.5f, 0f);
        rRT.anchoredPosition = new Vector2(0f, 4f);
        rRT.sizeDelta = new Vector2(0f, 32f);
        rGO.AddComponent<Image>().color = C_BTN;
        rGO.AddComponent<Button>();
        var rL = MkTxt("Label", rGO.transform, "Przywróć domyślne", 13f, C_TEXT, FontStyles.Bold);
        Stretch(rL.GetComponent<RectTransform>());

        // Scroll z listą bindów
        var scrollGO = new GameObject("ControlsScroll");
        scrollGO.transform.SetParent(p.transform, false);
        var sRT2 = scrollGO.AddComponent<RectTransform>();
        sRT2.anchorMin = Vector2.zero;
        sRT2.anchorMax = Vector2.one;
        sRT2.offsetMin = new Vector2(0f, 42f);
        sRT2.offsetMax = Vector2.zero;

        var scroll = scrollGO.AddComponent<ScrollRect>();
        scroll.horizontal = false;

        var vp = new GameObject("Viewport");
        vp.transform.SetParent(scrollGO.transform, false);
        Stretch(vp.AddComponent<RectTransform>());
        vp.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f);
        vp.AddComponent<Mask>().showMaskGraphic = false;
        scroll.viewport = vp.GetComponent<RectTransform>();

        var content = new GameObject("Content");
        content.transform.SetParent(vp.transform, false);
        var cRT2 = content.AddComponent<RectTransform>();
        cRT2.anchorMin = new Vector2(0f, 1f);
        cRT2.anchorMax = new Vector2(1f, 1f);
        cRT2.pivot     = new Vector2(0.5f, 1f);
        cRT2.sizeDelta = Vector2.zero;
        scroll.content = cRT2;

        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 6f;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.padding = new RectOffset(0, 0, 4, 4);

        content.AddComponent<ContentSizeFitter>().verticalFit =
            ContentSizeFitter.FitMode.PreferredSize;

        // Wiersze bindów
        InputBindings.Load();
        foreach (var action in InputBindings.AllActions)
            MkBindRow(content.transform, action);
    }

    // ── Stopka ─────────────────────────────────────────────────────────────

    static void BuildSFooter(Transform panel)
    {
        var foot = new GameObject("Footer");
        foot.transform.SetParent(panel, false);
        var fRT = foot.AddComponent<RectTransform>();
        fRT.anchorMin = new Vector2(0.04f, 0f);
        fRT.anchorMax = new Vector2(0.96f, 0.12f);
        fRT.offsetMin = fRT.offsetMax = Vector2.zero;

        var hlg = foot.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 14f;
        hlg.childAlignment = TextAnchor.MiddleRight;
        hlg.childForceExpandHeight = true; hlg.childForceExpandWidth = false;
        hlg.childControlHeight = hlg.childControlWidth = true;
        hlg.padding = new RectOffset(0, 0, 4, 4);

        var spacer = new GameObject("Spacer");
        spacer.transform.SetParent(foot.transform, false);
        spacer.AddComponent<LayoutElement>().flexibleWidth = 1f;
        spacer.AddComponent<RectTransform>();

        MkBtn(foot.transform, "Btn_Zapisz", "Zapisz",  C_SAVE,  C_SAVE_H,  C_BTN_P, C_TEXT, 42f);
        MkBtn(foot.transform, "Btn_Back",   "Powrót",  C_BTN,   C_BTN_H,   C_BTN_P, C_TEXT, 42f);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  ESC MENU CANVAS
    // ════════════════════════════════════════════════════════════════════════

    static GameObject BuildEscMenuCanvas()
    {
        var root = new GameObject("EscMenuCanvas");

        var c = root.AddComponent<Canvas>();
        c.renderMode   = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 100;

        var cs = root.AddComponent<CanvasScaler>();
        cs.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920f, 1080f);
        cs.matchWidthOrHeight  = 0.5f;

        root.AddComponent<GraphicRaycaster>();

        var overlay = MkImg("Overlay", root.transform, C_OVERLAY_ESC);
        Stretch(overlay.GetComponent<RectTransform>());

        var panel = MkImg("EscPanel", overlay.transform, C_PANEL_ESC);
        var pRT   = panel.GetComponent<RectTransform>();
        pRT.anchorMin = pRT.anchorMax = pRT.pivot = new Vector2(0.5f, 0.5f);
        pRT.sizeDelta = new Vector2(340f, 300f);

        var ol = panel.AddComponent<Outline>();
        ol.effectColor    = C_SEP;
        ol.effectDistance = new Vector2(1f, -1f);

        // Tytuł
        var tRT = MkTxt("Title", panel.transform, "PAUZA", 22f, C_TITLE, FontStyles.Bold)
                      .GetComponent<RectTransform>();
        tRT.anchorMin = new Vector2(0f, 1f); tRT.anchorMax = new Vector2(1f, 1f);
        tRT.pivot     = new Vector2(0.5f, 1f);
        tRT.anchoredPosition = new Vector2(0f, -22f);
        tRT.sizeDelta = new Vector2(0f, 36f);

        // Separator
        var sRT = MkImg("Sep", panel.transform, C_SEP).GetComponent<RectTransform>();
        sRT.anchorMin = new Vector2(0.08f, 1f); sRT.anchorMax = new Vector2(0.92f, 1f);
        sRT.pivot     = new Vector2(0.5f, 1f);
        sRT.anchoredPosition = new Vector2(0f, -62f);
        sRT.sizeDelta = new Vector2(0f, 2f);

        // Kontener przycisków
        var bc   = new GameObject("Buttons");
        bc.transform.SetParent(panel.transform, false);
        var bcRT = bc.AddComponent<RectTransform>();
        bcRT.anchorMin = bcRT.anchorMax = bcRT.pivot = new Vector2(0.5f, 0.5f);
        bcRT.anchoredPosition = new Vector2(0f, -22f);
        bcRT.sizeDelta = new Vector2(264f, 190f);

        var vlg = bc.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 12f;
        vlg.childAlignment    = TextAnchor.UpperCenter;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = vlg.childControlHeight = true;

        MkBtn(bc.transform, "Btn_Wznów",    "Wznów grę",       C_BTN_ESC,   C_BTN_H_ESC, C_BTN_P, C_TEXT, 46f);
        MkBtn(bc.transform, "Btn_Ustawienia","Ustawienia",      C_BTN_ESC,   C_BTN_H_ESC, C_BTN_P, C_TEXT, 46f);
        MkBtn(bc.transform, "Btn_Wyjście",  "Wyjście do menu", C_BTN_QUIT,  C_BTN_QUIT_H,C_BTN_P, C_TEXT, 46f);

        return root;
    }

    // ════════════════════════════════════════════════════════════════════════
    //  HELPERS — elementy UI
    // ════════════════════════════════════════════════════════════════════════

    static GameObject MkTabPanel(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Stretch(go.AddComponent<RectTransform>());
        return go;
    }

    // Rząd ze sliderem (goId = ASCII nazwa GO, label = tekst wyświetlany)
    static void MkSliderRow(Transform parent, string goId, string label, float defaultVal)
    {
        var row = MkRow("Row_" + goId, parent, 30f);
        MkRowLbl(row.transform, label);

        // Procent
        var pct = new GameObject("Pct");
        pct.transform.SetParent(row.transform, false);
        pct.AddComponent<LayoutElement>().preferredWidth = 46f;
        var pctTMP = pct.AddComponent<TextMeshProUGUI>();
        pctTMP.fontSize  = 13f;
        pctTMP.color     = C_TEXT_DIM;
        pctTMP.alignment = TextAlignmentOptions.MidlineRight;
        pctTMP.text      = Mathf.RoundToInt(defaultVal * 100f) + "%";

        // Slider GO
        var sGO = new GameObject("Slider");
        sGO.transform.SetParent(row.transform, false);
        var sLE = sGO.AddComponent<LayoutElement>();
        sLE.preferredWidth = 220f; sLE.preferredHeight = 10f; sLE.flexibleWidth = 1f;
        sGO.AddComponent<RectTransform>();

        var bg = new GameObject("BG");
        bg.transform.SetParent(sGO.transform, false);
        Stretch(bg.AddComponent<RectTransform>());
        bg.AddComponent<Image>().color = C_SLIDER_BG;

        var fa = new GameObject("FillArea");
        fa.transform.SetParent(sGO.transform, false);
        var faRT = fa.AddComponent<RectTransform>();
        faRT.anchorMin = new Vector2(0f, 0.25f); faRT.anchorMax = new Vector2(1f, 0.75f);
        faRT.offsetMin = new Vector2(5f, 0f);    faRT.offsetMax = new Vector2(-5f, 0f);

        var fill = new GameObject("Fill");
        fill.transform.SetParent(fa.transform, false);
        Stretch(fill.AddComponent<RectTransform>());
        fill.AddComponent<Image>().color = C_SLIDER_FILL;

        var ha = new GameObject("HandleArea");
        ha.transform.SetParent(sGO.transform, false);
        Stretch(ha.AddComponent<RectTransform>());

        var handle = new GameObject("Handle");
        handle.transform.SetParent(ha.transform, false);
        var hRT = handle.AddComponent<RectTransform>();
        hRT.sizeDelta = new Vector2(12f, 14f);
        handle.AddComponent<Image>().color = Color.white;

        var slider = sGO.AddComponent<Slider>();
        slider.minValue      = 0f;
        slider.maxValue      = 1f;
        slider.value         = defaultVal;
        slider.fillRect      = fill.GetComponent<RectTransform>();
        slider.handleRect    = hRT;
        slider.targetGraphic = handle.GetComponent<Image>();
        slider.direction     = Slider.Direction.LeftToRight;
    }

    // Rząd z kwadratowym checkboxem (goId = ASCII nazwa GO, label = tekst wyświetlany)
    static void MkToggleRow(Transform parent, string goId, string label)
    {
        var row = MkRow("Row_" + goId, parent, 36f);
        MkRowLbl(row.transform, label);

        var tGO = new GameObject("Toggle");
        tGO.transform.SetParent(row.transform, false);
        var tLE = tGO.AddComponent<LayoutElement>();
        tLE.preferredWidth = 20f; tLE.preferredHeight = 20f; tLE.flexibleWidth = 0f;

        var bgImg = tGO.AddComponent<Image>();
        bgImg.color = C_SLIDER_BG;
        var outl = tGO.AddComponent<Outline>();
        outl.effectColor    = C_SEP;
        outl.effectDistance = new Vector2(1.5f, -1.5f);

        var toggle = tGO.AddComponent<Toggle>();
        toggle.targetGraphic = bgImg;
        toggle.isOn = false;

        var chk = new GameObject("Check");
        chk.transform.SetParent(tGO.transform, false);
        var cRT = chk.AddComponent<RectTransform>();
        cRT.anchorMin = new Vector2(0.15f, 0.15f);
        cRT.anchorMax = new Vector2(0.85f, 0.85f);
        cRT.offsetMin = cRT.offsetMax = Vector2.zero;
        var chkImg = chk.AddComponent<Image>();
        chkImg.color = C_SLIDER_FILL;
        toggle.graphic = chkImg;
    }

    // Pełny TMP_Dropdown — opcje ładowane w Play Mode przez AdoptAndPopulateDropdown()
    static TMP_Dropdown BuildEditorDropdown(Transform parent)
    {
        var go = new GameObject("ResDropdown");
        go.transform.SetParent(parent, false);

        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = 280f; le.preferredHeight = 36f; le.flexibleWidth = 1f;

        var img = go.AddComponent<Image>();
        img.color = C_PANEL2;

        var dd = go.AddComponent<TMP_Dropdown>();
        dd.targetGraphic = img;

        // Label
        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(go.transform, false);
        var lRT = labelGO.AddComponent<RectTransform>();
        lRT.anchorMin = new Vector2(0.05f, 0f); lRT.anchorMax = new Vector2(0.85f, 1f);
        lRT.offsetMin = lRT.offsetMax = Vector2.zero;
        var lTMP = labelGO.AddComponent<TextMeshProUGUI>();
        lTMP.color = C_TEXT; lTMP.fontSize = 14f;
        lTMP.alignment = TextAlignmentOptions.MidlineLeft;
        dd.captionText = lTMP;

        // Arrow
        var arrowGO = new GameObject("Arrow");
        arrowGO.transform.SetParent(go.transform, false);
        var aRT = arrowGO.AddComponent<RectTransform>();
        aRT.anchorMin = new Vector2(0.85f, 0.1f); aRT.anchorMax = new Vector2(1f, 0.9f);
        aRT.offsetMin = aRT.offsetMax = Vector2.zero;
        var aTMP = arrowGO.AddComponent<TextMeshProUGUI>();
        aTMP.text = "▼"; aTMP.color = C_TEXT_DIM;
        aTMP.fontSize = 12f; aTMP.alignment = TextAlignmentOptions.Center;

        // Template (wymagany przez TMP_Dropdown)
        var tGO = new GameObject("Template");
        tGO.transform.SetParent(go.transform, false);
        tGO.SetActive(false);
        var tImg = tGO.AddComponent<Image>();
        tImg.color = C_PANEL;
        var tRT = tGO.GetComponent<RectTransform>();
        tRT.anchorMin = new Vector2(0f, 0f); tRT.anchorMax = new Vector2(1f, 0f);
        tRT.pivot     = new Vector2(0.5f, 1f);
        tRT.anchoredPosition = new Vector2(0f, 2f);
        tRT.sizeDelta = new Vector2(0f, 150f);

        var scrollGO = new GameObject("Scroll");
        scrollGO.transform.SetParent(tGO.transform, false);
        var scrollRect = scrollGO.AddComponent<ScrollRect>();
        Stretch(scrollGO.GetComponent<RectTransform>());

        var viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollGO.transform, false);
        Stretch(viewport.AddComponent<RectTransform>());
        viewport.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);
        viewport.AddComponent<Mask>().showMaskGraphic = false;
        scrollRect.viewport = viewport.GetComponent<RectTransform>();

        var content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        var cRT = content.AddComponent<RectTransform>();
        cRT.anchorMin = new Vector2(0f, 1f); cRT.anchorMax = new Vector2(1f, 1f);
        cRT.pivot     = new Vector2(0.5f, 1f);
        cRT.sizeDelta = new Vector2(0f, 28f);
        scrollRect.content = cRT;
        var cvlg = content.AddComponent<VerticalLayoutGroup>();
        cvlg.childForceExpandWidth = true;
        cvlg.childControlWidth = cvlg.childControlHeight = true;

        // Item template
        var itemGO = new GameObject("Item");
        itemGO.transform.SetParent(content.transform, false);
        itemGO.AddComponent<Image>().color = C_PANEL2;
        var itemLE = itemGO.AddComponent<LayoutElement>();
        itemLE.minHeight = 28f;
        var itemToggle = itemGO.AddComponent<Toggle>();
        itemToggle.targetGraphic = itemGO.GetComponent<Image>();

        var itemLblGO = new GameObject("ItemLabel");
        itemLblGO.transform.SetParent(itemGO.transform, false);
        var iRT = itemLblGO.AddComponent<RectTransform>();
        Stretch(iRT);
        iRT.offsetMin = new Vector2(8f, 0f);
        var itemTMP = itemLblGO.AddComponent<TextMeshProUGUI>();
        itemTMP.color = C_TEXT; itemTMP.fontSize = 13f;
        itemTMP.alignment = TextAlignmentOptions.MidlineLeft;
        dd.itemText = itemTMP;
        itemToggle.graphic = itemGO.GetComponent<Image>();

        dd.template = tGO.GetComponent<RectTransform>();

        return dd;
    }

    // Wiersz rebindowania klawisza
    static void MkBindRow(Transform parent, string action)
    {
        var row = new GameObject("Row_" + action);
        row.transform.SetParent(parent, false);
        var le = row.AddComponent<LayoutElement>();
        le.preferredHeight = 38f; le.flexibleWidth = 1f;

        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment    = TextAnchor.MiddleLeft;
        hlg.childForceExpandHeight = true;
        hlg.childControlHeight     = true;
        hlg.padding = new RectOffset(4, 4, 2, 2);
        hlg.spacing = 8f;

        var lGO = new GameObject("ActionLabel");
        lGO.transform.SetParent(row.transform, false);
        var lLE = lGO.AddComponent<LayoutElement>();
        lLE.preferredWidth = 320f; lLE.flexibleWidth = 1f;
        var lTMP = lGO.AddComponent<TextMeshProUGUI>();
        lTMP.text      = InputBindings.GetDisplayName(action);
        lTMP.fontSize  = 14f;
        lTMP.color     = C_TEXT;
        lTMP.alignment = TextAlignmentOptions.MidlineLeft;

        var bGO = new GameObject("BindBtn");
        bGO.transform.SetParent(row.transform, false);
        var bLE = bGO.AddComponent<LayoutElement>();
        bLE.preferredWidth = 130f; bLE.flexibleWidth = 0f; bLE.preferredHeight = 34f;
        bGO.AddComponent<Image>().color = C_BIND_BTN;
        bGO.AddComponent<Button>();

        var kGO = new GameObject("KeyLabel");
        kGO.transform.SetParent(bGO.transform, false);
        Stretch(kGO.AddComponent<RectTransform>());
        var kTMP = kGO.AddComponent<TextMeshProUGUI>();
        kTMP.text        = InputBindings.KeyName(InputBindings.Get(action));
        kTMP.fontSize    = 13f;
        kTMP.color       = C_TEXT;
        kTMP.fontStyle   = FontStyles.Bold;
        kTMP.alignment   = TextAlignmentOptions.Center;
        kTMP.raycastTarget = false;
    }

    // ── Primitives ─────────────────────────────────────────────────────────

    static GameObject MkImg(string name, Transform parent, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        go.AddComponent<Image>().color = color;
        return go;
    }

    static GameObject MkTxt(string name, Transform parent, string text, float size,
        Color color, FontStyles style = FontStyles.Normal)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = size; tmp.color = color;
        tmp.fontStyle = style; tmp.alignment = TextAlignmentOptions.Center;
        return go;
    }

    static Button MkBtn(Transform parent, string goName, string label,
        Color normal, Color hover, Color pressed, Color textColor, float height)
    {
        var go = new GameObject(goName);
        go.transform.SetParent(parent, false);
        var le  = go.AddComponent<LayoutElement>();
        le.preferredHeight = height; le.flexibleWidth = 1f;
        var img = go.AddComponent<Image>();
        img.color = normal;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var col = btn.colors;
        col.normalColor = normal; col.highlightedColor = hover;
        col.pressedColor = pressed; col.selectedColor = normal;
        col.fadeDuration = 0.08f;
        btn.colors = col;

        var tGO = new GameObject("Label");
        tGO.transform.SetParent(go.transform, false);
        var tRT = tGO.AddComponent<RectTransform>();
        tRT.anchorMin = Vector2.zero; tRT.anchorMax = Vector2.one;
        tRT.offsetMin = tRT.offsetMax = Vector2.zero;
        var tmp = tGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label; tmp.fontSize = 15f; tmp.color = textColor;
        tmp.fontStyle = FontStyles.Bold; tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        return btn;
    }

    static GameObject MkRow(string name, Transform parent, float height)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var le  = go.AddComponent<LayoutElement>();
        le.preferredHeight = height; le.flexibleWidth = 1f;
        go.AddComponent<RectTransform>();
        var hlg = go.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment    = TextAnchor.MiddleLeft;
        hlg.childForceExpandHeight = true; hlg.childForceExpandWidth = false;
        hlg.childControlHeight = hlg.childControlWidth = true;
        hlg.padding = new RectOffset(8, 8, 4, 4);
        hlg.spacing = 12f;
        return go;
    }

    static void MkRowLbl(Transform parent, string text)
    {
        var go = new GameObject("Lbl");
        go.transform.SetParent(parent, false);
        var le  = go.AddComponent<LayoutElement>();
        le.preferredWidth = 220f; le.flexibleWidth = 0f;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = 15f;
        tmp.color = C_TEXT; tmp.alignment = TextAlignmentOptions.MidlineLeft;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    // ── Utilities ──────────────────────────────────────────────────────────

    static void EnsureEditorEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;
        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<EventSystem>();
        esGO.AddComponent<StandaloneInputModule>();
        Undo.RegisterCreatedObjectUndo(esGO, "Dodaj EventSystem");
        EditorSceneManager.MarkSceneDirty(esGO.scene);
        Debug.Log("[Generator] Dodano EventSystem do sceny.");
    }

    static bool ConfirmReplace(string canvasName)
    {
        var existing = GameObject.Find(canvasName);
        if (existing == null) return true;
        if (EditorUtility.DisplayDialog("Generator",
            $"{canvasName} już istnieje w scenie.\nUsunąć i wygenerować od nowa?", "Tak", "Nie"))
        {
            Undo.DestroyObjectImmediate(existing);
            return true;
        }
        return false;
    }
}
