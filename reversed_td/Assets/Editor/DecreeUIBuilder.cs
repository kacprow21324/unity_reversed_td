using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class DecreeUIBuilder
{
    [MenuItem("AI-Tools/Generate Decree UI")]
    public static void Generate()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("DecreeUIBuilder: Brak Canvas w scenie. Dodaj Canvas i sprobuj ponownie.");
            return;
        }

        // Usun stary panel, jesli istnieje
        Transform existing = canvas.transform.Find("DecreePanel");
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);

        // ── Tlo panelu (pelny ekran) ───────────────────────────────────────
        var panel   = new GameObject("DecreePanel");
        panel.transform.SetParent(canvas.transform, false);
        var panelRT = panel.AddComponent<RectTransform>();
        panelRT.anchorMin   = Vector2.zero;
        panelRT.anchorMax   = Vector2.one;
        panelRT.offsetMin   = Vector2.zero;
        panelRT.offsetMax   = Vector2.zero;

        var panelImg  = panel.AddComponent<Image>();
        panelImg.color = new Color(0f, 0f, 0f, 0.78f);

        // ── Tytul panelu ──────────────────────────────────────────────────
        var titleGO  = new GameObject("PanelTitle");
        titleGO.transform.SetParent(panel.transform, false);
        var titleRT  = titleGO.AddComponent<RectTransform>();
        titleRT.anchorMin       = new Vector2(0.05f, 0.80f);
        titleRT.anchorMax       = new Vector2(0.95f, 0.93f);
        titleRT.offsetMin       = Vector2.zero;
        titleRT.offsetMax       = Vector2.zero;

        var titleTMP            = titleGO.AddComponent<TextMeshProUGUI>();
        titleTMP.text           = "WYBIERZ DEKRET";
        titleTMP.enableAutoSizing = true;
        titleTMP.fontSizeMin    = 20f;
        titleTMP.fontSizeMax    = 42f;
        titleTMP.fontStyle      = FontStyles.Bold;
        titleTMP.color          = new Color(1f, 0.85f, 0.20f);
        titleTMP.alignment      = TextAlignmentOptions.Center;

        // ── Kontener kart z HorizontalLayoutGroup ─────────────────────────
        var cardsGO = new GameObject("Cards");
        cardsGO.transform.SetParent(panel.transform, false);
        var cardsRT = cardsGO.AddComponent<RectTransform>();
        cardsRT.anchorMin   = new Vector2(0.04f, 0.10f);
        cardsRT.anchorMax   = new Vector2(0.96f, 0.78f);
        cardsRT.offsetMin   = Vector2.zero;
        cardsRT.offsetMax   = Vector2.zero;

        var hlg                    = cardsGO.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing                = 20f;
        hlg.childForceExpandWidth  = true;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth      = true;
        hlg.childControlHeight     = true;
        hlg.padding                = new RectOffset(8, 8, 8, 8);
        hlg.childAlignment         = TextAnchor.MiddleCenter;

        // ── 3 karty – przyciski ───────────────────────────────────────────
        var titleRefs  = new TextMeshProUGUI[3];
        var changeRefs = new TextMeshProUGUI[3];
        var btnRefs    = new Button[3];

        for (int i = 0; i < 3; i++)
        {
            TextMeshProUGUI t, c;
            Button          b;
            BuildCard(cardsGO.transform, i, out t, out c, out b);
            titleRefs[i]  = t;
            changeRefs[i] = c;
            btnRefs[i]    = b;
        }

        // ── Odliczanie czasu ───────────────────────────────────────────────
        var timerGO  = new GameObject("DecreeTimer");
        timerGO.transform.SetParent(panel.transform, false);
        var timerRT  = timerGO.AddComponent<RectTransform>();
        timerRT.anchorMin       = new Vector2(0.3f, 0.10f);
        timerRT.anchorMax       = new Vector2(0.7f, 0.20f);
        timerRT.offsetMin       = Vector2.zero;
        timerRT.offsetMax       = Vector2.zero;
        var timerTMP            = timerGO.AddComponent<TextMeshProUGUI>();
        timerTMP.text           = "Czas na wybor: 30s";
        timerTMP.enableAutoSizing = true;
        timerTMP.fontSizeMin    = 14f;
        timerTMP.fontSizeMax    = 26f;
        timerTMP.fontStyle      = FontStyles.Bold;
        timerTMP.color          = Color.white;
        timerTMP.alignment      = TextAlignmentOptions.Center;

        // ── Podpowiedz ────────────────────────────────────────────────────
        var hintGO  = new GameObject("Hint");
        hintGO.transform.SetParent(panel.transform, false);
        var hintRT  = hintGO.AddComponent<RectTransform>();
        hintRT.anchorMin       = new Vector2(0.05f, 0.03f);
        hintRT.anchorMax       = new Vector2(0.95f, 0.10f);
        hintRT.offsetMin       = Vector2.zero;
        hintRT.offsetMax       = Vector2.zero;
        var hintTMP            = hintGO.AddComponent<TextMeshProUGUI>();
        hintTMP.text           = "Kliknij karte, aby wybrac dekret. Po uplywie czasu wybrany zostanie pierwszy dekret.";
        hintTMP.fontSize       = 13f;
        hintTMP.color          = new Color(0.65f, 0.65f, 0.65f);
        hintTMP.alignment      = TextAlignmentOptions.Center;

        // ── Podlacz do GameplayUIManager ──────────────────────────────────
        var uiManager = Object.FindFirstObjectByType<GameplayUIManager>();
        if (uiManager != null)
        {
            uiManager.decreePanel     = panel;
            uiManager.decreeTitles    = titleRefs;
            uiManager.decreeChanges   = changeRefs;
            uiManager.decreeButtons   = btnRefs;
            uiManager.decreeTimerText = timerTMP;
            EditorUtility.SetDirty(uiManager);
            Debug.Log("DecreeUIBuilder: Referencje przypisane do GameplayUIManager (z timerem).");
        }
        else
        {
            Debug.LogWarning("DecreeUIBuilder: Nie znaleziono GameplayUIManager – przypisz referencje recznie w inspektorze.");
        }

        panel.SetActive(false);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = panel;
        Debug.Log("DecreeUIBuilder: Panel Dekretow wygenerowany pomyslnie.");
    }

    static void BuildCard(Transform parent, int index,
        out TextMeshProUGUI titleOut, out TextMeshProUGUI changeOut, out Button btnOut)
    {
        // Kontener przycisku
        var cardGO  = new GameObject("Card_" + index);
        cardGO.transform.SetParent(parent, false);

        var cardImg = cardGO.AddComponent<Image>();
        cardImg.color = new Color(0.10f, 0.14f, 0.24f, 1f);

        var btn     = cardGO.AddComponent<Button>();
        btn.targetGraphic = cardImg;
        var bc      = btn.colors;
        bc.normalColor      = new Color(0.10f, 0.14f, 0.24f);
        bc.highlightedColor = new Color(0.20f, 0.32f, 0.52f);
        bc.pressedColor     = new Color(0.05f, 0.08f, 0.14f);
        bc.selectedColor    = new Color(0.16f, 0.26f, 0.42f);
        btn.colors  = bc;

        // Uklad pionowy
        var vlg                    = cardGO.AddComponent<VerticalLayoutGroup>();
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth      = true;
        vlg.childControlHeight     = true;
        vlg.childAlignment         = TextAnchor.UpperCenter;
        vlg.padding                = new RectOffset(14, 14, 18, 18);
        vlg.spacing                = 12f;

        // a) Ikona (Image placeholder)
        var iconGO  = new GameObject("Icon");
        iconGO.transform.SetParent(cardGO.transform, false);
        var iconImg = iconGO.AddComponent<Image>();
        iconImg.color          = new Color(0.20f, 0.30f, 0.52f, 1f);
        var iconLE             = iconGO.AddComponent<LayoutElement>();
        iconLE.preferredHeight = 90f;
        iconLE.flexibleHeight  = 0f;
        iconLE.minHeight       = 60f;

        // b) Tytul buffa (Auto Size)
        var titleGO  = new GameObject("Title");
        titleGO.transform.SetParent(cardGO.transform, false);
        var titleTMP               = titleGO.AddComponent<TextMeshProUGUI>();
        titleTMP.text              = "Dekret " + (index + 1);
        titleTMP.enableAutoSizing  = true;
        titleTMP.fontSizeMin       = 10f;
        titleTMP.fontSizeMax       = 20f;
        titleTMP.color             = Color.white;
        titleTMP.fontStyle         = FontStyles.Bold;
        titleTMP.alignment         = TextAlignmentOptions.Center;
        var titleLE                = titleGO.AddComponent<LayoutElement>();
        titleLE.preferredHeight    = 56f;
        titleLE.flexibleHeight     = 0f;
        titleLE.minHeight          = 36f;

        // c) Wyliczenie zmiany (Auto Size)
        var changeGO  = new GameObject("Change");
        changeGO.transform.SetParent(cardGO.transform, false);
        var changeTMP              = changeGO.AddComponent<TextMeshProUGUI>();
        changeTMP.text             = "100 -> 120";
        changeTMP.enableAutoSizing = true;
        changeTMP.fontSizeMin      = 12f;
        changeTMP.fontSizeMax      = 26f;
        changeTMP.color            = new Color(0.35f, 1f, 0.55f);
        changeTMP.fontStyle        = FontStyles.Bold;
        changeTMP.alignment        = TextAlignmentOptions.Center;
        var changeLE               = changeGO.AddComponent<LayoutElement>();
        changeLE.preferredHeight   = 40f;
        changeLE.flexibleHeight    = 0f;
        changeLE.minHeight         = 28f;

        titleOut  = titleTMP;
        changeOut = changeTMP;
        btnOut    = btn;
    }
}
