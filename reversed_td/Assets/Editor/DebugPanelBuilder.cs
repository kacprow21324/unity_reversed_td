using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public static class DebugPanelBuilder
{
    [MenuItem("AI-Tools/Generate Debug Panel")]
    public static void Generate()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("DebugPanelBuilder: Brak Canvas w scenie.");
            return;
        }

        Transform existing = canvas.transform.Find("DebugPanel");
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);

        // ── Kontener (prawy gorny rog) ─────────────────────────────────────
        var panel   = new GameObject("DebugPanel");
        panel.transform.SetParent(canvas.transform, false);
        var panelRT = panel.AddComponent<RectTransform>();
        panelRT.anchorMin        = new Vector2(1f, 1f);
        panelRT.anchorMax        = new Vector2(1f, 1f);
        panelRT.pivot            = new Vector2(1f, 1f);
        panelRT.anchoredPosition = new Vector2(-10f, -10f);
        panelRT.sizeDelta        = new Vector2(220f, 130f);

        var bg   = panel.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.65f);

        var vlg                    = panel.AddComponent<VerticalLayoutGroup>();
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth      = true;
        vlg.childControlHeight     = true;
        vlg.padding                = new RectOffset(8, 8, 8, 8);
        vlg.spacing                = 6f;

        // ── Naglowek ──────────────────────────────────────────────────────
        var headerGO  = new GameObject("Header");
        headerGO.transform.SetParent(panel.transform, false);
        var headerTmp = headerGO.AddComponent<TextMeshProUGUI>();
        headerTmp.text      = "DEBUG";
        headerTmp.fontSize  = 12f;
        headerTmp.color     = new Color(1f, 0.7f, 0.1f);
        headerTmp.fontStyle = FontStyles.Bold;
        headerTmp.alignment = TextAlignmentOptions.Center;
        var headerLE        = headerGO.AddComponent<LayoutElement>();
        headerLE.preferredHeight = 20f;

        // ── Komponent DebugPanel na panelu ─────────────────────────────────
        var debugComp = panel.AddComponent<DebugPanel>();

        // ── 3 przyciski ─────────────────────────────────────────────────────
        var btn0 = BuildBtn(panel.transform, "Wygraj Runde [O]",
                            new Color(0.15f, 0.55f, 0.25f), new Color(0.25f, 0.75f, 0.4f));
        var btn1 = BuildBtn(panel.transform, "Wygraj Gre [I]",
                            new Color(0.1f, 0.35f, 0.65f),  new Color(0.2f, 0.5f, 0.85f));
        var btn2 = BuildBtn(panel.transform, "Przegraj [U]",
                            new Color(0.55f, 0.1f, 0.1f),   new Color(0.8f, 0.2f, 0.2f));

        UnityEventTools.AddVoidPersistentListener(btn0.onClick, debugComp.DebugWinRound);
        UnityEventTools.AddVoidPersistentListener(btn1.onClick, debugComp.DebugWinGame);
        UnityEventTools.AddVoidPersistentListener(btn2.onClick, debugComp.DebugLoseGame);

        EditorUtility.SetDirty(panel);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = panel;
        Debug.Log("DebugPanelBuilder: Panel debug wygenerowany pomyslnie.");
    }

    static Button BuildBtn(Transform parent, string label, Color normal, Color highlighted)
    {
        var go  = new GameObject("Btn_" + label.Split(' ')[0]);
        go.transform.SetParent(parent, false);

        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 28f;
        le.flexibleHeight  = 0f;

        var img   = go.AddComponent<Image>();
        img.color = normal;

        var btn   = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var bc    = btn.colors;
        bc.normalColor      = normal;
        bc.highlightedColor = highlighted;
        bc.pressedColor     = new Color(normal.r * 0.6f, normal.g * 0.6f, normal.b * 0.6f);
        btn.colors = bc;

        var txtGO = new GameObject("Label");
        txtGO.transform.SetParent(go.transform, false);
        var txtRT = txtGO.AddComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero;
        txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = new Vector2(4f, 2f);
        txtRT.offsetMax = new Vector2(-4f, -2f);

        var tmp              = txtGO.AddComponent<TextMeshProUGUI>();
        tmp.text             = label;
        tmp.fontSize         = 12f;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin      = 9f;
        tmp.fontSizeMax      = 13f;
        tmp.color            = Color.white;
        tmp.fontStyle        = FontStyles.Bold;
        tmp.alignment        = TextAlignmentOptions.Center;

        return btn;
    }
}
