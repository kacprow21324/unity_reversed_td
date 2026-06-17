using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// Tooltip pojazdu — wyświetla się po najechaniu na ikonę w sklepie.
/// Dodaj do dowolnego GameObject w scenie gry.
public class VehicleTooltip : MonoBehaviour
{
    public static VehicleTooltip Instance { get; private set; }

    private GameObject      _panel;
    private TextMeshProUGUI _nazwa;
    private TextMeshProUGUI _hp;
    private TextMeshProUGUI _pancerz;
    private TextMeshProUGUI _predkosc;
    private TextMeshProUGUI _moc;

    static readonly Color C_BG     = new Color(0.07f, 0.10f, 0.18f, 0.96f);
    static readonly Color C_HEADER = new Color(0.75f, 0.85f, 1.00f, 1f);
    static readonly Color C_SEP    = new Color(0.28f, 0.28f, 0.40f, 1f);
    static readonly Color C_LABEL  = new Color(0.60f, 0.70f, 0.85f, 1f);
    static readonly Color C_VALUE  = Color.white;
    static readonly Color C_MOC    = new Color(1f, 0.85f, 0.30f, 1f);

    // ── Unity lifecycle ───────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        BuildUI();
        StartCoroutine(PodpiszHovery());
    }

    // Czeka jeden frame żeby GameplayUIManager.Start() zdążył zbudować sloty
    IEnumerator PodpiszHovery()
    {
        yield return null;

        var ui = GameplayUIManager.Instance;
        if (ui == null) yield break;

        for (int i = 0; i < ui.vehicleSlots.Length; i++)
        {
            var slot = ui.vehicleSlots[i];
            if (slot?.button == null) continue;

            int idx     = i;
            var trigger = slot.button.gameObject.GetComponent<EventTrigger>()
                       ?? slot.button.gameObject.AddComponent<EventTrigger>();

            var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ => Pokaz(idx));
            trigger.triggers.Add(enter);

            var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(_ => Ukryj());
            trigger.triggers.Add(exit);
        }
    }

    // ── API ───────────────────────────────────────────────────────────────

    public void Pokaz(int index)
    {
        var ui = GameplayUIManager.Instance;
        if (ui?.config == null || index >= ui.config.vehicles.Length) return;

        var cfg = ui.config.vehicles[index];

        GameObject prefab = null;
        if (VehicleSpawner.Instance != null && index < VehicleSpawner.Instance.vehiclePrefabs.Length)
            prefab = VehicleSpawner.Instance.vehiclePrefabs[index];

        var p     = prefab?.GetComponent<pojazd>();
        var agent = prefab?.GetComponent<NavMeshAgent>();

        if (_nazwa    != null) _nazwa.text    = cfg.vehicleName;
        if (_hp       != null) _hp.text       = p != null ? $"{p.maxHp} HP" : "—";
        if (_pancerz  != null) _pancerz.text  = p != null
            ? $"{p.pancerz} (redukuje każde trafienie)"
            : "—";
        if (_predkosc != null) _predkosc.text = agent != null
            ? $"{agent.speed:F1} m/s"
            : "—";
        if (_moc      != null) _moc.text      = GetMoc(prefab);

        if (_panel != null) _panel.SetActive(true);
    }

    public void Ukryj()
    {
        if (_panel != null) _panel.SetActive(false);
    }

    // ── Opis zdolności specjalnej ─────────────────────────────────────────

    string GetMoc(GameObject prefab)
    {
        if (prefab == null) return "Brak danych.";

        if (prefab.GetComponent<PojazdTank>() != null)
            return "Przyciąga ogień wszystkich wież na siebie.\nInne pojazdy w kolumnie są bezpieczne.";

        if (prefab.GetComponent<PojazdKamikaze>() is PojazdKamikaze k)
            return $"Niewidoczny dla wież bez sonaru. Gdy wejdzie\nw zasięg wieży — wybucha ({k.obrazeniaWybuchu} HP,\npromień {k.promienWybuchu} m).";

        if (prefab.GetComponent<PojazdLustro>() != null)
            return "Odbija pociski wieży podstawowej i laser plazmowy\nz powrotem w strzelającą wieżę.\nSam nie zadaje obrażeń — dekrety mogą to zmienić.";

        if (prefab.GetComponent<PojazdArtyleria>() is PojazdArtyleria a)
            return $"Ostrzela wieże podczas przejazdu.\nZasięg {a.zasiegAtaku} m · {a.obrazeniaStrzalu} HP · co {a.cooldownStrzalu} s.";

        if (prefab.GetComponent<PojazdPodstawowy>() != null)
            return "Standardowy pojazd.\nBrak zdolności specjalnych.";

        return "Brak zdolności specjalnych.";
    }

    // ── Budowanie UI ──────────────────────────────────────────────────────

    void BuildUI()
    {
        var cGO = new GameObject("VehicleTooltipCanvas");
        cGO.transform.SetParent(transform, false);

        var canvas          = cGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 60;

        var s                 = cGO.AddComponent<CanvasScaler>();
        s.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        s.referenceResolution = new Vector2(1920f, 1080f);
        s.matchWidthOrHeight  = 0.5f;

        cGO.AddComponent<GraphicRaycaster>();

        // Panel — środek ekranu
        _panel = new GameObject("Panel");
        _panel.transform.SetParent(cGO.transform, false);

        var pRT              = _panel.AddComponent<RectTransform>();
        pRT.anchorMin        = new Vector2(0.5f, 0.5f);
        pRT.anchorMax        = new Vector2(0.5f, 0.5f);
        pRT.pivot            = new Vector2(0.5f, 0.5f);
        pRT.anchoredPosition = Vector2.zero;
        pRT.sizeDelta        = new Vector2(320f, 10f);

        _panel.AddComponent<Image>().color = C_BG;

        var vlg                    = _panel.AddComponent<VerticalLayoutGroup>();
        vlg.spacing                = 5f;
        vlg.padding                = new RectOffset(16, 16, 12, 12);
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth      = true;
        vlg.childControlHeight     = true;

        var csf         = _panel.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Zawartość
        _nazwa = Lbl("—", 18f, C_HEADER, FontStyles.Bold, 26f);
        Sep();
        _hp       = Wiersz("HP:");
        _pancerz  = Wiersz("Pancerz:");
        _predkosc = Wiersz("Prędkość:");
        Sep();
        Lbl("Zdolność specjalna:", 12f, C_LABEL, FontStyles.Bold, 18f);
        _moc                  = Lbl("—", 12f, C_MOC, FontStyles.Normal, 56f);
        _moc.textWrappingMode = TMPro.TextWrappingModes.Normal;

        _panel.SetActive(false);
    }

    TextMeshProUGUI Lbl(string txt, float size, Color col, FontStyles style, float h)
    {
        var go             = new GameObject("L");
        go.transform.SetParent(_panel.transform, false);
        go.AddComponent<LayoutElement>().preferredHeight = h;
        var t              = go.AddComponent<TextMeshProUGUI>();
        t.text             = txt;
        t.fontSize         = size;
        t.color            = col;
        t.fontStyle        = style;
        t.alignment        = TextAlignmentOptions.Left;
        t.raycastTarget    = false;
        return t;
    }

    TextMeshProUGUI Wiersz(string label)
    {
        var row = new GameObject("Row");
        row.transform.SetParent(_panel.transform, false);
        row.AddComponent<LayoutElement>().preferredHeight = 20f;

        var hlg                    = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing                = 8f;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth      = true;
        hlg.childControlHeight     = true;

        var lGO            = new GameObject("L");
        lGO.transform.SetParent(row.transform, false);
        var lLE            = lGO.AddComponent<LayoutElement>();
        lLE.preferredWidth = 100f;
        lLE.flexibleWidth  = 0f;
        var lTmp           = lGO.AddComponent<TextMeshProUGUI>();
        lTmp.text          = label;
        lTmp.fontSize      = 13f;
        lTmp.fontStyle     = FontStyles.Bold;
        lTmp.color         = C_LABEL;
        lTmp.alignment     = TextAlignmentOptions.Left;
        lTmp.raycastTarget = false;

        var vGO = new GameObject("V");
        vGO.transform.SetParent(row.transform, false);
        vGO.AddComponent<LayoutElement>().flexibleWidth = 1f;
        var vTmp           = vGO.AddComponent<TextMeshProUGUI>();
        vTmp.text          = "—";
        vTmp.fontSize      = 13f;
        vTmp.color         = C_VALUE;
        vTmp.alignment     = TextAlignmentOptions.Left;
        vTmp.raycastTarget = false;
        return vTmp;
    }

    void Sep()
    {
        var go             = new GameObject("Sep");
        go.transform.SetParent(_panel.transform, false);
        go.AddComponent<LayoutElement>().preferredHeight = 1f;
        var img            = go.AddComponent<Image>();
        img.color          = C_SEP;
        img.raycastTarget  = false;
    }
}
