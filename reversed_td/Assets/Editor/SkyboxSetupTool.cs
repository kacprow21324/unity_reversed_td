using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// Narzędzie edytora: ustawia skybox i oświetlenie sceny jednym kliknięciem.
/// Dostępne pod: Tools → Skybox i Oświetlenie → ...
///
/// Użycie:
///   1. Otwórz dowolną scenę.
///   2. Tools → Skybox i Oświetlenie → wybrany wariant.
///   3. Ctrl+S — zapisz scenę.
public static class SkyboxSetupTool
{
    const string BASE = "Assets/Grafiki3d/AllSkyFree/";

    [MenuItem("Tools/Skybox i Oswietlenie/Zachmurzone (Overcast Low)")]
    static void SetOvercast() => Apply(
        matPath:          BASE + "Overcast Low/AllSky_Overcast4_Low.mat",
        sunColor:         new Color(0.88f, 0.88f, 0.85f),
        sunIntensity:     0.75f,
        sunPitch:         45f,
        sunYaw:           130f,
        ambientIntensity: 1.0f,
        fogColor:         new Color(0.72f, 0.72f, 0.72f),
        fogDensity:       0.004f,
        enableFog:        true
    );

    [MenuItem("Tools/Skybox i Oswietlenie/Noc z Ksiezycem (Night MoonBurst)")]
    static void SetNight() => Apply(
        matPath:          BASE + "Night MoonBurst/AllSky_Night_MoonBurst Equirect.mat",
        sunColor:         new Color(0.55f, 0.65f, 0.95f),
        sunIntensity:     0.25f,
        sunPitch:         35f,
        sunYaw:           220f,
        ambientIntensity: 0.25f,
        fogColor:         new Color(0.04f, 0.05f, 0.12f),
        fogDensity:       0.008f,
        enableFog:        true
    );

    [MenuItem("Tools/Skybox i Oswietlenie/Obca Planeta (Space AnotherPlanet)")]
    static void SetSpace() => Apply(
        matPath:          BASE + "Space_AnotherPlanet/AllSky_Space_AnotherPlanet.mat",
        sunColor:         new Color(1.0f, 0.72f, 0.38f),
        sunIntensity:     1.1f,
        sunPitch:         40f,
        sunYaw:           60f,
        ambientIntensity: 0.65f,
        fogColor:         new Color(0.18f, 0.06f, 0.22f),
        fogDensity:       0.003f,
        enableFog:        false
    );

    static void Apply(
        string matPath,
        Color  sunColor,
        float  sunIntensity,
        float  sunPitch,
        float  sunYaw,
        float  ambientIntensity,
        Color  fogColor,
        float  fogDensity,
        bool   enableFog)
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            Debug.LogError("[SkyboxSetupTool] Nie znaleziono materialu: " + matPath);
            return;
        }

        // Skybox + ambient
        RenderSettings.skybox           = mat;
        RenderSettings.ambientMode      = AmbientMode.Skybox;
        RenderSettings.ambientIntensity = ambientIntensity;

        // Mgla
        RenderSettings.fog        = enableFog;
        RenderSettings.fogColor   = fogColor;
        RenderSettings.fogMode    = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = fogDensity;

        // Odswiezenie ambient probe
        DynamicGI.UpdateEnvironment();

        // Directional Light
        Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
        Light sun = null;
        foreach (Light l in lights)
            if (l.type == LightType.Directional) { sun = l; break; }

        if (sun != null)
        {
            Undo.RecordObject(sun,             "Ustaw Oswietlenie");
            Undo.RecordObject(sun.transform,   "Ustaw Oswietlenie");
            sun.color     = sunColor;
            sun.intensity = sunIntensity;
            sun.transform.eulerAngles = new Vector3(sunPitch, sunYaw, 0f);
        }
        else
        {
            Debug.LogWarning("[SkyboxSetupTool] Brak Directional Light w scenie. " +
                             "Dodaj: GameObject > Light > Directional Light.");
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        Debug.Log("[SkyboxSetupTool] Ustawiono: " + mat.name +
                  " | Ambient: " + ambientIntensity +
                  (sun != null ? " | Slonce: " + sunIntensity : " | BRAK DIRECTIONAL LIGHT"));
    }
}
