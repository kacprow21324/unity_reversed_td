using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// Generuje górny pasek HP/Timer dla trybu Multiplayer i podłącza go do MatchHPUI.
/// Uruchom: Tools → Generuj Match HP UI
public static class MatchHPUIBuilder
{
    [MenuItem("Tools/Generuj Match HP UI")]
    public static void Generate()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[MatchHPUIBuilder] Brak Canvas w scenie. Dodaj Canvas i spróbuj ponownie.");
            return;
        }

        // Usuń stary pasek jeśli istnieje
        RemoveIfExists(canvas.transform, "MatchHPBar");

        // ── Pasek główny ──────────────────────────────────────────────────
        var bar = new GameObject("MatchHPBar");
        bar.transform.SetParent(canvas.transform, false);
        bar.transform.SetAsFirstSibling();

        var barRT       = bar.AddComponent<RectTransform>();
        barRT.anchorMin = new Vector2(0f, 1f);
        barRT.anchorMax = new Vector2(1f, 1f);
        barRT.pivot     = new Vector2(0.5f, 1f);
        barRT.offsetMin = Vector2.zero;
        barRT.offsetMax = Vector2.zero;
        barRT.sizeDelta = new Vector2(0f, 48f);

        var bg   = bar.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.72f);

        var hlg = bar.AddComponent<HorizontalLayoutGroup>();
        hlg.childForceExpandWidth  = true;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth      = true;
        hlg.childControlHeight     = true;
        hlg.padding                = new RectOffset(12, 12, 6, 6);
        hlg.spacing                = 8f;

        // ── Lewa sekcja: MójNick + Serca ─────────────────────────────────
        var leftGO  = new GameObject("Left");
        leftGO.transform.SetParent(bar.transform, false);
        var leftHLG = leftGO.AddComponent<HorizontalLayoutGroup>();
        leftHLG.childAlignment        = TextAnchor.MiddleLeft;
        leftHLG.childForceExpandWidth  = false;
        leftHLG.childForceExpandHeight = true;
        leftHLG.childControlWidth      = true;
        leftHLG.childControlHeight     = true;
        leftHLG.spacing                = 10f;
        leftGO.AddComponent<LayoutElement>().flexibleWidth = 1f;

        var myNameTMP   = MakeLabel(leftGO.transform, "MyName",   "Gracz",  16f, Color.white,                  TextAlignmentOptions.Left);
        var myHeartsTMP = MakeLabel(leftGO.transform, "MyHearts", "♥♥♥",   20f, new Color(0.9f, 0.15f, 0.15f), TextAlignmentOptions.Left);

        // ── Środkowa sekcja: Timer ────────────────────────────────────────
        var centerGO = new GameObject("Center");
        centerGO.transform.SetParent(bar.transform, false);
        centerGO.AddComponent<LayoutElement>().minWidth = 80f;

        var timerTMP      = MakeLabel(centerGO.transform, "Timer", "60", 24f, new Color(1f, 0.88f, 0.15f), TextAlignmentOptions.Center);
        timerTMP.fontStyle = FontStyles.Bold;

        // ── Prawa sekcja: Serca + NickPrzeciwnika ─────────────────────────
        var rightGO  = new GameObject("Right");
        rightGO.transform.SetParent(bar.transform, false);
        var rightHLG = rightGO.AddComponent<HorizontalLayoutGroup>();
        rightHLG.childAlignment        = TextAnchor.MiddleRight;
        rightHLG.childForceExpandWidth  = false;
        rightHLG.childForceExpandHeight = true;
        rightHLG.childControlWidth      = true;
        rightHLG.childControlHeight     = true;
        rightHLG.spacing                = 10f;
        rightGO.AddComponent<LayoutElement>().flexibleWidth = 1f;

        var enemyHeartsTMP = MakeLabel(rightGO.transform, "EnemyHearts", "♥♥♥",    20f, new Color(0.9f, 0.15f, 0.15f), TextAlignmentOptions.Right);
        var enemyNameTMP   = MakeLabel(rightGO.transform, "EnemyName",   "Gracz 2", 16f, Color.white,                  TextAlignmentOptions.Right);

        // ── Komponent MatchHPUI na pasku ──────────────────────────────────
        var hpUI = bar.AddComponent<MatchHPUI>();

        var so = new SerializedObject(hpUI);
        so.FindProperty("myNameText").objectReferenceValue      = myNameTMP;
        so.FindProperty("myHeartsText").objectReferenceValue    = myHeartsTMP;
        so.FindProperty("timerText").objectReferenceValue       = timerTMP;
        so.FindProperty("enemyHeartsText").objectReferenceValue = enemyHeartsTMP;
        so.FindProperty("enemyNameText").objectReferenceValue   = enemyNameTMP;
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(hpUI);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log("[MatchHPUIBuilder] Pasek HP wygenerowany i podłączony.");
        Selection.activeGameObject = bar;
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    static TextMeshProUGUI MakeLabel(Transform parent, string goName, string text,
        float fontSize, Color color, TextAlignmentOptions align)
    {
        var go  = new GameObject(goName);
        go.transform.SetParent(parent, false);
        go.AddComponent<LayoutElement>().flexibleWidth = 1f;

        var tmp           = go.AddComponent<TextMeshProUGUI>();
        tmp.text          = text;
        tmp.fontSize      = fontSize;
        tmp.color         = color;
        tmp.fontStyle     = FontStyles.Bold;
        tmp.alignment     = align;
        tmp.raycastTarget = false;
        return tmp;
    }

    static void RemoveIfExists(Transform parent, string childName)
    {
        Transform t = parent.Find(childName);
        if (t != null) Object.DestroyImmediate(t.gameObject);
    }
}
