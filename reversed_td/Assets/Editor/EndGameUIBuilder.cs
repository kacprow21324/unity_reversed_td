using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class EndGameUIBuilder
{
    [MenuItem("AI-Tools/Generate End Game UI")]
    public static void Generate()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("EndGameUIBuilder: Brak Canvas w scenie.");
            return;
        }

        GameManager gm = Object.FindFirstObjectByType<GameManager>();
        if (gm == null)
            Debug.LogWarning("EndGameUIBuilder: Nie znaleziono GameManager – przypisz referencje ręcznie.");

        RemoveIfExists(canvas.transform, "VictoryPanel");
        RemoveIfExists(canvas.transform, "DefeatPanel");
        RemoveIfExists(canvas.transform, "DrawPanel");

        // ── Panel Wygranej ─────────────────────────────────────────────────
        var victoryPanel = BuildEndPanel(canvas.transform, "VictoryPanel",
            "WYGRANA!",
            new Color(0f, 0.06f, 0f, 0.88f),
            new Color(0.2f, 1f, 0.3f),
            out var victoryStats, out var victoryMenu,
            out var victoryRestart, out var victoryLobby);

        // ── Panel Przegranej ───────────────────────────────────────────────
        var defeatPanel = BuildEndPanel(canvas.transform, "DefeatPanel",
            "PRZEGRANA",
            new Color(0.08f, 0f, 0f, 0.88f),
            new Color(1f, 0.25f, 0.2f),
            out var defeatStats, out var defeatMenu,
            out var defeatRestart, out var defeatLobby);

        // ── Panel Remisu (tylko MP) ────────────────────────────────────────
        var drawPanel = BuildDrawPanel(canvas.transform,
            out var drawStats, out var drawLobby);

        // ── Podłącz do GameManager ─────────────────────────────────────────
        if (gm != null)
        {
            gm.victoryPanel  = victoryPanel;
            gm.defeatPanel   = defeatPanel;
            gm.drawPanel     = drawPanel;

            gm.victoryStatsText = victoryStats;
            gm.defeatStatsText  = defeatStats;
            gm.drawStatsText    = drawStats;

            gm.victoryRestartButton = victoryRestart;
            gm.defeatRestartButton  = defeatRestart;

            gm.victoryLobbyButton = victoryLobby;
            gm.defeatLobbyButton  = defeatLobby;
            gm.drawLobbyButton    = drawLobby;

            victoryMenu.onClick.AddListener(gm.GoToMainMenu);
            victoryRestart.onClick.AddListener(gm.RestartScene);
            victoryLobby.onClick.AddListener(gm.ReturnToLobby);

            defeatMenu.onClick.AddListener(gm.GoToMainMenu);
            defeatRestart.onClick.AddListener(gm.RestartScene);
            defeatLobby.onClick.AddListener(gm.ReturnToLobby);

            drawLobby.onClick.AddListener(gm.ReturnToLobby);

            EditorUtility.SetDirty(gm);
            Debug.Log("EndGameUIBuilder: Panele podłączone do GameManager.");
        }

        victoryPanel.SetActive(false);
        defeatPanel.SetActive(false);
        drawPanel.SetActive(false);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("EndGameUIBuilder: Panele konca gry wygenerowane.");
    }

    // ── Budowa panelu Wygranej / Przegranej ───────────────────────────────

    static GameObject BuildEndPanel(Transform canvasParent, string panelName,
        string titleText, Color bgColor, Color accentColor,
        out TextMeshProUGUI statsOut,
        out Button menuBtnOut, out Button restartBtnOut, out Button lobbyBtnOut)
    {
        var panel   = new GameObject(panelName);
        panel.transform.SetParent(canvasParent, false);

        var panelRT       = panel.AddComponent<RectTransform>();
        panelRT.anchorMin = Vector2.zero;
        panelRT.anchorMax = Vector2.one;
        panelRT.offsetMin = Vector2.zero;
        panelRT.offsetMax = Vector2.zero;

        var bg    = panel.AddComponent<Image>();
        bg.color  = bgColor;

        // ── Tytuł ──────────────────────────────────────────────────────────
        var titleGO = new GameObject("Title");
        titleGO.transform.SetParent(panel.transform, false);
        var titleRT              = titleGO.AddComponent<RectTransform>();
        titleRT.anchorMin        = new Vector2(0.1f, 0.82f);
        titleRT.anchorMax        = new Vector2(0.9f, 0.96f);
        titleRT.offsetMin        = Vector2.zero;
        titleRT.offsetMax        = Vector2.zero;
        var titleTMP             = titleGO.AddComponent<TextMeshProUGUI>();
        titleTMP.text            = titleText;
        titleTMP.enableAutoSizing = true;
        titleTMP.fontSizeMin     = 24f;
        titleTMP.fontSizeMax     = 60f;
        titleTMP.fontStyle       = FontStyles.Bold;
        titleTMP.color           = accentColor;
        titleTMP.alignment       = TextAlignmentOptions.Center;

        // ── Statystyki ────────────────────────────────────────────────────
        var statsGO = new GameObject("StatsText");
        statsGO.transform.SetParent(panel.transform, false);
        var statsRT              = statsGO.AddComponent<RectTransform>();
        statsRT.anchorMin        = new Vector2(0.05f, 0.22f);
        statsRT.anchorMax        = new Vector2(0.95f, 0.80f);
        statsRT.offsetMin        = Vector2.zero;
        statsRT.offsetMax        = Vector2.zero;
        var statsTMP             = statsGO.AddComponent<TextMeshProUGUI>();
        statsTMP.text            = "Ladowanie statystyk...";
        statsTMP.fontSize        = 16f;
        statsTMP.enableAutoSizing = true;
        statsTMP.fontSizeMin     = 10f;
        statsTMP.fontSizeMax     = 18f;
        statsTMP.color           = new Color(0.9f, 0.9f, 0.9f);
        statsTMP.alignment       = TextAlignmentOptions.TopLeft;
        statsOut                 = statsTMP;

        // ── Konter przycisków ──────────────────────────────────────────────
        var btnGO      = new GameObject("Buttons");
        btnGO.transform.SetParent(panel.transform, false);
        var btnRT      = btnGO.AddComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(0.1f, 0.04f);
        btnRT.anchorMax = new Vector2(0.9f, 0.19f);
        btnRT.offsetMin = Vector2.zero;
        btnRT.offsetMax = Vector2.zero;
        var hlg         = btnGO.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing     = 16f;
        hlg.childForceExpandWidth  = true;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth      = true;
        hlg.childControlHeight     = true;

        menuBtnOut    = BuildNavButton(btnGO.transform, "BtnMenu",    "Powrot do Menu",     accentColor);
        restartBtnOut = BuildNavButton(btnGO.transform, "BtnRestart", "Restart",            new Color(0.85f, 0.85f, 0.85f));
        lobbyBtnOut   = BuildNavButton(btnGO.transform, "BtnLobby",  "Powrot do Lobby",    new Color(0.4f, 0.8f, 1f));

        return panel;
    }

    // ── Budowa panelu Remisu ──────────────────────────────────────────────

    static GameObject BuildDrawPanel(Transform canvasParent,
        out TextMeshProUGUI statsOut, out Button lobbyBtnOut)
    {
        var panel   = new GameObject("DrawPanel");
        panel.transform.SetParent(canvasParent, false);

        var panelRT       = panel.AddComponent<RectTransform>();
        panelRT.anchorMin = Vector2.zero;
        panelRT.anchorMax = Vector2.one;
        panelRT.offsetMin = Vector2.zero;
        panelRT.offsetMax = Vector2.zero;

        var bg    = panel.AddComponent<Image>();
        bg.color  = new Color(0f, 0.02f, 0.08f, 0.88f);

        var accentColor = new Color(0.35f, 0.75f, 1f);

        // ── Tytuł ──────────────────────────────────────────────────────────
        var titleGO = new GameObject("Title");
        titleGO.transform.SetParent(panel.transform, false);
        var titleRT              = titleGO.AddComponent<RectTransform>();
        titleRT.anchorMin        = new Vector2(0.1f, 0.82f);
        titleRT.anchorMax        = new Vector2(0.9f, 0.96f);
        titleRT.offsetMin        = Vector2.zero;
        titleRT.offsetMax        = Vector2.zero;
        var titleTMP             = titleGO.AddComponent<TextMeshProUGUI>();
        titleTMP.text            = "REMIS!";
        titleTMP.enableAutoSizing = true;
        titleTMP.fontSizeMin     = 24f;
        titleTMP.fontSizeMax     = 60f;
        titleTMP.fontStyle       = FontStyles.Bold;
        titleTMP.color           = accentColor;
        titleTMP.alignment       = TextAlignmentOptions.Center;

        // ── Statystyki ────────────────────────────────────────────────────
        var statsGO = new GameObject("StatsText");
        statsGO.transform.SetParent(panel.transform, false);
        var statsRT              = statsGO.AddComponent<RectTransform>();
        statsRT.anchorMin        = new Vector2(0.05f, 0.22f);
        statsRT.anchorMax        = new Vector2(0.95f, 0.80f);
        statsRT.offsetMin        = Vector2.zero;
        statsRT.offsetMax        = Vector2.zero;
        var statsTMP             = statsGO.AddComponent<TextMeshProUGUI>();
        statsTMP.text            = "Ladowanie statystyk...";
        statsTMP.fontSize        = 16f;
        statsTMP.enableAutoSizing = true;
        statsTMP.fontSizeMin     = 10f;
        statsTMP.fontSizeMax     = 18f;
        statsTMP.color           = new Color(0.9f, 0.9f, 0.9f);
        statsTMP.alignment       = TextAlignmentOptions.TopLeft;
        statsOut                 = statsTMP;

        // ── Przycisk ─────────────────────────────────────────────────────
        var btnGO       = new GameObject("Buttons");
        btnGO.transform.SetParent(panel.transform, false);
        var btnRT       = btnGO.AddComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(0.25f, 0.04f);
        btnRT.anchorMax = new Vector2(0.75f, 0.19f);
        btnRT.offsetMin = Vector2.zero;
        btnRT.offsetMax = Vector2.zero;

        lobbyBtnOut = BuildNavButton(btnGO.transform, "BtnLobby", "Powrot do Lobby", accentColor);

        return panel;
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    static Button BuildNavButton(Transform parent, string goName, string label, Color textColor)
    {
        var go    = new GameObject(goName);
        go.transform.SetParent(parent, false);

        var img   = go.AddComponent<Image>();
        img.color = new Color(0.14f, 0.14f, 0.18f, 1f);

        var btn   = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var bc    = btn.colors;
        bc.normalColor      = new Color(0.14f, 0.14f, 0.18f);
        bc.highlightedColor = new Color(0.28f, 0.28f, 0.36f);
        bc.pressedColor     = new Color(0.06f, 0.06f, 0.09f);
        btn.colors = bc;

        var txtGO  = new GameObject("Label");
        txtGO.transform.SetParent(go.transform, false);
        var txtRT  = txtGO.AddComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero;
        txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = new Vector2(6f, 4f);
        txtRT.offsetMax = new Vector2(-6f, -4f);
        var tmp    = txtGO.AddComponent<TextMeshProUGUI>();
        tmp.text   = label;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 12f;
        tmp.fontSizeMax = 22f;
        tmp.fontStyle   = FontStyles.Bold;
        tmp.color       = textColor;
        tmp.alignment   = TextAlignmentOptions.Center;

        return btn;
    }

    static void RemoveIfExists(Transform parent, string childName)
    {
        Transform t = parent.Find(childName);
        if (t != null) Object.DestroyImmediate(t.gameObject);
    }
}
