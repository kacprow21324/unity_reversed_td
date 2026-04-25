using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameplayUIManager : MonoBehaviour
{
    public static GameplayUIManager Instance;

    // ── Zagnieżdżone typy ─────────────────────────────────────────────────

    [System.Serializable]
    public class QueueEntry
    {
        public int vehicleIndex;
        public int cost;
    }

    [System.Serializable]
    public class VehicleUISlot
    {
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI costText;
        public Image iconImage;
        public Button button;
    }

    // ── Inspektor ────────────────────────────────────────────────────────

    [Header("Dane")]
    public GameConfig config;

    [Header("Statystyki HUD")]
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI roundText;
    public TextMeshProUGUI phaseText;

    [Header("Tekst HUD")]
    [SerializeField] private string prefixZloto = "Zloto: ";
    [SerializeField] private string prefixRunda = "Runda: ";

    [Header("Tekst Faz")]
    [SerializeField] private string labelPlanowanie = "PLANOWANIE";
    [SerializeField] private string labelAtak       = "ATAK";
    [SerializeField] private Color  colorPlanowanie = new Color(0.25f, 0.9f, 0.45f);
    [SerializeField] private Color  colorAtak       = new Color(1f,    0.4f, 0.15f);

    [Header("Sklep - sloty pojazdow")]
    public VehicleUISlot[] vehicleSlots = new VehicleUISlot[5];

    [Header("Panel Kolejki")]
    public Transform  attackQueueContainer;
    public GameObject queueButtonPrefab;

    [Header("Kontrolki")]
    public Button          startButton;
    public TextMeshProUGUI startButtonText;
    [SerializeField] private string labelStartPusty    = "START";
    [SerializeField] private string labelStartZKolejka = "START ({0})";

    [Header("Informacja - Brak Zlota")]
    public TextMeshProUGUI noMoneyText;
    public GameObject      noMoneyPanel;
    [SerializeField] private string noMoneyMessage  = "Brak zlota";
    [SerializeField] private float  noMoneyDuration = 1.5f;

    [Header("Panele Faz")]
    public GameObject queuePanel;
    public GameObject abilitiesPanel;

    // ── Stan wewnętrzny ──────────────────────────────────────────────────

    private int  _currentGold;
    private int  _currentRound     = 1;
    private bool _isPlanning       = true;
    private bool _spawningComplete = false;
    private int  _activeVehicles   = 0;

    private readonly List<QueueEntry> _queue          = new List<QueueEntry>();
    private readonly List<GameObject> _queueItems     = new List<GameObject>();
    private readonly List<Button>     _abilityButtons = new List<Button>();
    private Coroutine _noMoneyCoroutine;

    public bool IsPlanning => _isPlanning;

    // ── Unity lifecycle ──────────────────────────────────────────────────

    void Awake()
    {
        Instance = this;
        Time.timeScale = 0f;
    }

    void Start()
    {
        if (config == null) { Debug.LogError("UIManager: brak GameConfig!"); return; }

        _currentGold = config.startingGold;

        BuildNoMoneyTextIfMissing();

        if (abilitiesPanel != null)
        {
            abilitiesPanel.SetActive(false);
            SetupAbilitiesPanel();
        }

        RefreshHUD();
        SetupShopButtons();

        if (startButton != null)
            startButton.onClick.AddListener(OnStartClicked);

        SyncStartButton();

        TowerSpawner.Instance?.GenerateTowers(_currentRound);
    }

    void BuildNoMoneyTextIfMissing()
    {
        if (noMoneyText != null) return;

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        var go = new GameObject("NoMoneyText");
        go.transform.SetParent(canvas.transform, false);

        var rt               = go.AddComponent<RectTransform>();
        rt.anchorMin         = new Vector2(0.5f, 0.5f);
        rt.anchorMax         = new Vector2(0.5f, 0.5f);
        rt.pivot             = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition  = Vector2.zero;
        rt.sizeDelta         = new Vector2(420f, 90f);

        noMoneyText           = go.AddComponent<TextMeshProUGUI>();
        noMoneyText.text      = noMoneyMessage;
        noMoneyText.color     = Color.red;
        noMoneyText.fontSize  = 40f;
        noMoneyText.fontStyle = FontStyles.Bold;
        noMoneyText.alignment = TextAlignmentOptions.Center;

        go.SetActive(false);
    }

    // ══ FAZY ══════════════════════════════════════════════════════════════

    void EnterExecution()
    {
        _isPlanning       = false;
        _spawningComplete = false;
        _activeVehicles   = 0;

        Time.timeScale = 1f;

        foreach (var s in vehicleSlots)
            if (s?.button != null) s.button.interactable = false;
        if (startButton != null) startButton.interactable = false;

        if (queuePanel     != null) queuePanel.SetActive(false);
        if (abilitiesPanel != null) abilitiesPanel.SetActive(true);

        ClearQueueUI();

        VehicleSpawner.Instance.StartSpawning(new List<QueueEntry>(_queue));
        _queue.Clear();

        RefreshHUD();
    }

    void EnterPlanning()
    {
        _isPlanning       = true;
        _spawningComplete = false;
        _activeVehicles   = 0;
        _currentRound++;

        TowerSpawner.Instance?.GenerateTowers(_currentRound);

        if (GameStatistics.Instance != null)
            GameStatistics.Instance.wavesSurvived++;

        if (abilitiesPanel != null) abilitiesPanel.SetActive(false);
        if (queuePanel     != null) queuePanel.SetActive(true);

        Time.timeScale = 0f;

        foreach (var s in vehicleSlots)
            if (s?.button != null) s.button.interactable = true;

        RefreshHUD();
        SyncStartButton();
    }

    // ══ ZAKUPY ════════════════════════════════════════════════════════════

    void OnBuyVehicle(int index)
    {
        if (!_isPlanning) return;

        int cost = config.vehicles[index].cost;
        if (!TrySpendGold(cost)) { FlashNoMoney(); return; }

        if (GameStatistics.Instance != null)
            GameStatistics.Instance.totalGoldSpent += cost;

        var entry = new QueueEntry { vehicleIndex = index, cost = cost };
        _queue.Add(entry);
        BuildQueueItem(entry);
        SyncStartButton();
    }

    // ══ KOLEJKA UI ════════════════════════════════════════════════════════

    void BuildQueueItem(QueueEntry entry)
    {
        if (attackQueueContainer == null || queueButtonPrefab == null) return;

        var btn = Instantiate(queueButtonPrefab, attackQueueContainer);

        var txt = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (txt != null)
        {
            txt.transform.SetAsLastSibling();
            txt.gameObject.layer = LayerMask.NameToLayer("UI");
            txt.color           = Color.white;
            txt.raycastTarget   = false;
            txt.text            = GetInitial(config.vehicles[entry.vehicleIndex].vehicleName);
        }

        var button = btn.GetComponent<Button>();
        if (button != null)
            button.onClick.AddListener(() => OnRefundQueueItem(entry, btn));

        _queueItems.Add(btn);
    }

    void OnRefundQueueItem(QueueEntry entry, GameObject btn)
    {
        if (!_isPlanning) return;

        int idx = _queue.IndexOf(entry);
        if (idx < 0) return;

        _queue.RemoveAt(idx);
        AddGold(entry.cost);

        if (GameStatistics.Instance != null)
            GameStatistics.Instance.totalGoldSpent -= entry.cost;

        _queueItems.Remove(btn);
        Destroy(btn);
        SyncStartButton();
    }

    void ClearQueueUI()
    {
        foreach (var go in _queueItems)
            if (go != null) Destroy(go);
        _queueItems.Clear();
    }

    // Inicjał z ostatniego członu nazwy: "Wóz Tank" → "T", "Kamikaze" → "K"
    static string GetInitial(string name)
    {
        if (string.IsNullOrEmpty(name)) return "?";
        string[] parts = name.Trim().Split(' ');
        string last = parts[parts.Length - 1];
        return last.Length > 0 ? last[0].ToString().ToUpper() : "?";
    }

    // ══ PANEL MOCY ════════════════════════════════════════════════════════

    void SetupAbilitiesPanel()
    {
        if (abilitiesPanel == null) return;
        if (abilitiesPanel.transform.childCount > 0)
        {
            foreach (Transform child in abilitiesPanel.transform)
            {
                var btn = child.GetComponent<Button>();
                if (btn != null) _abilityButtons.Add(btn);
            }
            for (int i = 0; i < _abilityButtons.Count; i++)
            {
                int captured = i;
                _abilityButtons[i].onClick.AddListener(() => OnAbilityClicked(captured));
            }
            return;
        }

        var hlg = abilitiesPanel.GetComponent<HorizontalLayoutGroup>()
               ?? abilitiesPanel.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing                = 8f;
        hlg.childForceExpandWidth  = true;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth      = true;
        hlg.childControlHeight     = true;
        hlg.padding                = new RectOffset(8, 8, 6, 6);

        var ta = TacticalAbilities.Instance;
        CreateAbilityButton("Nalot",  ta != null ? (int)ta.airstrikeCost : 100, 0);
        CreateAbilityButton("Tarcza", ta != null ? (int)ta.shieldCost    : 75,  1);
        CreateAbilityButton("Boost",  ta != null ? (int)ta.boostCost     : 50,  2);
    }

    void CreateAbilityButton(string abilityName, int cost, int index)
    {
        var btnGO = new GameObject($"Ability_{abilityName}");
        btnGO.transform.SetParent(abilitiesPanel.transform, false);

        var img = btnGO.AddComponent<Image>();
        img.color = new Color(0.12f, 0.18f, 0.32f, 1f);
        var btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = img;
        var bc = btn.colors;
        bc.normalColor      = new Color(0.12f, 0.18f, 0.32f, 1f);
        bc.highlightedColor = new Color(0.22f, 0.35f, 0.55f);
        bc.pressedColor     = new Color(0.06f, 0.10f, 0.18f);
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
        iconImg.color          = new Color(0.25f, 0.38f, 0.60f, 1f);
        iconImg.preserveAspect = true;
        var iconLE             = iconGO.AddComponent<LayoutElement>();
        iconLE.minWidth        = 48f;
        iconLE.preferredWidth  = 48f;
        iconLE.flexibleWidth   = 0f;

        // Blok tekstowy po prawej
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
        nameTxt.color            = Color.white;
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
        costTxt.color          = new Color(1f, 0.85f, 0.2f);
        costTxt.fontStyle      = FontStyles.Bold;
        costTxt.alignment      = TextAlignmentOptions.Center;
        var costLE             = costGO.AddComponent<LayoutElement>();
        costLE.preferredHeight = 18f;
        costLE.flexibleHeight  = 0f;

        int captured = index;
        btn.onClick.AddListener(() => OnAbilityClicked(captured));
        _abilityButtons.Add(btn);
    }

    void OnAbilityClicked(int index)
    {
        if (_isPlanning) return;
        switch (index)
        {
            case 0: TacticalAbilities.Instance?.ActivateAirstrike(); break;
            case 1: TacticalAbilities.Instance?.ActivateShield();    break;
            case 2: TacticalAbilities.Instance?.ActivateBoost();     break;
        }
    }

    // ══ SPAWNER – callbacki ═══════════════════════════════════════════════

    public void OnVehicleSpawned() => _activeVehicles++;

    public void OnVehicleRemoved()
    {
        if (_isPlanning) return;
        _activeVehicles = Mathf.Max(0, _activeVehicles - 1);
        CheckRoundEnd();
    }

    public void OnSpawningComplete()
    {
        _spawningComplete = true;
        CheckRoundEnd();
    }

    void CheckRoundEnd()
    {
        if (_spawningComplete && _activeVehicles == 0)
            EnterPlanning();
    }

    // ══ ZLOTO ═════════════════════════════════════════════════════════════

    public bool TrySpendGold(int amount)
    {
        if (_currentGold < amount) return false;
        _currentGold -= amount;
        UpdateGoldDisplay();
        return true;
    }

    public void AddGold(int amount)
    {
        _currentGold += amount;
        UpdateGoldDisplay();
    }

    public void AddGoldForEscapedVehicle()
    {
        if (config != null) _currentGold += config.goldPerEscapedVehicle;
        UpdateGoldDisplay();
    }

    // ══ BRAK ZLOTA ════════════════════════════════════════════════════════

    public void FlashNoMoney()
    {
        if (_noMoneyCoroutine != null) StopCoroutine(_noMoneyCoroutine);
        _noMoneyCoroutine = StartCoroutine(NoMoneyCoroutine());
    }

    IEnumerator NoMoneyCoroutine()
    {
        if (noMoneyText == null) yield break;
        noMoneyText.text = noMoneyMessage;
        GameObject toggleTarget = noMoneyPanel != null ? noMoneyPanel : noMoneyText.gameObject;
        toggleTarget.SetActive(true);
        yield return new WaitForSecondsRealtime(noMoneyDuration);
        toggleTarget.SetActive(false);
    }

    // ══ START ═════════════════════════════════════════════════════════════

    void OnStartClicked()
    {
        if (_isPlanning && _queue.Count > 0)
            EnterExecution();
    }

    void SyncStartButton()
    {
        if (startButton == null) return;
        bool canStart = _isPlanning && _queue.Count > 0;
        startButton.interactable = canStart;

        if (startButtonText != null)
            startButtonText.text = _queue.Count > 0
                ? string.Format(labelStartZKolejka, _queue.Count)
                : labelStartPusty;
    }

    // ══ HUD ═══════════════════════════════════════════════════════════════

    void SetupShopButtons()
    {
        for (int i = 0; i < vehicleSlots.Length; i++)
        {
            var slot = vehicleSlots[i];
            if (slot == null || i >= config.vehicles.Length) continue;

            slot.nameText.text = config.vehicles[i].vehicleName;
            slot.costText.text = $"{config.vehicles[i].cost} Z";

            if (config.vehicles[i].icon != null && slot.iconImage != null)
                slot.iconImage.sprite = config.vehicles[i].icon;

            int captured = i;
            slot.button.onClick.AddListener(() => OnBuyVehicle(captured));
        }
    }

    void RefreshHUD()
    {
        UpdateGoldDisplay();
        UpdateRoundDisplay();
        UpdatePhaseDisplay();
    }

    void UpdateGoldDisplay()
    {
        if (goldText != null) goldText.text = prefixZloto + _currentGold;
    }

    void UpdateRoundDisplay()
    {
        if (roundText != null) roundText.text = prefixRunda + _currentRound;
    }

    void UpdatePhaseDisplay()
    {
        if (phaseText == null) return;
        if (_isPlanning)
        {
            phaseText.text  = labelPlanowanie;
            phaseText.color = colorPlanowanie;
        }
        else
        {
            phaseText.text  = labelAtak;
            phaseText.color = colorAtak;
        }
    }
}
