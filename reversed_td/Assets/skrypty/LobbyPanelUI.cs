using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// Zarządza panelem lobby przed-meczowego.
/// Elementy UI mogą być podpięte przez Inspektora (AutoLobbyBuilder)
/// lub zostaną wygenerowane dynamicznie w Start() jeśli brakuje referencji.
public class LobbyPanelUI : MonoBehaviour
{
    public static LobbyPanelUI Instance { get; private set; }

    // ── Inspektor ──────────────────────────────────────────────────────────

    [Header("Lista graczy (auto-tworzona jeśli brak)")]
    public Transform playerListRoot;

    [Header("Wybór mapy (tylko Host)")]
    public TMP_Dropdown  mapDropdown;
    public Image         mapPreviewImage;
    public Sprite[]      mapPreviewSprites;

    [Header("Start Gold (tylko Host)")]
    public TMP_InputField startGoldInput;

    [Header("Przyciski")]
    public Button            readyButton;
    public Button            startMatchButton;
    public TextMeshProUGUI   readyButtonText;
    public TextMeshProUGUI   statusText;

    [Header("Sceny map (kolejność = dropdown)")]
    public string[] mapSceneNames =
    {
        "Teren1_Multiplayer",
        "Teren2_Multiplayer",
        "Teren3_Multiplayer"
    };

    // ── Stan wewnętrzny ────────────────────────────────────────────────────

    bool _myLobbyReady;
    readonly List<(TextMeshProUGUI name, TextMeshProUGUI status)> _rows = new();

    static readonly Color C_Ready = new Color(0.20f, 0.85f, 0.35f);
    static readonly Color C_Wait  = new Color(0.58f, 0.60f, 0.66f);

    // ── Lifecycle ──────────────────────────────────────────────────────────

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        EnsureAllElements();
        WireButtons();
        // Wstępne odświeżenie (może nie być jeszcze żadnych graczy)
        RefreshPlayerList();
    }

    // ── Publiczne API wywoływane przez MultiplayerLobbyUI ─────────────────

    /// Wywoływane gdy panel staje się widoczny (gracz połączył się).
    public void OnLobbyOpened()
    {
        _myLobbyReady = false;
        SetupHostControls();
        RefreshPlayerList();
    }

    // ── API wywoływane z haków SyncVar w NetworkPlayer ────────────────────

    public void RefreshPlayerList()
    {
        var players = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None);
        System.Array.Sort(players, (a, b) => a.playerIndex.CompareTo(b.playerIndex));

        EnsureRows(2);

        for (int i = 0; i < 2; i++)
        {
            if (i < players.Length)
            {
                var p = players[i];
                string prefix = p.playerIndex == 1 ? "[Host] " : "[Klient] ";
                _rows[i].name.text   = prefix + p.playerNickname;
                _rows[i].status.text  = p.isLobbyReady ? "GOTOWY" : "Czeka...";
                _rows[i].status.color = p.isLobbyReady ? C_Ready : C_Wait;
            }
            else
            {
                _rows[i].name.text   = "-- oczekiwanie na gracza... --";
                _rows[i].status.text  = "";
            }
        }

        UpdateReadyButton();
        UpdateStartButton(players);
        UpdateStatusText(players.Length);
    }

    /// Wywoływane gdy Host zmienił wybraną mapę (hook SyncVar).
    public void OnHostMapChanged(int index)
    {
        if (!NetworkServer.active && mapDropdown != null)
            mapDropdown.SetValueWithoutNotify(index);
        RefreshMapPreview(index);
    }

    /// Wywoływane gdy Host zmienił Start Gold (hook SyncVar).
    public void OnHostGoldChanged(int gold)
    {
        if (!NetworkServer.active && startGoldInput != null)
            startGoldInput.SetTextWithoutNotify(gold.ToString());
    }

    // ── Handlery przycisków ────────────────────────────────────────────────

    void OnReadyClicked()
    {
        _myLobbyReady = !_myLobbyReady;
        NetworkClient.localPlayer?.GetComponent<NetworkPlayer>()?.CmdSetLobbyReady(_myLobbyReady);
        UpdateReadyButton();
    }

    void OnStartMatchClicked()
    {
        if (!NetworkServer.active) return;

        var local = NetworkClient.localPlayer?.GetComponent<NetworkPlayer>();
        if (local == null) return;

        int mapIdx = mapDropdown != null ? mapDropdown.value : 0;
        string scene = (mapIdx < mapSceneNames.Length) ? mapSceneNames[mapIdx] : "";
        if (string.IsNullOrEmpty(scene))
        {
            Debug.LogWarning("[LobbyPanelUI] Brak nazwy sceny dla wybranej mapy. Uzupełnij mapSceneNames.");
            return;
        }

        int gold = 500;
        if (startGoldInput != null && int.TryParse(startGoldInput.text, out int parsed))
            gold = Mathf.Max(0, parsed);

        local.CmdTryStartMatch(scene, gold);
    }

    void OnMapDropdownChanged(int index)
    {
        if (!NetworkServer.active) return;
        NetworkClient.localPlayer?.GetComponent<NetworkPlayer>()?.CmdSetMapIndex(index);
        RefreshMapPreview(index);
    }

    void OnStartGoldEdited(string value)
    {
        if (!NetworkServer.active) return;
        if (int.TryParse(value, out int gold))
            NetworkClient.localPlayer?.GetComponent<NetworkPlayer>()?.CmdSetStartGold(Mathf.Max(0, gold));
    }

    // ── Logika UI ─────────────────────────────────────────────────────────

    void SetupHostControls()
    {
        bool isHost = NetworkServer.active;

        if (mapDropdown != null)
        {
            mapDropdown.interactable = isHost;
            mapDropdown.onValueChanged.RemoveAllListeners();
            if (isHost) mapDropdown.onValueChanged.AddListener(OnMapDropdownChanged);
        }

        if (startGoldInput != null)
        {
            startGoldInput.interactable = isHost;
            startGoldInput.onEndEdit.RemoveAllListeners();
            if (isHost)
            {
                startGoldInput.text = "500";
                startGoldInput.onEndEdit.AddListener(OnStartGoldEdited);
            }
        }

        if (startMatchButton != null)
            startMatchButton.gameObject.SetActive(isHost);
    }

    void UpdateReadyButton()
    {
        if (readyButtonText != null)
            readyButtonText.text = _myLobbyReady ? "Nie gotowy" : "Gotowy!";
    }

    void UpdateStartButton(NetworkPlayer[] players)
    {
        if (startMatchButton == null || !NetworkServer.active) return;
        bool allReady = players.Length == 2;
        if (allReady)
            foreach (var p in players)
                if (!p.isLobbyReady) { allReady = false; break; }
        startMatchButton.interactable = allReady;
    }

    void UpdateStatusText(int count)
    {
        if (statusText == null) return;
        statusText.text = count >= 2 ? "Lobby gotowe (2/2)" : $"Oczekiwanie na graczy ({count}/2)...";
    }

    void RefreshMapPreview(int index)
    {
        if (mapPreviewImage == null) return;
        bool hasSprite = mapPreviewSprites != null
            && index >= 0 && index < mapPreviewSprites.Length
            && mapPreviewSprites[index] != null;

        if (hasSprite) mapPreviewImage.sprite = mapPreviewSprites[index];
        mapPreviewImage.color = hasSprite ? Color.white : new Color(0.15f, 0.18f, 0.26f);
    }

    void WireButtons()
    {
        if (readyButton != null)
        {
            readyButton.onClick.RemoveAllListeners();
            readyButton.onClick.AddListener(OnReadyClicked);
        }
        if (startMatchButton != null)
        {
            startMatchButton.onClick.RemoveAllListeners();
            startMatchButton.onClick.AddListener(OnStartMatchClicked);
        }
    }

    // ── Dynamiczne generowanie UI ─────────────────────────────────────────

    void EnsureAllElements()
    {
        _btnBuildCount = 0; // resetuj przed budowaniem przycisków

        // Lista graczy
        if (playerListRoot == null)
            playerListRoot = BuildPlayerListRoot();

        EnsureRows(2);

        // Dropdown wyboru mapy
        if (mapDropdown == null)
            mapDropdown = BuildMapDropdown();

        // Podgląd mapy
        if (mapPreviewImage == null)
            mapPreviewImage = BuildMapPreview();

        // Pole Start Gold
        if (startGoldInput == null)
            startGoldInput = BuildStartGoldInput();

        // Przycisk Ready
        if (readyButton == null)
            readyButton = BuildButton("ReadyButton", "Gotowy!", new Color(0.15f, 0.55f, 0.25f), out readyButtonText);

        // Przycisk Start Match
        if (startMatchButton == null)
            startMatchButton = BuildButton("StartMatchButton", "Rozpocznij mecz", new Color(0.15f, 0.42f, 0.80f), out var _);

        // Tekst statusu
        if (statusText == null)
            statusText = BuildStatusText();
    }

    void EnsureRows(int count)
    {
        // Zbierz istniejące wiersze z playerListRoot
        if (_rows.Count == 0 && playerListRoot != null)
        {
            for (int i = 0; i < playerListRoot.childCount; i++)
            {
                var child = playerListRoot.GetChild(i);
                var n = child.Find("PlayerName")?.GetComponent<TextMeshProUGUI>();
                var s = child.Find("ReadyStatus")?.GetComponent<TextMeshProUGUI>();
                if (n != null && s != null) _rows.Add((n, s));
            }
        }

        while (_rows.Count < count)
            _rows.Add(BuildPlayerRow(_rows.Count));
    }

    (TextMeshProUGUI name, TextMeshProUGUI status) BuildPlayerRow(int index)
    {
        var row = new GameObject($"PlayerRow_{index}");
        row.transform.SetParent(playerListRoot, false);

        var img = row.AddComponent<Image>();
        img.color = new Color(0.10f, 0.13f, 0.18f, 0.85f);

        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(12, 12, 8, 8);
        hlg.spacing = 10f;
        hlg.childForceExpandWidth  = true;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth = hlg.childControlHeight = true;

        row.AddComponent<LayoutElement>().preferredHeight = 46f;

        var nameGO = new GameObject("PlayerName");
        nameGO.transform.SetParent(row.transform, false);
        var nameTxt = nameGO.AddComponent<TextMeshProUGUI>();
        nameTxt.fontSize  = 16f;
        nameTxt.color     = Color.white;
        nameTxt.fontStyle = FontStyles.Bold;
        nameTxt.alignment = TextAlignmentOptions.Left;
        nameTxt.text      = "-- oczekiwanie... --";
        nameGO.AddComponent<LayoutElement>().flexibleWidth = 1f;

        var statusGO = new GameObject("ReadyStatus");
        statusGO.transform.SetParent(row.transform, false);
        var statusTxt = statusGO.AddComponent<TextMeshProUGUI>();
        statusTxt.fontSize  = 14f;
        statusTxt.color     = C_Wait;
        statusTxt.alignment = TextAlignmentOptions.Right;
        statusTxt.text      = "";
        statusGO.AddComponent<LayoutElement>().preferredWidth = 110f;

        return (nameTxt, statusTxt);
    }

    Transform BuildPlayerListRoot()
    {
        var go = new GameObject("PlayerListRoot");
        go.transform.SetParent(transform, false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.05f, 0.65f);
        rt.anchorMax = new Vector2(0.95f, 0.90f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        var vlg = go.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 4f;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = vlg.childControlHeight = true;

        return go.transform;
    }

    TMP_Dropdown BuildMapDropdown()
    {
        var go = new GameObject("MapDropdown");
        go.transform.SetParent(transform, false);
        SetAnchors(go, new Vector2(0.05f, 0.46f), new Vector2(0.55f, 0.58f));
        go.AddComponent<Image>().color = new Color(0.08f, 0.10f, 0.15f);

        // Caption label
        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(go.transform, false);
        StretchChild(labelGO, new Vector2(10, 4), new Vector2(-30, -4));
        var labelTxt = labelGO.AddComponent<TextMeshProUGUI>();
        labelTxt.fontSize  = 14f;
        labelTxt.color     = Color.white;
        labelTxt.alignment = TextAlignmentOptions.Left;

        // Arrow
        var arrowGO = new GameObject("Arrow");
        arrowGO.transform.SetParent(go.transform, false);
        var arrowRT = arrowGO.AddComponent<RectTransform>();
        arrowRT.anchorMin = new Vector2(1f, 0.5f);
        arrowRT.anchorMax = new Vector2(1f, 0.5f);
        arrowRT.pivot     = new Vector2(1f, 0.5f);
        arrowRT.sizeDelta = new Vector2(22f, 22f);
        arrowRT.anchoredPosition = new Vector2(-5f, 0f);
        arrowGO.AddComponent<Image>().color = new Color(0.7f, 0.72f, 0.78f);

        // Template (wymagany przez TMP_Dropdown do otwierania listy)
        var (templateRT, itemLabelTxt) = BuildDropdownTemplate(go.transform);

        var dd = go.AddComponent<TMP_Dropdown>();
        dd.captionText = labelTxt;
        dd.itemText    = itemLabelTxt;
        dd.template    = templateRT;
        dd.options.Clear();
        foreach (var s in mapSceneNames)
            dd.options.Add(new TMP_Dropdown.OptionData(s.Replace("_Multiplayer", "")));
        dd.RefreshShownValue();
        return dd;
    }

    static (RectTransform template, TextMeshProUGUI itemLabel) BuildDropdownTemplate(Transform parent)
    {
        var templateGO = new GameObject("Template");
        templateGO.transform.SetParent(parent, false);
        var templateRT = templateGO.AddComponent<RectTransform>();
        templateRT.anchorMin = new Vector2(0f, 0f);
        templateRT.anchorMax = new Vector2(1f, 0f);
        templateRT.pivot     = new Vector2(0.5f, 1f);
        templateRT.sizeDelta = new Vector2(0f, 140f);
        templateGO.AddComponent<Image>().color = new Color(0.09f, 0.11f, 0.17f);
        var scrollRect = templateGO.AddComponent<ScrollRect>();
        templateGO.AddComponent<CanvasGroup>();

        // Viewport
        var vpGO = new GameObject("Viewport");
        vpGO.transform.SetParent(templateGO.transform, false);
        var vpRT = vpGO.AddComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero;
        vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = vpRT.offsetMax = Vector2.zero;
        vpGO.AddComponent<Image>().color = new Color(0.09f, 0.11f, 0.17f);
        vpGO.AddComponent<Mask>().showMaskGraphic = false;
        scrollRect.viewport = vpRT;

        // Content
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
        scrollRect.content = contentRT;

        // Item (szablon wiersza)
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
        StretchChild(bgGO, Vector2.zero, Vector2.zero);
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
        StretchChild(itemLabelGO, new Vector2(28f, 2f), new Vector2(-4f, -2f));
        var itemLabelTxt = itemLabelGO.AddComponent<TextMeshProUGUI>();
        itemLabelTxt.alignment = TextAlignmentOptions.Left;
        itemLabelTxt.color     = Color.white;
        itemLabelTxt.fontSize  = 14f;

        templateGO.SetActive(false); // Template musi być nieaktywny

        return (templateRT, itemLabelTxt);
    }

    Image BuildMapPreview()
    {
        var go = new GameObject("MapPreviewImage");
        go.transform.SetParent(transform, false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.58f, 0.36f);
        rt.anchorMax = new Vector2(0.95f, 0.62f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        var img = go.AddComponent<Image>();
        img.color = new Color(0.15f, 0.18f, 0.26f);
        img.preserveAspect = true;
        return img;
    }

    TMP_InputField BuildStartGoldInput()
    {
        var go = new GameObject("StartGoldInput");
        go.transform.SetParent(transform, false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.05f, 0.33f);
        rt.anchorMax = new Vector2(0.55f, 0.44f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        go.AddComponent<Image>().color = new Color(0.06f, 0.08f, 0.12f);

        var phGO = new GameObject("Placeholder");
        phGO.transform.SetParent(go.transform, false);
        StretchChild(phGO, new Vector2(8, 4), new Vector2(-8, -4));
        var phTxt = phGO.AddComponent<TextMeshProUGUI>();
        phTxt.text      = "Start Gold (np. 500)";
        phTxt.fontSize  = 13f;
        phTxt.color     = new Color(0.4f, 0.42f, 0.48f);
        phTxt.alignment = TextAlignmentOptions.Left;

        var txtGO = new GameObject("Text");
        txtGO.transform.SetParent(go.transform, false);
        StretchChild(txtGO, new Vector2(8, 4), new Vector2(-8, -4));
        var txtTxt = txtGO.AddComponent<TextMeshProUGUI>();
        txtTxt.fontSize  = 14f;
        txtTxt.color     = Color.white;
        txtTxt.alignment = TextAlignmentOptions.Left;

        var field = go.AddComponent<TMP_InputField>();
        field.textComponent  = txtTxt;
        field.placeholder    = phTxt;
        field.characterLimit = 6;
        field.contentType    = TMP_InputField.ContentType.IntegerNumber;
        field.text           = "500";
        return field;
    }

    static int _btnBuildCount = 0;
    Button BuildButton(string goName, string label, Color color, out TextMeshProUGUI labelTxt)
    {
        // Układamy przyciski od prawej żeby nie nachodziły na siebie
        _btnBuildCount++;
        float xMin = _btnBuildCount == 1 ? 0.04f : 0.52f;
        float xMax = _btnBuildCount == 1 ? 0.46f : 0.96f;

        var go = new GameObject(goName);
        go.transform.SetParent(transform, false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(xMin, 0.06f);
        rt.anchorMax = new Vector2(xMax, 0.18f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        var img = go.AddComponent<Image>();
        img.color = color;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var cols = btn.colors;
        cols.highlightedColor = Lighten(color, 0.12f);
        cols.pressedColor     = Darken(color, 0.18f);
        cols.disabledColor    = new Color(color.r, color.g, color.b, 0.38f);
        btn.colors = cols;

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(go.transform, false);
        StretchChild(textGO, Vector2.zero, Vector2.zero);
        var txt = textGO.AddComponent<TextMeshProUGUI>();
        txt.text      = label;
        txt.fontSize  = 15f;
        txt.fontStyle = FontStyles.Bold;
        txt.color     = Color.white;
        txt.alignment = TextAlignmentOptions.Center;

        labelTxt = txt;
        return btn;
    }

    TextMeshProUGUI BuildStatusText()
    {
        var go = new GameObject("LobbyStatusText");
        go.transform.SetParent(transform, false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.05f, 0.19f);
        rt.anchorMax = new Vector2(0.95f, 0.29f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        var txt = go.AddComponent<TextMeshProUGUI>();
        txt.fontSize  = 13f;
        txt.color     = new Color(0.55f, 0.58f, 0.65f);
        txt.alignment = TextAlignmentOptions.Center;
        txt.fontStyle = FontStyles.Italic;
        txt.text      = "Oczekiwanie na graczy...";
        return txt;
    }

    static void SetAnchors(GameObject go, Vector2 anchorMin, Vector2 anchorMax)
    {
        var rt = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static void StretchChild(GameObject go, Vector2 offsetMin, Vector2 offsetMax)
    {
        var rt = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
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
