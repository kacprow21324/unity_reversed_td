using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class LobbyUIGenerator
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    static T FindInScene<T>() where T : Component
    {
        foreach (var c in Resources.FindObjectsOfTypeAll<T>())
            if (c.gameObject.scene.IsValid()) return c;
        return null;
    }

    static Button CreateButton(Transform parent, string name,
        Color normal, Color hover, Color press,
        string label, float fontSize = 15f)
    {
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Utwórz " + name);
        go.transform.SetParent(parent, false);

        var img = go.AddComponent<Image>();
        img.color = normal;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var cols = btn.colors;
        cols.normalColor      = normal;
        cols.highlightedColor = hover;
        cols.pressedColor     = press;
        cols.selectedColor    = normal;
        cols.fadeDuration     = 0.1f;
        btn.colors = cols;

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(go.transform, false);
        var textRT = textGO.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = new Vector2(6f, 4f);
        textRT.offsetMax = new Vector2(-6f, -4f);
        var txt = textGO.AddComponent<TextMeshProUGUI>();
        txt.text      = label;
        txt.fontSize  = fontSize;
        txt.fontStyle = FontStyles.Bold;
        txt.color     = Color.white;
        txt.alignment = TextAlignmentOptions.Center;

        return btn;
    }

    static void AssignAndSave(Object target, string property, Object value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(property).objectReferenceValue = value;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
        EditorSceneManager.MarkSceneDirty((target as Component)?.gameObject.scene ?? default);
    }

    // ── Generuj wyjście lobby (LobbyPanel) ───────────────────────────────────

    [MenuItem("Tools/Lobby UI/Generuj przycisk: Wyjdź z lobby")]
    static void GenerateExitLobbyButton()
    {
        var lobbyPanelUI = FindInScene<LobbyPanelUI>();
        if (lobbyPanelUI == null)
        {
            EditorUtility.DisplayDialog("Błąd", "Nie znaleziono LobbyPanelUI w scenie.", "OK");
            return;
        }

        Transform parent = lobbyPanelUI.playerListRoot?.parent;
        if (parent == null)
        {
            EditorUtility.DisplayDialog("Błąd",
                "playerListRoot nie jest przypisany w LobbyPanelUI — nie wiem gdzie wstawić przycisk.", "OK");
            return;
        }

        var existing = parent.Find("ExitLobbyButton");
        if (existing != null)
        {
            if (!EditorUtility.DisplayDialog("Zastąpić?", "ExitLobbyButton już istnieje. Zastąpić?", "Tak", "Anuluj"))
                return;
            Undo.DestroyObjectImmediate(existing.gameObject);
        }

        var btn = CreateButton(parent, "ExitLobbyButton",
            new Color(0.55f, 0.15f, 0.10f),
            new Color(0.75f, 0.22f, 0.15f),
            new Color(0.35f, 0.09f, 0.06f),
            "← Wyjdź do menu");

        var rt = btn.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.04f, 0.91f);
        rt.anchorMax = new Vector2(0.32f, 0.98f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        AssignAndSave(lobbyPanelUI, "exitLobbyButton", btn);

        Selection.activeGameObject = btn.gameObject;
        EditorGUIUtility.PingObject(btn.gameObject);
        EditorUtility.DisplayDialog("Gotowe!", "ExitLobbyButton utworzony i podpięty.\nZapisz scenę (Ctrl+S).", "OK");
    }

    // ── Generuj przycisk Wstecz na multiplayerPanel ──────────────────────────

    [MenuItem("Tools/Lobby UI/Generuj przycisk: Wstecz (MultiplayerPanel)")]
    static void GenerateBackButton()
    {
        var multiUI = FindInScene<MultiplayerLobbyUI>();
        if (multiUI == null)
        {
            EditorUtility.DisplayDialog("Błąd", "Nie znaleziono MultiplayerLobbyUI w scenie.", "OK");
            return;
        }

        if (multiUI.multiplayerPanel == null)
        {
            EditorUtility.DisplayDialog("Błąd",
                "Pole multiplayerPanel nie jest przypisane w MultiplayerLobbyUI.", "OK");
            return;
        }

        Transform parent = multiUI.multiplayerPanel.transform;

        var existing = parent.Find("BackButton");
        if (existing != null)
        {
            if (!EditorUtility.DisplayDialog("Zastąpić?", "BackButton już istnieje. Zastąpić?", "Tak", "Anuluj"))
                return;
            Undo.DestroyObjectImmediate(existing.gameObject);
        }

        var btn = CreateButton(parent, "BackButton",
            new Color(0.18f, 0.18f, 0.22f),
            new Color(0.30f, 0.30f, 0.38f),
            new Color(0.10f, 0.10f, 0.13f),
            "← Wstecz", 14f);

        var rt = btn.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0f, 1f);
        rt.anchorMax        = new Vector2(0f, 1f);
        rt.pivot            = new Vector2(0f, 1f);
        rt.sizeDelta        = new Vector2(130f, 42f);
        rt.anchoredPosition = new Vector2(10f, -10f);

        AssignAndSave(multiUI, "backFromMultiplayerButton", btn);

        Selection.activeGameObject = btn.gameObject;
        EditorGUIUtility.PingObject(btn.gameObject);
        EditorUtility.DisplayDialog("Gotowe!", "BackButton utworzony i podpięty.\nZapisz scenę (Ctrl+S).", "OK");
    }

    [MenuItem("Tools/Lobby UI/Generuj przycisk: Wyjdź z lobby", true)]
    [MenuItem("Tools/Lobby UI/Generuj przycisk: Wstecz (MultiplayerPanel)", true)]
    static bool Validate() => !Application.isPlaying;
}
