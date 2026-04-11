using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class GameplayUIBuilder : EditorWindow
{
    [MenuItem("Narzêdzia AI/Wygeneruj Interfejs Rozgrywki (Z danych)")]
    public static void CreateGameplayUI()
    {
        // 1. Tworzymy Canvas
        GameObject canvasGo = new GameObject("GameplayCanvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGo.AddComponent<GraphicRaycaster>();

        // 2. Podpinamy Managera i ³adujemy plik konfiguracyjny
        GameplayUIManager uiManager = canvasGo.AddComponent<GameplayUIManager>();
        uiManager.config = GetOrCreateGameConfig();

        // 3. Dolny Panel T³a
        GameObject bottomBar = new GameObject("BottomPanel_Background");
        bottomBar.transform.SetParent(canvasGo.transform, false);
        RectTransform barRt = bottomBar.AddComponent<RectTransform>();
        barRt.anchorMin = new Vector2(0, 0);
        barRt.anchorMax = new Vector2(1, 0.2f);
        barRt.offsetMin = Vector2.zero; barRt.offsetMax = Vector2.zero;
        bottomBar.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 1f);

        // 4. Strefa Pojazdów
        GameObject vehiclesArea = new GameObject("Vehicles_Area");
        vehiclesArea.transform.SetParent(bottomBar.transform, false);
        RectTransform vehRt = vehiclesArea.AddComponent<RectTransform>();
        vehRt.anchorMin = new Vector2(0, 0); vehRt.anchorMax = new Vector2(0.7f, 1f);
        vehRt.offsetMin = new Vector2(20, 20); vehRt.offsetMax = new Vector2(-20, -20);

        HorizontalLayoutGroup hlg = vehiclesArea.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 20;
        hlg.childControlWidth = true; hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true; hlg.childForceExpandHeight = true;

        for (int i = 0; i < 5; i++)
        {
            uiManager.vehicleSlots[i] = CreateVehicleButton(vehiclesArea.transform, $"Slot_Pojazdu_{i + 1}");
        }

        // 5. Strefa Statystyk
        GameObject statsArea = new GameObject("Stats_Area");
        statsArea.transform.SetParent(bottomBar.transform, false);
        RectTransform statsRt = statsArea.AddComponent<RectTransform>();
        statsRt.anchorMin = new Vector2(0.75f, 0); statsRt.anchorMax = new Vector2(1f, 1f);
        statsRt.offsetMin = new Vector2(0, 20); statsRt.offsetMax = new Vector2(-20, -20);

        VerticalLayoutGroup vlg = statsArea.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 20;
        vlg.childAlignment = TextAnchor.MiddleRight;

        uiManager.roundText = CreateStatText(statsArea.transform, "Runda_Text", "Runda 1/1", 40, Color.white);
        uiManager.goldText = CreateStatText(statsArea.transform, "Gold_Text", "Z³oto: 0", 45, new Color(1f, 0.8f, 0f));

        Debug.Log("Interfejs stworzony i podpiêty pod GameConfig!");
    }

    private static GameConfig GetOrCreateGameConfig()
    {
        GameConfig config = AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/UstawieniaEkonomii.asset");
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<GameConfig>();
            config.startingGold = 200;
            config.goldPerWin = 100;
            config.goldPerEscapedVehicle = 10;

            config.vehicles = new VehicleConfig[5];
            config.vehicles[0] = new VehicleConfig { vehicleName = "Wóz Tank (Czo³g)", cost = 50 };
            config.vehicles[1] = new VehicleConfig { vehicleName = "Wóz Dalekosiê¿ny", cost = 25 };
            config.vehicles[2] = new VehicleConfig { vehicleName = "Wóz Zasadzka", cost = 30 };
            config.vehicles[3] = new VehicleConfig { vehicleName = "Wóz Lustrzany", cost = 45 };
            config.vehicles[4] = new VehicleConfig { vehicleName = "Wóz Podstawowy", cost = 20 };

            AssetDatabase.CreateAsset(config, "Assets/UstawieniaEkonomii.asset");
            AssetDatabase.SaveAssets();
        }
        return config;
    }

    private static GameplayUIManager.VehicleUISlot CreateVehicleButton(Transform parent, string name)
    {
        GameObject btnGo = new GameObject(name);
        btnGo.transform.SetParent(parent, false);
        btnGo.AddComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f);
        Button btn = btnGo.AddComponent<Button>();

        GameObject iconGo = new GameObject("Icon");
        iconGo.transform.SetParent(btnGo.transform, false);
        RectTransform iconRt = iconGo.AddComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0.05f, 0.35f); iconRt.anchorMax = new Vector2(0.95f, 0.95f);
        iconRt.offsetMin = Vector2.zero; iconRt.offsetMax = Vector2.zero;
        Image iconImg = iconGo.AddComponent<Image>();
        iconImg.color = new Color(0.5f, 0.5f, 0.5f);

        GameObject nameGo = new GameObject("NameText");
        nameGo.transform.SetParent(btnGo.transform, false);
        RectTransform nameRt = nameGo.AddComponent<RectTransform>();
        nameRt.anchorMin = new Vector2(0, 0.15f); nameRt.anchorMax = new Vector2(1, 0.35f);
        nameRt.offsetMin = Vector2.zero; nameRt.offsetMax = Vector2.zero;
        TextMeshProUGUI nameTmp = nameGo.AddComponent<TextMeshProUGUI>();
        nameTmp.text = "Nazwa";
        nameTmp.alignment = TextAlignmentOptions.Center;
        nameTmp.fontSize = 20;

        GameObject costGo = new GameObject("CostText");
        costGo.transform.SetParent(btnGo.transform, false);
        RectTransform costRt = costGo.AddComponent<RectTransform>();
        costRt.anchorMin = new Vector2(0, 0); costRt.anchorMax = new Vector2(1, 0.15f);
        costRt.offsetMin = Vector2.zero; costRt.offsetMax = Vector2.zero;
        TextMeshProUGUI costTmp = costGo.AddComponent<TextMeshProUGUI>();
        costTmp.text = "0";
        costTmp.color = new Color(1f, 0.8f, 0f);
        costTmp.alignment = TextAlignmentOptions.Center;
        costTmp.fontSize = 18;

        return new GameplayUIManager.VehicleUISlot
        {
            button = btn,
            iconImage = iconImg,
            nameText = nameTmp,
            costText = costTmp
        };
    }

    private static TextMeshProUGUI CreateStatText(Transform parent, string name, string text, float size, Color color)
    {
        GameObject textGo = new GameObject(name);
        textGo.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.MidlineRight;
        return tmp;
    }
}