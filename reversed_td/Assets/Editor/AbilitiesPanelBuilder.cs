using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// Menu: Narzędzia AI → Wygeneruj Panel Mocy Taktycznych
/// Tworzy AbilitiesPanel z 3 gotowymi guzikami (ikona, nazwa, koszt).
/// Wymagania: GameplayUIManager w scenie + opcjonalnie TacticalAbilities.
public class AbilitiesPanelBuilder
{
    // ── Paleta ────────────────────────────────────────────────────────────
    static readonly Color C_Panel     = new Color(0.10f, 0.12f, 0.16f, 0.97f);
    static readonly Color C_BtnBg     = new Color(0.12f, 0.18f, 0.32f, 1.00f);
    static readonly Color C_IconBg    = new Color(0.20f, 0.32f, 0.55f, 1.00f);
    static readonly Color C_Text      = new Color(0.92f, 0.93f, 0.95f, 1.00f);
    static readonly Color C_Gold      = new Color(1.00f, 0.85f, 0.20f, 1.00f);
    static readonly Color C_Highlight = new Color(0.22f, 0.35f, 0.55f, 1.00f);
    static readonly Color C_Pressed   = new Color(0.06f, 0.10f, 0.18f, 1.00f);

    [MenuItem("Narzędzia AI/Wygeneruj Panel Mocy Taktycznych")]
    public static void Build()
    {
        var uiManager = Object.FindFirstObjectByType<GameplayUIManager>();
        if (uiManager == null)
        {
            Debug.LogError("AbilitiesPanelBuilder: Nie znaleziono GameplayUIManager w scenie.");
            return;
        }

        Canvas canvas = uiManager.GetComponentInParent<Canvas>()
                     ?? Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("AbilitiesPanelBuilder: Nie znaleziono Canvas w scenie.");
            return;
        }

        // Usuń stary panel
        if (uiManager.abilitiesPanel != null)
        {
            Object.DestroyImmediate(uiManager.abilitiesPanel);
            uiManager.abilitiesPanel = null;
        }

        // Rodzic: ten sam co queuePanel, lub bezpośrednio Canvas
        Transform panelParent = uiManager.queuePanel != null
            ? uiManager.queuePanel.transform.parent
            : canvas.transform;

        // ── Panel główny ──────────────────────────────────────────────────
        var panelGO = new GameObject("AbilitiesPanel");
        panelGO.transform.SetParent(panelParent, false);

        // Kopiuj pozycję/anchory z queuePanel, ale wymuś minimalny rozmiar
        var rt = panelGO.AddComponent<RectTransform>();
        if (uiManager.queuePanel != null)
        {
            var src             = uiManager.queuePanel.GetComponent<RectTransform>();
            rt.anchorMin        = src.anchorMin;
            rt.anchorMax        = src.anchorMax;
            rt.pivot            = src.pivot;
            rt.anchoredPosition = src.anchoredPosition;
            // Szerokość co najmniej 400, wysokość co najmniej 90
            rt.sizeDelta = new Vector2(
                Mathf.Max(src.sizeDelta.x, 400f),
                Mathf.Max(src.sizeDelta.y, 90f));
        }
        else
        {
            rt.anchorMin        = new Vector2(0.5f, 0f);
            rt.anchorMax        = new Vector2(0.5f, 0f);
            rt.pivot            = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 10f);
            rt.sizeDelta        = new Vector2(420f, 90f);
        }

        panelGO.AddComponent<Image>().color = C_Panel;

        var hlg = panelGO.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing                = 8f;
        hlg.childForceExpandWidth  = true;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth      = true;
        hlg.childControlHeight     = true;
        hlg.padding                = new RectOffset(10, 10, 8, 8);

        // Pobierz koszty z TacticalAbilities jeśli jest w scenie
        var ta            = Object.FindFirstObjectByType<TacticalAbilities>();
        int airstrikeCost = ta != null ? (int)ta.airstrikeCost : 100;
        int shieldCost    = ta != null ? (int)ta.shieldCost    : 75;
        int boostCost     = ta != null ? (int)ta.boostCost     : 50;

        CreateAbilityButton("Nalot",  airstrikeCost, panelGO.transform);
        CreateAbilityButton("Tarcza", shieldCost,    panelGO.transform);
        CreateAbilityButton("Boost",  boostCost,     panelGO.transform);

        // Panel startowo ukryty (faza planowania)
        panelGO.SetActive(false);

        // Przypisz referencję i zapisz scenę
        uiManager.abilitiesPanel = panelGO;
        EditorUtility.SetDirty(uiManager);
        EditorSceneManager.MarkSceneDirty(uiManager.gameObject.scene);

        Selection.activeGameObject = panelGO;
        Debug.Log("[AbilitiesPanelBuilder] Panel mocy taktycznych wygenerowany! Zapisz scene (Ctrl+S).");
    }

    // ── Guzik mocy: [ikona | nazwa / koszt] ──────────────────────────────
    static void CreateAbilityButton(string abilityName, int cost, Transform parent)
    {
        var btnGO = new GameObject($"Ability_{abilityName}");
        btnGO.transform.SetParent(parent, false);

        var img = btnGO.AddComponent<Image>();
        img.color = C_BtnBg;

        var btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = img;
        var bc = btn.colors;
        bc.normalColor      = C_BtnBg;
        bc.highlightedColor = C_Highlight;
        bc.pressedColor     = C_Pressed;
        bc.disabledColor    = new Color(0.15f, 0.15f, 0.15f, 0.5f);
        btn.colors = bc;

        // Poziomy: ikona po lewej, teksty po prawej
        var hlg = btnGO.AddComponent<HorizontalLayoutGroup>();
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth      = true;
        hlg.childControlHeight     = true;
        hlg.padding                = new RectOffset(8, 8, 6, 6);
        hlg.spacing                = 8f;
        hlg.childAlignment         = TextAnchor.MiddleLeft;

        // Ikona kwadratowa po lewej
        var iconGO  = new GameObject("Icon");
        iconGO.transform.SetParent(btnGO.transform, false);
        var iconImg = iconGO.AddComponent<Image>();
        iconImg.color          = C_IconBg;
        iconImg.preserveAspect = true;
        var iconLE             = iconGO.AddComponent<LayoutElement>();
        iconLE.minWidth        = 48f;
        iconLE.preferredWidth  = 48f;
        iconLE.flexibleWidth   = 0f;

        // Blok tekstowy po prawej (pionowo: nazwa nad kosztem)
        var textGO = new GameObject("Texts");
        textGO.transform.SetParent(btnGO.transform, false);
        var vlg = textGO.AddComponent<VerticalLayoutGroup>();
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth      = true;
        vlg.childControlHeight     = true;
        vlg.childAlignment         = TextAnchor.MiddleCenter;
        vlg.spacing                = 2f;
        textGO.AddComponent<LayoutElement>().flexibleWidth = 1f;

        // Nazwa mocy
        var nameGO  = new GameObject("Name");
        nameGO.transform.SetParent(textGO.transform, false);
        var nameTxt              = nameGO.AddComponent<TextMeshProUGUI>();
        nameTxt.text             = abilityName;
        nameTxt.fontSize         = 15f;
        nameTxt.enableAutoSizing = true;
        nameTxt.fontSizeMin      = 10f;
        nameTxt.fontSizeMax      = 15f;
        nameTxt.color            = C_Text;
        nameTxt.fontStyle        = FontStyles.Bold;
        nameTxt.alignment        = TextAlignmentOptions.Center;
        var nameLE               = nameGO.AddComponent<LayoutElement>();
        nameLE.preferredHeight   = 22f;
        nameLE.flexibleHeight    = 0f;

        // Koszt
        var costGO  = new GameObject("Cost");
        costGO.transform.SetParent(textGO.transform, false);
        var costTxt            = costGO.AddComponent<TextMeshProUGUI>();
        costTxt.text           = $"{cost} Z";
        costTxt.fontSize       = 13f;
        costTxt.color          = C_Gold;
        costTxt.fontStyle      = FontStyles.Bold;
        costTxt.alignment      = TextAlignmentOptions.Center;
        var costLE             = costGO.AddComponent<LayoutElement>();
        costLE.preferredHeight = 18f;
        costLE.flexibleHeight  = 0f;
    }
}
