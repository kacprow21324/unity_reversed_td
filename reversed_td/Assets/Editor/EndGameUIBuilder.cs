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
            Debug.LogError("EndGameUIBuilder: Brak Canvas w scenie. Dodaj Canvas i sprobuj ponownie.");
            return;
        }

        GameManager gm = Object.FindFirstObjectByType<GameManager>();
        if (gm == null)
            Debug.LogWarning("EndGameUIBuilder: Nie znaleziono GameManager w scenie – referencje paneli trzeba przypisac recznie.");

        RemoveIfExists(canvas.transform, "VictoryPanel");
        RemoveIfExists(canvas.transform, "DefeatPanel");

        // ── Panel Wygranej ─────────────────────────────────────────────────
        var victoryPanel    = BuildEndPanel(canvas.transform, "VictoryPanel",
            "WYGRANA!",
            new Color(0f, 0.06f, 0f, 0.88f),
            new Color(0.2f, 1f, 0.3f),
            out var victoryStats, out var victoryMenu, out var victoryRestart);

        // ── Panel Przegranej ───────────────────────────────────────────────
        var defeatPanel     = BuildEndPanel(canvas.transform, "DefeatPanel",
            "PRZEGRANA",
            new Color(0.08f, 0f, 0f, 0.88f),
            new Color(1f, 0.25f, 0.2f),
            out var defeatStats, out var defeatMenu, out var defeatRestart);

        // ── Podlacz do GameManager ─────────────────────────────────────────
        if (gm != null)
        {
            gm.victoryPanel     = victoryPanel;
            gm.defeatPanel      = defeatPanel;
            gm.victoryStatsText = victoryStats;
            gm.defeatStatsText  = defeatStats;

            victoryMenu.onClick.AddListener(   gm.GoToMainMenu);
            victoryRestart.onClick.AddListener(gm.RestartScene);
            defeatMenu.onClick.AddListener(    gm.GoToMainMenu);
            defeatRestart.onClick.AddListener( gm.RestartScene);

            EditorUtility.SetDirty(gm);
            Debug.Log("EndGameUIBuilder: Panele podlaczone do GameManager.");
        }

        victoryPanel.SetActive(false);
        defeatPanel.SetActive(false);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("EndGameUIBuilder: Panele konca gry wygenerowane pomyslnie.");
    }

    // ── Budowa panelu ──────────────────────────────────────────────────────

    static GameObject BuildEndPanel(Transform canvasParent, string panelName,
        string titleText, Color bgColor, Color accentColor,
        out TextMeshProUGUI statsOut, out Button menuBtnOut, out Button restartBtnOut)
    {
        var panel   = new GameObject(panelName);
        panel.transform.SetParent(canvasParent, false);
        var panelRT = panel.AddComponent<RectTransform>();
        panelRT.anchorMin = Vector2.zero;
        panelRT.anchorMax = Vector2.one;
        panelRT.offsetMin = Vector2.zero;
        panelRT.offsetMax = Vector2.zero;

        var bg    = panel.AddComponent<Image>();
        bg.color  = bgColor;

        // ── Tytul ──────────────────────────────────────────────────────────
        var titleGO  = new GameObject("Title");
        titleGO.transform.SetParent(panel.transform, false);
        var titleRT  = titleGO.AddComponent<RectTransform>();
        titleRT.anchorMin       = new Vector2(0.1f, 0.82f);
        titleRT.anchorMax       = new Vector2(0.9f, 0.96f);
        titleRT.offsetMin       = Vector2.zero;
        titleRT.offsetMax       = Vector2.zero;
        var titleTMP            = titleGO.AddComponent<TextMeshProUGUI>();
        titleTMP.text           = titleText;
        titleTMP.enableAutoSizing = true;
        titleTMP.fontSizeMin    = 24f;
        titleTMP.fontSizeMax    = 60f;
        titleTMP.fontStyle      = FontStyles.Bold;
        titleTMP.color          = accentColor;
        titleTMP.alignment      = TextAlignmentOptions.Center;

        // ── Statystyki (ScrollView lub TextMeshPro) ────────────────────────
        var statsGO  = new GameObject("StatsText");
        statsGO.transform.SetParent(panel.transform, false);
        var statsRT  = statsGO.AddComponent<RectTransform>();
        statsRT.anchorMin       = new Vector2(0.05f, 0.22f);
        statsRT.anchorMax       = new Vector2(0.95f, 0.80f);
        statsRT.offsetMin       = Vector2.zero;
        statsRT.offsetMax       = Vector2.zero;
        var statsTMP            = statsGO.AddComponent<TextMeshProUGUI>();
        statsTMP.text           = "Ladowanie statystyk...";
        statsTMP.fontSize       = 16f;
        statsTMP.enableAutoSizing = true;
        statsTMP.fontSizeMin    = 10f;
        statsTMP.fontSizeMax    = 18f;
        statsTMP.color          = new Color(0.9f, 0.9f, 0.9f);
        statsTMP.alignment      = TextAlignmentOptions.TopLeft;
        statsOut                = statsTMP;

        // ── Kontener przyciskow ────────────────────────────────────────────
        var btnContainerGO = new GameObject("Buttons");
        btnContainerGO.transform.SetParent(panel.transform, false);
        var btnRT          = btnContainerGO.AddComponent<RectTransform>();
        btnRT.anchorMin    = new Vector2(0.15f, 0.04f);
        btnRT.anchorMax    = new Vector2(0.85f, 0.19f);
        btnRT.offsetMin    = Vector2.zero;
        btnRT.offsetMax    = Vector2.zero;
        var hlg            = btnContainerGO.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing        = 20f;
        hlg.childForceExpandWidth  = true;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth      = true;
        hlg.childControlHeight     = true;
        hlg.padding        = new RectOffset(0, 0, 0, 0);

        menuBtnOut    = BuildNavButton(btnContainerGO.transform, "BtnMenu",    "Powrot do Menu",    accentColor);
        restartBtnOut = BuildNavButton(btnContainerGO.transform, "BtnRestart", "Restart",           new Color(0.9f, 0.9f, 0.9f));

        return panel;
    }

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
