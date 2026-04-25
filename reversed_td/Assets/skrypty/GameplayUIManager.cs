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

    // ── Stan wewnętrzny ──────────────────────────────────────────────────

    private int  _currentGold;
    private int  _currentRound    = 1;
    private bool _isPlanning      = true;
    private bool _spawningComplete = false;
    private int  _activeVehicles  = 0;

    private readonly List<QueueEntry>  _queue      = new List<QueueEntry>();
    private readonly List<GameObject>  _queueItems = new List<GameObject>();
    private Coroutine _noMoneyCoroutine;

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

        RefreshHUD();
        SetupShopButtons();

        if (startButton != null)
            startButton.onClick.AddListener(OnStartClicked);

        SyncStartButton();
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

        if (GameStatistics.Instance != null)
            GameStatistics.Instance.wavesSurvived++;

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
            string vName = config.vehicles[entry.vehicleIndex].vehicleName;
            txt.text = vName.Length > 0 ? vName[0].ToString() : "?";
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

    void FlashNoMoney()
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
