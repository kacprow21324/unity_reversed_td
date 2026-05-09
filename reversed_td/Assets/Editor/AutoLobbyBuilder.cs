using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public static class AutoLobbyBuilder
{
    // ── Paleta ─────────────────────────────────────────────────────────────
    static readonly Color C_BG        = new Color(0.08f, 0.10f, 0.14f, 0.95f);
    static readonly Color C_Panel     = new Color(0.12f, 0.15f, 0.20f, 0.98f);
    static readonly Color C_PanelDark = new Color(0.09f, 0.11f, 0.16f, 0.98f);
    static readonly Color C_BtnBlue   = new Color(0.15f, 0.42f, 0.80f, 1.00f);
    static readonly Color C_BtnGreen  = new Color(0.12f, 0.60f, 0.28f, 1.00f);
    static readonly Color C_BtnRed    = new Color(0.75f, 0.12f, 0.12f, 1.00f);
    static readonly Color C_BtnOrange = new Color(0.85f, 0.45f, 0.05f, 1.00f);
    static readonly Color C_Text      = new Color(0.92f, 0.93f, 0.95f, 1.00f);
    static readonly Color C_Subtle    = new Color(0.55f, 0.58f, 0.65f, 1.00f);
    static readonly Color C_Input     = new Color(0.06f, 0.08f, 0.11f, 1.00f);
    static readonly Color C_Row       = new Color(0.10f, 0.13f, 0.18f, 0.85f);
    static readonly Color C_Sep       = new Color(0.25f, 0.28f, 0.35f, 1.00f);

    [MenuItem("Tools/Generuj UI Lobby")]
    public static void Build()
    {
        Canvas canvas = FindOrCreateCanvas();
        GameObject canvasGO = canvas.gameObject;

        // EventSystem
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<StandaloneInputModule>();
            Undo.RegisterCreatedObjectUndo(esGO, "Stwórz EventSystem");
        }

        // Usuń stary LobbyManager
        var oldManager = GameObject.Find("LobbyManager");
        if (oldManager != null)
        {
            Undo.DestroyObjectImmediate(oldManager);
            Debug.Log("[AutoLobbyBuilder] Usunięto stary LobbyManager.");
        }

        // LobbyManager (komponenty)
        var managerGO = new GameObject("LobbyManager");
        Undo.RegisterCreatedObjectUndo(managerGO, "Stwórz LobbyManager");
        var lobbyUI    = managerGO.AddComponent<MultiplayerLobbyUI>();
        var lobbyPanel = managerGO.AddComponent<LobbyPanelUI>();
        managerGO.AddComponent<NicknameManager>();

        // Overlay
        var overlayGO = MakeStretch("LobbyOverlay", canvasGO.transform);
        overlayGO.AddComponent<Image>().color = C_BG;

        // ══════════════════════════════════════════════════════════════════
        //  MULTIPLAYER PANEL  (400 × 520 px)
        // ══════════════════════════════════════════════════════════════════
        var multiPanel = MakeCenteredPanel("MultiplayerPanel", overlayGO.transform, 400f, 520f);

        MakeLabel("TitleLabel", multiPanel.transform,
            "LOBBY MULTIPLAYER",
            new Vector2(0f, 0.86f), Vector2.one,
            22f, FontStyles.Bold, C_Text);

        MakeSeparator("Sep0", multiPanel.transform, new Vector2(0.05f, 0.82f), new Vector2(0.95f, 0.835f));

        // Nick
        MakeLabel("NickLabel", multiPanel.transform,
            "Twój nick:",
            new Vector2(0.06f, 0.74f), new Vector2(0.94f, 0.80f),
            13f, FontStyles.Normal, C_Subtle, TextAlignmentOptions.Left);
        var nickField = MakeInputField("NicknameInput", multiPanel.transform,
            new Vector2(0.06f, 0.63f), new Vector2(0.94f, 0.73f), "Wpisz nick...");

        MakeSeparator("Sep1", multiPanel.transform, new Vector2(0.05f, 0.60f), new Vector2(0.95f, 0.615f));

        // IP
        MakeLabel("IPLabel", multiPanel.transform,
            "Adres IP hosta:",
            new Vector2(0.06f, 0.51f), new Vector2(0.94f, 0.58f),
            13f, FontStyles.Normal, C_Subtle, TextAlignmentOptions.Left);
        var ipField = MakeInputField("IPInputField", multiPanel.transform,
            new Vector2(0.06f, 0.40f), new Vector2(0.94f, 0.50f), "localhost");

        // Przyciski
        var hostBtn = MakeButton("HostButton", multiPanel.transform,
            "HOST",
            new Vector2(0.06f, 0.25f), new Vector2(0.94f, 0.37f), C_BtnGreen);
        var joinBtn = MakeButton("JoinButton", multiPanel.transform,
            "DOŁĄCZ",
            new Vector2(0.06f, 0.09f), new Vector2(0.94f, 0.21f), C_BtnBlue);

        // ══════════════════════════════════════════════════════════════════
        //  WAITING PANEL  (380 × 240 px)
        // ══════════════════════════════════════════════════════════════════
        var waitPanel = MakeCenteredPanel("WaitingPanel", overlayGO.transform, 380f, 240f);

        MakeLabel("WaitingTitle", waitPanel.transform,
            "OCZEKIWANIE...",
            new Vector2(0f, 0.70f), Vector2.one,
            20f, FontStyles.Bold, C_Text);

        MakeSeparator("Sep2", waitPanel.transform, new Vector2(0.05f, 0.64f), new Vector2(0.95f, 0.655f));

        var waitStatus = MakeLabel("WaitingStatusLabel", waitPanel.transform,
            "Łączenie...",
            new Vector2(0.06f, 0.38f), new Vector2(0.94f, 0.62f),
            15f, FontStyles.Normal, C_Subtle);

        var disconnectBtn = MakeButton("DisconnectButton", waitPanel.transform,
            "ROZŁĄCZ",
            new Vector2(0.15f, 0.10f), new Vector2(0.85f, 0.30f), C_BtnRed);

        waitPanel.SetActive(false);

        // ══════════════════════════════════════════════════════════════════
        //  LOBBY PANEL  (640 × 580 px)
        // ══════════════════════════════════════════════════════════════════
        var lobbyPanelGO = MakeCenteredPanel("LobbyPanel", overlayGO.transform, 640f, 580f);

        // Tytuł
        MakeLabel("LobbyTitle", lobbyPanelGO.transform,
            "PRE-GAME LOBBY",
            new Vector2(0f, 0.90f), Vector2.one,
            22f, FontStyles.Bold, C_Text);

        MakeSeparator("SepA", lobbyPanelGO.transform, new Vector2(0.04f, 0.868f), new Vector2(0.96f, 0.882f));

        // Lista graczy (dwa wiersze)
        var playerListRoot = MakePlayerListRoot(lobbyPanelGO.transform);

        MakeSeparator("SepB", lobbyPanelGO.transform, new Vector2(0.04f, 0.588f), new Vector2(0.96f, 0.602f));

        // Etykiety sekcji
        MakeLabel("MapLabel", lobbyPanelGO.transform,
            "Mapa:",
            new Vector2(0.04f, 0.52f), new Vector2(0.22f, 0.57f),
            13f, FontStyles.Normal, C_Subtle, TextAlignmentOptions.Left);

        MakeLabel("GoldLabel", lobbyPanelGO.transform,
            "Start Gold:",
            new Vector2(0.04f, 0.40f), new Vector2(0.26f, 0.45f),
            13f, FontStyles.Normal, C_Subtle, TextAlignmentOptions.Left);

        // Dropdown mapy
        var mapDD = MakeMapDropdown("MapDropdown", lobbyPanelGO.transform,
            new Vector2(0.04f, 0.44f), new Vector2(0.55f, 0.52f));

        // Podgląd mapy
        var mapPreviewGO = new GameObject("MapPreviewImage");
        mapPreviewGO.transform.SetParent(lobbyPanelGO.transform, false);
        SetAnchors(mapPreviewGO, new Vector2(0.58f, 0.40f), new Vector2(0.96f, 0.58f));
        var mapImg = mapPreviewGO.AddComponent<Image>();
        mapImg.color = new Color(0.12f, 0.15f, 0.22f);
        mapImg.preserveAspect = true;

        // InputField Start Gold
        var goldInput = MakeInputField("StartGoldInput", lobbyPanelGO.transform,
            new Vector2(0.04f, 0.32f), new Vector2(0.55f, 0.40f), "np. 500");

        MakeSeparator("SepC", lobbyPanelGO.transform, new Vector2(0.04f, 0.298f), new Vector2(0.96f, 0.312f));

        // Przycisk Gotowy
        var readyBtn = MakeButton("ReadyButton", lobbyPanelGO.transform,
            "Gotowy!",
            new Vector2(0.04f, 0.14f), new Vector2(0.44f, 0.27f), C_BtnGreen);

        // Przycisk Start Match (host)
        var startMatchBtn = MakeButton("StartMatchButton", lobbyPanelGO.transform,
            "Rozpocznij mecz",
            new Vector2(0.52f, 0.14f), new Vector2(0.96f, 0.27f), C_BtnOrange);
        startMatchBtn.interactable = false;

        // Status
        var lobbyStatus = MakeLabel("LobbyStatusText", lobbyPanelGO.transform,
            "Oczekiwanie na graczy (0/2)...",
            new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.13f),
            13f, FontStyles.Italic, C_Subtle);

        lobbyPanelGO.SetActive(false);

        // ── Podpięcie listenerów ──────────────────────────────────────────
        hostBtn.onClick.AddListener(lobbyUI.HostGame);
        joinBtn.onClick.AddListener(lobbyUI.JoinGameFromInput);
        disconnectBtn.onClick.AddListener(lobbyUI.Disconnect);
        readyBtn.onClick.RemoveAllListeners();
        startMatchBtn.onClick.RemoveAllListeners();

        // ── Przypisanie referencji MultiplayerLobbyUI ─────────────────────
        lobbyUI.multiplayerPanel    = multiPanel;
        lobbyUI.waitingPanel        = waitPanel;
        lobbyUI.lobbyPanel          = lobbyPanelGO;
        lobbyUI.ipInputField        = ipField;
        lobbyUI.nicknameInputField  = nickField;
        lobbyUI.statusLabel         = waitStatus;
        lobbyUI.lobbyPanelUI        = lobbyPanel;

        // ── Przypisanie referencji LobbyPanelUI ───────────────────────────
        lobbyPanel.playerListRoot   = playerListRoot;
        lobbyPanel.mapDropdown      = mapDD;
        lobbyPanel.mapPreviewImage  = mapImg;
        lobbyPanel.startGoldInput   = goldInput;
        lobbyPanel.readyButton      = readyBtn;
        lobbyPanel.startMatchButton = startMatchBtn;
        lobbyPanel.readyButtonText  = readyBtn.GetComponentInChildren<TextMeshProUGUI>();
        lobbyPanel.statusText       = lobbyStatus;

        // Pobierz TextMeshProUGUI z przycisku Gotowy, żeby LobbyPanelUI mógł zmieniać tekst
        var readyTxt = readyBtn.transform.Find("Text")?.GetComponent<TextMeshProUGUI>();
        if (readyTxt != null) lobbyPanel.readyButtonText = readyTxt;

        Selection.activeGameObject = managerGO;
        EditorUtility.SetDirty(managerGO);
        Debug.Log("[AutoLobbyBuilder] UI Lobby (z panelem pre-meczowym) wygenerowane pomyślnie!");
    }

    // ══ Helpers: konstruktory elementów ═══════════════════════════════════

    static Transform MakePlayerListRoot(Transform parent)
    {
        var go = new GameObject("PlayerListRoot");
        go.transform.SetParent(parent, false);
        SetAnchors(go, new Vector2(0.04f, 0.605f), new Vector2(0.96f, 0.862f));

        var vlg = go.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 5f;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.padding = new RectOffset(0, 0, 2, 2);

        // Wiersze graczy (2 szt.)
        for (int i = 0; i < 2; i++)
            BuildPlayerRow(go.transform, i);

        return go.transform;
    }

    static void BuildPlayerRow(Transform parent, int index)
    {
        var row = new GameObject($"PlayerRow_{index}");
        row.transform.SetParent(parent, false);

        row.AddComponent<Image>().color = new Color(0.10f, 0.13f, 0.18f, 0.85f);

        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(12, 12, 8, 8);
        hlg.spacing = 10f;
        hlg.childForceExpandWidth  = true;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth = hlg.childControlHeight = true;

        row.AddComponent<LayoutElement>().preferredHeight = 46f;

        // Nazwa gracza
        var nameGO = new GameObject("PlayerName");
        nameGO.transform.SetParent(row.transform, false);
        var nameTxt = nameGO.AddComponent<TextMeshProUGUI>();
        nameTxt.text      = index == 0 ? "[Host] oczekiwanie..." : "[Klient] oczekiwanie...";
        nameTxt.fontSize  = 16f;
        nameTxt.color     = new Color(0.90f, 0.92f, 0.95f);
        nameTxt.fontStyle = FontStyles.Bold;
        nameTxt.alignment = TextAlignmentOptions.Left;
        nameGO.AddComponent<LayoutElement>().flexibleWidth = 1f;

        // Status gotowości
        var statusGO = new GameObject("ReadyStatus");
        statusGO.transform.SetParent(row.transform, false);
        var statusTxt = statusGO.AddComponent<TextMeshProUGUI>();
        statusTxt.text      = "Czeka...";
        statusTxt.fontSize  = 14f;
        statusTxt.color     = new Color(0.55f, 0.58f, 0.66f);
        statusTxt.alignment = TextAlignmentOptions.Right;
        statusGO.AddComponent<LayoutElement>().preferredWidth = 110f;
    }

    static TMP_Dropdown MakeMapDropdown(string name, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        SetAnchors(go, anchorMin, anchorMax);
        go.AddComponent<Image>().color = C_Input;

        // Caption
        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(go.transform, false);
        StretchRT(labelGO, new Vector2(10, 4), new Vector2(-30, -4));
        var labelTxt = labelGO.AddComponent<TextMeshProUGUI>();
        labelTxt.fontSize  = 14f;
        labelTxt.color     = C_Text;
        labelTxt.alignment = TextAlignmentOptions.Left;

        // Arrow
        var arrowGO = new GameObject("Arrow");
        arrowGO.transform.SetParent(go.transform, false);
        var arrowRT = arrowGO.AddComponent<RectTransform>();
        arrowRT.anchorMin = new Vector2(1f, 0.5f);
        arrowRT.anchorMax = new Vector2(1f, 0.5f);
        arrowRT.pivot     = new Vector2(1f, 0.5f);
        arrowRT.sizeDelta = new Vector2(24f, 24f);
        arrowRT.anchoredPosition = new Vector2(-6f, 0f);
        arrowGO.AddComponent<Image>().color = C_Subtle;

        // Template (wymagany do otwierania listy)
        var (templateRT, itemLabelTxt) = BuildDropdownTemplate(go.transform);

        var dd = go.AddComponent<TMP_Dropdown>();
        dd.captionText = labelTxt;
        dd.itemText    = itemLabelTxt;
        dd.template    = templateRT;
        dd.options.Clear();
        dd.options.Add(new TMP_Dropdown.OptionData("Teren 1"));
        dd.options.Add(new TMP_Dropdown.OptionData("Teren 2"));
        dd.options.Add(new TMP_Dropdown.OptionData("Teren 3"));
        dd.RefreshShownValue();
        return dd;
    }

    static (RectTransform template, TextMeshProUGUI itemLabel) BuildDropdownTemplate(Transform parent)
    {
        var tGO = new GameObject("Template");
        tGO.transform.SetParent(parent, false);
        var tRT = tGO.AddComponent<RectTransform>();
        tRT.anchorMin = new Vector2(0f, 0f);
        tRT.anchorMax = new Vector2(1f, 0f);
        tRT.pivot     = new Vector2(0.5f, 1f);
        tRT.sizeDelta = new Vector2(0f, 140f);
        tGO.AddComponent<Image>().color = new Color(0.09f, 0.11f, 0.17f);
        var sr = tGO.AddComponent<ScrollRect>();
        tGO.AddComponent<CanvasGroup>();

        var vpGO = new GameObject("Viewport");
        vpGO.transform.SetParent(tGO.transform, false);
        var vpRT = vpGO.AddComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero;
        vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = vpRT.offsetMax = Vector2.zero;
        vpGO.AddComponent<Image>().color = new Color(0.09f, 0.11f, 0.17f);
        vpGO.AddComponent<Mask>().showMaskGraphic = false;
        sr.viewport = vpRT;

        var contentGO = new GameObject("Content");
        contentGO.transform.SetParent(vpGO.transform, false);
        var contentRT = contentGO.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = Vector2.one;
        contentRT.pivot     = new Vector2(0.5f, 1f);
        contentRT.sizeDelta = Vector2.zero;
        var vlg = contentGO.AddComponent<VerticalLayoutGroup>();
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = vlg.childControlHeight = true;
        sr.content = contentRT;

        var itemGO = new GameObject("Item");
        itemGO.transform.SetParent(contentGO.transform, false);
        var itemRT = itemGO.AddComponent<RectTransform>();
        itemRT.anchorMin = new Vector2(0f, 0.5f);
        itemRT.anchorMax = new Vector2(1f, 0.5f);
        itemRT.pivot     = new Vector2(0.5f, 0.5f);
        itemRT.sizeDelta = new Vector2(0f, 32f);
        var toggle = itemGO.AddComponent<Toggle>();
        itemGO.AddComponent<LayoutElement>().minHeight = 32f;

        var bgGO = new GameObject("Item Background");
        bgGO.transform.SetParent(itemGO.transform, false);
        StretchRT(bgGO, Vector2.zero, Vector2.zero);
        bgGO.AddComponent<Image>().color = new Color(0.14f, 0.17f, 0.24f);

        var checkGO = new GameObject("Item Checkmark");
        checkGO.transform.SetParent(itemGO.transform, false);
        var checkRT = checkGO.AddComponent<RectTransform>();
        checkRT.anchorMin = new Vector2(0f, 0.5f);
        checkRT.anchorMax = new Vector2(0f, 0.5f);
        checkRT.pivot     = new Vector2(0f, 0.5f);
        checkRT.sizeDelta = new Vector2(20f, 20f);
        checkRT.anchoredPosition = new Vector2(5f, 0f);
        var checkImg = checkGO.AddComponent<Image>();
        checkImg.color = new Color(0.20f, 0.85f, 0.35f);
        toggle.graphic = checkImg;

        var itemLabelGO = new GameObject("Item Label");
        itemLabelGO.transform.SetParent(itemGO.transform, false);
        StretchRT(itemLabelGO, new Vector2(28f, 2f), new Vector2(-4f, -2f));
        var itemLabelTxt = itemLabelGO.AddComponent<TextMeshProUGUI>();
        itemLabelTxt.alignment = TextAlignmentOptions.Left;
        itemLabelTxt.color     = C_Text;
        itemLabelTxt.fontSize  = 14f;

        tGO.SetActive(false);
        return (tRT, itemLabelTxt);
    }

    // ══ Helpers: ogólne ═══════════════════════════════════════════════════

    static Canvas FindOrCreateCanvas()
    {
        var existing = Object.FindFirstObjectByType<Canvas>();
        if (existing != null) return existing;

        var go     = new GameObject("LobbyCanvas");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight  = 0.5f;

        go.AddComponent<GraphicRaycaster>();
        Undo.RegisterCreatedObjectUndo(go, "Stwórz LobbyCanvas");
        return canvas;
    }

    static GameObject MakeStretch(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return go;
    }

    static GameObject MakeCenteredPanel(string name, Transform parent, float w, float h)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = Vector2.zero;
        go.AddComponent<Image>().color = C_Panel;
        return go;
    }

    static TextMeshProUGUI MakeLabel(string name, Transform parent, string text,
        Vector2 anchorMin, Vector2 anchorMax,
        float fontSize, FontStyles style, Color color,
        TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        // Bezpośrednie AddComponent zamiast GetComponent??Add — unika Unity fake-null z ?? operatorem
        var rt       = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var tmp      = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.fontStyle = style;
        tmp.color     = color;
        tmp.alignment = align;
        return tmp;
    }

    static TMP_InputField MakeInputField(string name, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax, string placeholder)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt       = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        go.AddComponent<Image>().color = C_Input;

        var phGO = new GameObject("Placeholder");
        phGO.transform.SetParent(go.transform, false);
        StretchRT(phGO, new Vector2(8f, 4f), new Vector2(-8f, -4f));
        var phTmp = phGO.AddComponent<TextMeshProUGUI>();
        phTmp.text      = placeholder;
        phTmp.fontSize  = 14f;
        phTmp.color     = new Color(0.4f, 0.42f, 0.48f, 1f);
        phTmp.alignment = TextAlignmentOptions.Left;

        var txtGO = new GameObject("Text");
        txtGO.transform.SetParent(go.transform, false);
        StretchRT(txtGO, new Vector2(8f, 4f), new Vector2(-8f, -4f));
        var txtTmp = txtGO.AddComponent<TextMeshProUGUI>();
        txtTmp.fontSize  = 14f;
        txtTmp.color     = C_Text;
        txtTmp.alignment = TextAlignmentOptions.Left;

        var field = go.AddComponent<TMP_InputField>();
        field.textComponent  = txtTmp;
        field.placeholder    = phTmp;
        field.characterLimit = 64;
        return field;
    }

    static Button MakeButton(string name, Transform parent, string label,
        Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt       = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        var img = go.AddComponent<Image>();
        img.color = color;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var cols = btn.colors;
        cols.highlightedColor = Lighten(color, 0.15f);
        cols.pressedColor     = Darken(color, 0.20f);
        cols.disabledColor    = new Color(color.r, color.g, color.b, 0.38f);
        btn.colors = cols;

        var txtGO = new GameObject("Text");
        txtGO.transform.SetParent(go.transform, false);
        StretchRT(txtGO, Vector2.zero, Vector2.zero);
        var tmp = txtGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 15f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color     = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;

        return btn;
    }

    static void MakeSeparator(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt       = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        go.AddComponent<Image>().color = C_Sep;
    }

    // SetAnchors używane dla MapPreviewImage i innych pre-istniejących GO
    static void SetAnchors(GameObject go, Vector2 anchorMin, Vector2 anchorMax)
    {
        // Używamy if==null zamiast ?? — Unity override'uje == ale nie ??, co powoduje fake-null bugę
        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static void StretchRT(GameObject go, Vector2 offsetMin, Vector2 offsetMax)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
    }

    static Color Lighten(Color c, float a) =>
        new Color(Mathf.Clamp01(c.r + a), Mathf.Clamp01(c.g + a), Mathf.Clamp01(c.b + a), c.a);
    static Color Darken(Color c, float a) =>
        new Color(Mathf.Clamp01(c.r - a), Mathf.Clamp01(c.g - a), Mathf.Clamp01(c.b - a), c.a);
}
