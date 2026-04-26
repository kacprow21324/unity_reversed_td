using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public class AIEnhancedMenuCreator : EditorWindow
{
    [MenuItem("Narz�dzia AI/Wygeneruj Gotowe Menu")]
    public static void CreateCompleteMenuSystem()
    {
        // 1. Tworzymy Canvas
        GameObject canvasGo = new GameObject("FullMenuCanvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); // Zak�adamy bazow� rozdzielczo�� FullHD
        canvasGo.AddComponent<GraphicRaycaster>();

        // Event System
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject esGo = new GameObject("EventSystem");
            esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // --- DODAJEMY SKRYPT LOGIKI DO CANVASA ---
        MainMenuLogic logic = canvasGo.AddComponent<MainMenuLogic>();

        // 2. TWORZYMY PANELE BAZOWE
        GameObject mainPanelGo = CreatePanel(canvasGo.transform, "MainMenuPanel", LoadSprite("image_0"));
        GameObject mapSelectionPanelGo = CreatePanel(canvasGo.transform, "MapSelectionPanel", LoadSprite("image_1"));
        GameObject settingsPanelGo = CreateSimplePlaceholderPanel(canvasGo.transform, "SettingsPanel", "USTAWIENIA");
        GameObject multiPanelGo = CreateSimplePlaceholderPanel(canvasGo.transform, "MultiplayerPanel", "MULTI-PLAYER");
        GameObject tutorialPanelGo = CreateSimplePlaceholderPanel(canvasGo.transform, "TutorialPanel", "TUTORIAL");

        // --- PRZYPISUJEMY PANELE DO LOGIKI ---
        logic.mainMenuPanel = mainPanelGo;
        logic.mapSelectionPanel = mapSelectionPanelGo;
        logic.settingsPanel = settingsPanelGo;
        logic.multiPanel = multiPanelGo;
        logic.tutorialPanel = tutorialPanelGo;

        // ---------------------------------------------------------
        // 3. BUDUJEMY MAIN MENU (Z Pozycjonowaniem na slotach)
        // ---------------------------------------------------------

        // Okr�g�e Logo (W lewym g�rnym rogu)
        GameObject logoGo = new GameObject("Logo_Okragle");
        logoGo.transform.SetParent(mainPanelGo.transform, false);
        RectTransform logoRt = logoGo.AddComponent<RectTransform>();
        logoRt.anchorMin = new Vector2(0, 1); logoRt.anchorMax = new Vector2(0, 1); // Lewy g�rny r�g
        logoRt.pivot = new Vector2(0, 1);
        logoRt.anchoredPosition = new Vector2(50, -50); // Odst�p od lewego i g�rnego brzegu
        logoRt.sizeDelta = new Vector2(250, 250);
        Image logoImg = logoGo.AddComponent<Image>();
        logoImg.color = new Color(0.4f, 0.4f, 0.4f, 0.8f);
        // U�ywamy wbudowanego k�ka Unity jako maski/t�a
        logoImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        CreateText(logoGo.transform, "LOGO", 50, true);

        // Przyciski G��wne (R�czne pozycjonowanie na metalowych slotach)
        // Warto�ci anchoredPosition na osi Y musisz ewentualnie lekko poprawi� w Unity, 
        // �eby idealnie wpasowa�y si� w narysowane przez Ciebie szpary.
        Button btnTutorial = CreateMenuButton(mainPanelGo.transform, "Tutorial_Button", "TUTORIAL", new Vector2(250, 350));
        Button btnSingle = CreateMenuButton(mainPanelGo.transform, "SinglePlayer_Button", "SINGLE-PLAYER", new Vector2(250, 150));
        Button btnMulti = CreateMenuButton(mainPanelGo.transform, "Multiplayer_Button", "MULTI-PLAYER", new Vector2(250, -50));
        Button btnSettings = CreateMenuButton(mainPanelGo.transform, "Settings_Button", "USTAWIENIA", new Vector2(250, -250));

        // Przycisk Wyj�cia (Prawy dolny r�g)
        Button btnExit = CreateMenuButton(mainPanelGo.transform, "ShutDown_Button", "SHUT DOWN", new Vector2(1700, -950));
        btnExit.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 80);
        btnExit.GetComponentInChildren<TextMeshProUGUI>().color = new Color(0.8f, 0.2f, 0.2f); // Czerwony napis

        // ---------------------------------------------------------
        // 4. BUDUJEMY EKRAN WYBORU MAPY
        // ---------------------------------------------------------
        string[] mapNames = { "teren1", "teren2", "teren3" };
        for (int i = 0; i < 3; i++)
        {
            // Pozycjonowanie przycisk�w mapy w poziomie (Dostosuj X je�li trzeba)
            float posX = 400f + (i * 450f);
            Button mapBtn = CreateMenuButton(mapSelectionPanelGo.transform, $"Map_{mapNames[i]}_Button", $"MAPA: {mapNames[i]}", new Vector2(posX, 0));
            mapBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 300); // Kwadratowe przyciski
            mapBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(posX, 100);

            // Automatyczne podpi�cie logiki z argumentem string!
            UnityAction<string> actionMap = new UnityAction<string>(logic.OnClickMap);
            UnityEditor.Events.UnityEventTools.AddStringPersistentListener(mapBtn.onClick, actionMap, mapNames[i]);
        }

        // Przycisk Powrotu
        Button btnBackMaps = CreateMenuButton(mapSelectionPanelGo.transform, "Back_Button", "POWR�T", new Vector2(1700, -950));

        // ---------------------------------------------------------
        // 5. AUTOMATYCZNE OPROGRAMOWANIE POZOSTA�YCH PRZYCISK�W
        // ---------------------------------------------------------
        UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(btnSingle.onClick, new UnityAction(logic.OnClickSinglePlayer));
        UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(btnMulti.onClick, new UnityAction(logic.OnClickMultiplayer));
        UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(btnSettings.onClick, new UnityAction(logic.OnClickSettings));
        UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(btnTutorial.onClick, new UnityAction(logic.OnClickTutorial));
        UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(btnExit.onClick, new UnityAction(logic.OnClickShutDown));
        UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(btnBackMaps.onClick, new UnityAction(logic.OnClickBack));

        // Podpinamy powr�t w placeholderach
        AttachBackToPlaceholder(settingsPanelGo, logic);
        AttachBackToPlaceholder(multiPanelGo, logic);
        AttachBackToPlaceholder(tutorialPanelGo, logic);

        // Ukrywamy pozosta�e panele na start
        mapSelectionPanelGo.SetActive(false);
        settingsPanelGo.SetActive(false);
        multiPanelGo.SetActive(false);
        tutorialPanelGo.SetActive(false);

        Debug.Log("Menu zosta�o wygenerowane i w pe�ni oprogramowane!");
    }

    // --- FUNKCJE POMOCNICZE ---

    private static Sprite LoadSprite(string textureName)
    {
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/{textureName}.png");
        if (tex == null) return null;
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
    }

    private static GameObject CreatePanel(Transform parent, string name, Sprite sprite)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        if (sprite != null) go.AddComponent<Image>().sprite = sprite;
        return go;
    }

    private static Button CreateMenuButton(Transform parent, string name, string text, Vector2 position)
    {
        GameObject btnGo = new GameObject(name);
        btnGo.transform.SetParent(parent, false);
        RectTransform rt = btnGo.AddComponent<RectTransform>();

        // Pozycjonowanie wzgl�dem lewego g�rnego rogu (�atwiej dopasowa� do grafiki t�a)
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(position.x, -position.y); // Odwracamy Y �eby by�o intuicyjnie
        rt.sizeDelta = new Vector2(350, 80); // Wielko�� pasuj�ca do pod�u�nego slotu

        // P�przezroczyste, ciemne t�o przycisku (mo�esz da� tu alpha na 0 je�li chcesz by t�o przycisku by�o w 100% z obrazka pod spodem)
        Image img = btnGo.AddComponent<Image>();
        img.color = new Color(0.1f, 0.1f, 0.1f, 0.0f); // 0.0f = Niewidzialny przycisk! Wida� tylko tekst i grafik� pod spodem

        Button btn = btnGo.AddComponent<Button>();

        CreateText(btnGo.transform, text, 36, true);
        return btn;
    }

    private static void CreateText(Transform parent, string text, float fontSize, bool center)
    {
        GameObject textGo = new GameObject("Text");
        textGo.transform.SetParent(parent, false);
        RectTransform rt = textGo.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; // Tekst wype�nia ca�y przycisk
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = Color.white;
        tmp.fontStyle = FontStyles.Bold;
        if (center) tmp.alignment = TextAlignmentOptions.Center;
    }

    private static GameObject CreateSimplePlaceholderPanel(Transform parent, string name, string text)
    {
        GameObject panelGo = CreatePanel(parent, name, null);
        panelGo.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
        CreateText(panelGo.transform, text, 80, true);
        return panelGo;
    }

    private static void AttachBackToPlaceholder(GameObject panelGo, MainMenuLogic logic)
    {
        Button btnBack = CreateMenuButton(panelGo.transform, "Back_Button", "POWR�T", new Vector2(1700, -950));
        UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(btnBack.onClick, new UnityAction(logic.OnClickBack));
    }
}