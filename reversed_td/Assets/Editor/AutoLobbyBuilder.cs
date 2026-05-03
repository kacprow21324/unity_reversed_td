using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public static class AutoLobbyBuilder
{
    // ── Paleta ────────────────────────────────────────────────────────────
    static readonly Color C_BG       = new Color(0.08f, 0.10f, 0.14f, 0.95f);
    static readonly Color C_Panel    = new Color(0.12f, 0.15f, 0.20f, 0.98f);
    static readonly Color C_BtnBlue  = new Color(0.15f, 0.42f, 0.80f, 1.00f);
    static readonly Color C_BtnGreen = new Color(0.12f, 0.60f, 0.28f, 1.00f);
    static readonly Color C_BtnRed   = new Color(0.75f, 0.12f, 0.12f, 1.00f);
    static readonly Color C_Text     = new Color(0.92f, 0.93f, 0.95f, 1.00f);
    static readonly Color C_Subtle   = new Color(0.55f, 0.58f, 0.65f, 1.00f);
    static readonly Color C_Input    = new Color(0.06f, 0.08f, 0.11f, 1.00f);

    [MenuItem("Tools/Generuj UI Lobby")]
    public static void Build()
    {
        // ── Canvas ──────────────────────────────────────────────────────
        Canvas canvas = FindOrCreateCanvas();
        GameObject canvasGO = canvas.gameObject;

        // ── EventSystem ──────────────────────────────────────────────────
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<StandaloneInputModule>();
            Undo.RegisterCreatedObjectUndo(esGO, "Stwórz EventSystem");
        }

        // ── Usuń stary LobbyManager jeśli istnieje ───────────────────────
        var oldManager = GameObject.Find("LobbyManager");
        if (oldManager != null)
        {
            Undo.DestroyObjectImmediate(oldManager);
            Debug.Log("[AutoLobbyBuilder] Usunięto stary LobbyManager.");
        }

        // ── LobbyManager (MonoBehaviour) ─────────────────────────────────
        var managerGO = new GameObject("LobbyManager");
        Undo.RegisterCreatedObjectUndo(managerGO, "Stwórz LobbyManager");
        var lobbyUI = managerGO.AddComponent<MultiplayerLobbyUI>();

        // ── Overlay (ciemne tło całego ekranu) ───────────────────────────
        var overlayGO = MakeStretch("LobbyOverlay", canvasGO.transform);
        overlayGO.AddComponent<Image>().color = C_BG;

        // ══════════════════════════════════════════════════════════════════
        //  MULTIPLAYER PANEL  (380 × 460 px, wyśrodkowany)
        // ══════════════════════════════════════════════════════════════════
        var multiplayerPanel = MakeCenteredPanel("MultiplayerPanel", overlayGO.transform, 380f, 460f);

        // Tytuł
        MakeLabel("TitleLabel", multiplayerPanel.transform,
            "LOBBY MULTIPLAYER",
            anchorMin: new Vector2(0f, 0.84f), anchorMax: Vector2.one,
            fontSize: 22f, style: FontStyles.Bold, color: C_Text);

        // Separator
        MakeSeparator("Sep1", multiplayerPanel.transform,
            anchorMin: new Vector2(0.05f, 0.80f), anchorMax: new Vector2(0.95f, 0.815f));

        // Etykieta IP
        MakeLabel("IPLabel", multiplayerPanel.transform,
            "Adres IP hosta:",
            anchorMin: new Vector2(0.06f, 0.68f), anchorMax: new Vector2(0.94f, 0.78f),
            fontSize: 14f, style: FontStyles.Normal, color: C_Subtle,
            align: TextAlignmentOptions.Left);

        // InputField IP
        var ipField = MakeInputField("IPInputField", multiplayerPanel.transform,
            anchorMin: new Vector2(0.06f, 0.56f), anchorMax: new Vector2(0.94f, 0.68f),
            placeholder: "localhost");

        // Przycisk HOST
        var hostBtn = MakeButton("HostButton", multiplayerPanel.transform,
            "HOST",
            anchorMin: new Vector2(0.06f, 0.40f), anchorMax: new Vector2(0.94f, 0.52f),
            color: C_BtnGreen);

        // Przycisk DOŁĄCZ
        var joinBtn = MakeButton("JoinButton", multiplayerPanel.transform,
            "DOŁĄCZ",
            anchorMin: new Vector2(0.06f, 0.24f), anchorMax: new Vector2(0.94f, 0.36f),
            color: C_BtnBlue);

        // Status (mała etykieta pod przyciskami)
        var statusLabel = MakeLabel("StatusLabel", multiplayerPanel.transform,
            "",
            anchorMin: new Vector2(0.06f, 0.06f), anchorMax: new Vector2(0.94f, 0.22f),
            fontSize: 13f, style: FontStyles.Italic, color: C_Subtle);

        // ══════════════════════════════════════════════════════════════════
        //  WAITING PANEL  (380 × 240 px, wyśrodkowany)
        // ══════════════════════════════════════════════════════════════════
        var waitingPanel = MakeCenteredPanel("WaitingPanel", overlayGO.transform, 380f, 240f);

        MakeLabel("WaitingTitle", waitingPanel.transform,
            "OCZEKIWANIE...",
            anchorMin: new Vector2(0f, 0.70f), anchorMax: Vector2.one,
            fontSize: 20f, style: FontStyles.Bold, color: C_Text);

        MakeSeparator("Sep2", waitingPanel.transform,
            anchorMin: new Vector2(0.05f, 0.64f), anchorMax: new Vector2(0.95f, 0.655f));

        var waitingStatus = MakeLabel("WaitingStatusLabel", waitingPanel.transform,
            "Łączenie...",
            anchorMin: new Vector2(0.06f, 0.38f), anchorMax: new Vector2(0.94f, 0.62f),
            fontSize: 15f, style: FontStyles.Normal, color: C_Subtle);

        MakeButton("DisconnectButton", waitingPanel.transform,
            "ROZŁĄCZ",
            anchorMin: new Vector2(0.15f, 0.10f), anchorMax: new Vector2(0.85f, 0.30f),
            color: C_BtnRed);

        // WaitingPanel domyślnie ukryty
        waitingPanel.SetActive(false);

        // ── Podepnij przyciski ───────────────────────────────────────────
        Button disconnectBtn = waitingPanel.transform.Find("DisconnectButton").GetComponent<Button>();

        hostBtn.onClick.AddListener(lobbyUI.HostGame);
        joinBtn.onClick.AddListener(lobbyUI.JoinGameFromInput);
        disconnectBtn.onClick.AddListener(lobbyUI.Disconnect);

        // ── Przypisz referencje do MultiplayerLobbyUI ────────────────────
        lobbyUI.multiplayerPanel = multiplayerPanel;
        lobbyUI.waitingPanel  = waitingPanel;
        lobbyUI.ipInputField  = ipField;
        lobbyUI.statusLabel   = waitingStatus;

        // ── Zaznacz w Hierarchii ─────────────────────────────────────────
        Selection.activeGameObject = managerGO;
        EditorUtility.SetDirty(managerGO);

        Debug.Log("[AutoLobbyBuilder] UI Lobby wygenerowane pomyślnie!");
    }

    // ══ HELPERS ═══════════════════════════════════════════════════════════

    static Canvas FindOrCreateCanvas()
    {
        var existing = Object.FindFirstObjectByType<Canvas>();
        if (existing != null) return existing;

        var go     = new GameObject("LobbyCanvas");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode    = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder  = 20;

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
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var tmp = go.AddComponent<TextMeshProUGUI>();
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
        // Kontener
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        go.AddComponent<Image>().color = C_Input;

        // Placeholder text
        var phGO = new GameObject("Placeholder");
        phGO.transform.SetParent(go.transform, false);
        StretchRT(phGO, new Vector2(8f, 4f), new Vector2(-8f, -4f));
        var phTmp = phGO.AddComponent<TextMeshProUGUI>();
        phTmp.text      = placeholder;
        phTmp.fontSize  = 14f;
        phTmp.color     = new Color(0.4f, 0.42f, 0.48f, 1f);
        phTmp.alignment = TextAlignmentOptions.Left;

        // Input text
        var txtGO = new GameObject("Text");
        txtGO.transform.SetParent(go.transform, false);
        StretchRT(txtGO, new Vector2(8f, 4f), new Vector2(-8f, -4f));
        var txtTmp = txtGO.AddComponent<TextMeshProUGUI>();
        txtTmp.fontSize  = 14f;
        txtTmp.color     = C_Text;
        txtTmp.alignment = TextAlignmentOptions.Left;

        var field = go.AddComponent<TMP_InputField>();
        field.textComponent   = txtTmp;
        field.placeholder     = phTmp;
        field.characterLimit  = 64;
        return field;
    }

    static Button MakeButton(string name, Transform parent, string label,
        Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
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
        cols.disabledColor    = new Color(color.r, color.g, color.b, 0.4f);
        btn.colors = cols;

        // Tekst przycisku
        var txtGO = new GameObject("Text");
        txtGO.transform.SetParent(go.transform, false);
        StretchRT(txtGO, Vector2.zero, Vector2.zero);
        var tmp = txtGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 16f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color     = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;

        return btn;
    }

    static void MakeSeparator(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        go.AddComponent<Image>().color = new Color(0.25f, 0.28f, 0.35f, 1f);
    }

    static void StretchRT(GameObject go, Vector2 offsetMin, Vector2 offsetMax)
    {
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
    }

    static Color Lighten(Color c, float amount) =>
        new Color(Mathf.Clamp01(c.r + amount), Mathf.Clamp01(c.g + amount), Mathf.Clamp01(c.b + amount), c.a);

    static Color Darken(Color c, float amount) =>
        new Color(Mathf.Clamp01(c.r - amount), Mathf.Clamp01(c.g - amount), Mathf.Clamp01(c.b - amount), c.a);
}
