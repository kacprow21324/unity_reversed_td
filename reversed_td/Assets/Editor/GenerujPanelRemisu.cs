using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

public static class GenerujPanelRemisu
{
    [MenuItem("Tools/UI/Generuj Panel Remisu")]
    public static void Generate()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null) { Debug.LogError("[GenerujPanelRemisu] Brak Canvas w scenie!"); return; }

        Transform existing = canvas.transform.Find("DrawPanel");
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        GameManager gm = Object.FindFirstObjectByType<GameManager>();

        // Żółty motyw
        Color accent = new Color(1f, 0.85f, 0.10f);
        Color bg     = new Color(0.10f, 0.08f, 0f, 0.90f);

        var panel = BuildPanel(canvas.transform, accent, bg,
            out var statsText,
            out var menuBtn,
            out var lobbyBtn);

        panel.SetActive(false);
        Undo.RegisterCreatedObjectUndo(panel, "Generuj Panel Remisu");

        if (gm != null)
        {
            gm.drawPanel      = panel;
            gm.drawStatsText  = statsText;
            gm.drawMenuButton = menuBtn;
            gm.drawLobbyButton = lobbyBtn;

            UnityEventTools.AddVoidPersistentListener(menuBtn.onClick,  new UnityAction(gm.GoToMainMenu));
            UnityEventTools.AddVoidPersistentListener(lobbyBtn.onClick, new UnityAction(gm.ReturnToLobby));

            EditorUtility.SetDirty(gm);
            Debug.Log("[GenerujPanelRemisu] Podlaczono do GameManager.");
        }
        else
        {
            Debug.LogWarning("[GenerujPanelRemisu] Brak GameManager — podlacz recznie:\n" +
                "  drawPanel      → DrawPanel\n" +
                "  drawStatsText  → DrawPanel/StatsText\n" +
                "  drawMenuButton → DrawPanel/Buttons/BtnMenu\n" +
                "  drawLobbyButton→ DrawPanel/Buttons/BtnLobby");
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = panel;
        Debug.Log("[GenerujPanelRemisu] Panel wygenerowany (zolty, bez restartu).");
    }

    static GameObject BuildPanel(Transform parent, Color accent, Color bg,
        out TextMeshProUGUI statsOut,
        out Button menuOut, out Button lobbyOut)
    {
        var panel   = new GameObject("DrawPanel");
        panel.transform.SetParent(parent, false);
        var rt      = panel.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        panel.AddComponent<Image>().color = bg;

        var titleGO               = Child(panel.transform, "Title");
        var titleRT               = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin         = new Vector2(0.1f, 0.82f);
        titleRT.anchorMax         = new Vector2(0.9f, 0.96f);
        titleRT.offsetMin         = titleRT.offsetMax = Vector2.zero;
        var titleTMP              = titleGO.AddComponent<TextMeshProUGUI>();
        titleTMP.text             = "REMIS!";
        titleTMP.enableAutoSizing = true;
        titleTMP.fontSizeMin      = 24f; titleTMP.fontSizeMax = 60f;
        titleTMP.fontStyle        = FontStyles.Bold;
        titleTMP.color            = accent;
        titleTMP.alignment        = TextAlignmentOptions.Center;

        var statsGO               = Child(panel.transform, "StatsText");
        var statsRT               = statsGO.GetComponent<RectTransform>();
        statsRT.anchorMin         = new Vector2(0.05f, 0.22f);
        statsRT.anchorMax         = new Vector2(0.95f, 0.80f);
        statsRT.offsetMin         = statsRT.offsetMax = Vector2.zero;
        var statsTMP              = statsGO.AddComponent<TextMeshProUGUI>();
        statsTMP.text             = "Ladowanie statystyk...";
        statsTMP.enableAutoSizing = true;
        statsTMP.fontSizeMin      = 10f; statsTMP.fontSizeMax = 18f;
        statsTMP.color            = new Color(0.9f, 0.9f, 0.9f);
        statsTMP.alignment        = TextAlignmentOptions.TopLeft;
        statsOut = statsTMP;

        // Remis — tylko 2 przyciski (bez restartu)
        var btnBar     = Child(panel.transform, "Buttons");
        var btnRT      = btnBar.GetComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(0.15f, 0.04f);
        btnRT.anchorMax = new Vector2(0.85f, 0.20f);
        btnRT.offsetMin = btnRT.offsetMax = Vector2.zero;
        var hlg        = btnBar.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing    = 18f;
        hlg.childForceExpandWidth = hlg.childForceExpandHeight = true;
        hlg.childControlWidth     = hlg.childControlHeight     = true;

        // Żółte tło przycisku menu pasuje do motywu
        menuOut  = Btn(btnBar.transform, "BtnMenu",  "Wyjscie do menu", new Color(0.45f, 0.35f, 0.02f));
        lobbyOut = Btn(btnBar.transform, "BtnLobby", "Powrot do lobby", new Color(0.12f, 0.25f, 0.55f));

        return panel;
    }

    static GameObject Child(Transform parent, string childName)
    {
        var go = new GameObject(childName);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    static Button Btn(Transform parent, string goName, string label, Color bg)
    {
        var go  = new GameObject(goName);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = bg;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var bc  = btn.colors;
        bc.normalColor      = bg;
        bc.highlightedColor = new Color(Mathf.Min(bg.r*1.3f,1f), Mathf.Min(bg.g*1.3f,1f), Mathf.Min(bg.b*1.3f,1f), 1f);
        bc.pressedColor     = new Color(bg.r*0.65f, bg.g*0.65f, bg.b*0.65f, 1f);
        btn.colors = bc;

        var txtGO  = new GameObject("Label");
        txtGO.transform.SetParent(go.transform, false);
        var txtRT  = txtGO.AddComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero; txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = new Vector2(6f,4f); txtRT.offsetMax = new Vector2(-6f,-4f);
        var tmp        = txtGO.AddComponent<TextMeshProUGUI>();
        tmp.text       = label;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 12f; tmp.fontSizeMax = 22f;
        tmp.fontStyle  = FontStyles.Bold;
        tmp.color      = Color.white;
        tmp.alignment  = TextAlignmentOptions.Center;
        return btn;
    }
}
