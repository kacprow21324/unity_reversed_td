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

    [Header("Sklep – sloty pojazdów")]
    public VehicleUISlot[] vehicleSlots = new VehicleUISlot[5];

    [Header("Panel Kolejki")]
    public Transform queueContent;         // Content ScrollRect-a

    [Header("Kontrolki")]
    public Button startButton;
    public TextMeshProUGUI startButtonText;
    public TextMeshProUGUI noMoneyText;    // migający napis „Brak złota!"

    // ── Stan wewnętrzny ──────────────────────────────────────────────────

    private int _currentGold;
    private int _currentRound = 1;
    private bool _isPlanning = true;
    private bool _spawningComplete = false;
    private int _activeVehicles = 0;

    private readonly List<QueueEntry>   _queue       = new List<QueueEntry>();
    private readonly List<GameObject>   _queueItems  = new List<GameObject>();
    private Coroutine _noMoneyCoroutine;

    // ── Unity lifecycle ──────────────────────────────────────────────────

    void Awake()
    {
        Instance = this;
        Time.timeScale = 0f;   // Pauza na starcie → Faza Planowania
    }

    void Start()
    {
        if (config == null) { Debug.LogError("UIManager: brak GameConfig!"); return; }

        _currentGold = config.startingGold;
        RefreshHUD();
        SetupShopButtons();

        if (startButton != null)
            startButton.onClick.AddListener(OnStartClicked);

        if (noMoneyText != null)
            noMoneyText.gameObject.SetActive(false);

        SyncStartButton();
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

        var entry = new QueueEntry { vehicleIndex = index, cost = cost };
        _queue.Add(entry);
        BuildQueueItem(entry);
        SyncStartButton();
    }

    // ══ KOLEJKA UI ════════════════════════════════════════════════════════

    void BuildQueueItem(QueueEntry entry)
    {
        if (queueContent == null) return;

        var item = new GameObject("QI");
        item.transform.SetParent(queueContent, false);

        // Wymagany przez VerticalLayoutGroup
        var le = item.AddComponent<LayoutElement>();
        le.preferredHeight  = 48f;
        le.flexibleWidth    = 1f;
        le.minHeight        = 40f;

        var bg = item.AddComponent<Image>();
        bg.color = new Color(0.15f, 0.2f, 0.28f, 1f);

        // Nazwa pojazdu
        var nameGO = new GameObject("N");
        nameGO.transform.SetParent(item.transform, false);
        var nameRt = nameGO.AddComponent<RectTransform>();
        nameRt.anchorMin = new Vector2(0f, 0f);
        nameRt.anchorMax = new Vector2(0.77f, 1f);
        nameRt.offsetMin = new Vector2(8f, 3f);
        nameRt.offsetMax = new Vector2(-2f, -3f);
        var nameTxt = nameGO.AddComponent<TextMeshProUGUI>();
        nameTxt.text             = config.vehicles[entry.vehicleIndex].vehicleName;
        nameTxt.fontSize         = 14f;
        nameTxt.enableAutoSizing = true;
        nameTxt.fontSizeMin      = 8f;
        nameTxt.fontSizeMax      = 14f;
        nameTxt.color            = Color.white;
        nameTxt.alignment        = TextAlignmentOptions.MidlineLeft;
        nameTxt.overflowMode     = TextOverflowModes.Ellipsis;

        // Przycisk cofnięcia (X)
        var undoGO = new GameObject("U");
        undoGO.transform.SetParent(item.transform, false);
        var undoRt = undoGO.AddComponent<RectTransform>();
        undoRt.anchorMin = new Vector2(0.77f, 0.1f);
        undoRt.anchorMax = new Vector2(1f,    0.9f);
        undoRt.offsetMin = new Vector2(3f,  0f);
        undoRt.offsetMax = new Vector2(-4f, 0f);
        var undoBg  = undoGO.AddComponent<Image>();
        undoBg.color = new Color(0.65f, 0.1f, 0.1f, 1f);
        var undoBtn = undoGO.AddComponent<Button>();
        undoBtn.targetGraphic = undoBg;
        var undoColors = undoBtn.colors;
        undoColors.highlightedColor = new Color(0.85f, 0.2f, 0.2f);
        undoColors.pressedColor     = new Color(0.45f, 0.05f, 0.05f);
        undoBtn.colors = undoColors;

        var undoTxtGO = new GameObject("T");
        undoTxtGO.transform.SetParent(undoGO.transform, false);
        var undoTxtRt = undoTxtGO.AddComponent<RectTransform>();
        undoTxtRt.anchorMin = Vector2.zero;
        undoTxtRt.anchorMax = Vector2.one;
        undoTxtRt.offsetMin = Vector2.zero;
        undoTxtRt.offsetMax = Vector2.zero;
        var undoTxt = undoTxtGO.AddComponent<TextMeshProUGUI>();
        undoTxt.text      = "✕";
        undoTxt.fontSize  = 18f;
        undoTxt.color     = Color.white;
        undoTxt.alignment = TextAlignmentOptions.Center;
        undoTxt.fontStyle = FontStyles.Bold;

        undoBtn.onClick.AddListener(() => UndoQueueEntry(entry, item));

        _queueItems.Add(item);
    }

    void UndoQueueEntry(QueueEntry entry, GameObject item)
    {
        if (!_isPlanning) return;

        int idx = _queue.IndexOf(entry);
        if (idx < 0) return;

        AddGold(entry.cost);    // Zwrot złota
        _queue.RemoveAt(idx);
        _queueItems.Remove(item);
        Destroy(item);
        SyncStartButton();
    }

    void ClearQueueUI()
    {
        foreach (var go in _queueItems)
            if (go != null) Destroy(go);
        _queueItems.Clear();
    }

    // ══ SPAWNER – callbacki ═══════════════════════════════════════════════

    /// Wywoływane przez VehicleSpawner za każdym razem gdy instancjonuje pojazd.
    public void OnVehicleSpawned()
    {
        _activeVehicles++;
    }

    /// Wywoływane przez pojazd.Smierc() i FinishLine gdy pojazd opuszcza grę.
    public void OnVehicleRemoved()
    {
        if (_isPlanning) return;
        _activeVehicles = Mathf.Max(0, _activeVehicles - 1);
        CheckRoundEnd();
    }

    /// Wywoływane przez VehicleSpawner po ostatnim spawnie.
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

    // ══ ZŁOTO ═════════════════════════════════════════════════════════════

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

    // ══ BRAK ZŁOTA – miganie ══════════════════════════════════════════════

    void FlashNoMoney()
    {
        if (_noMoneyCoroutine != null) StopCoroutine(_noMoneyCoroutine);
        _noMoneyCoroutine = StartCoroutine(FlashCoroutine());
    }

    IEnumerator FlashCoroutine()
    {
        if (noMoneyText == null) yield break;

        bool visible = true;
        float elapsed = 0f;
        const float interval = 0.18f;
        const float total    = 1.8f;

        while (elapsed < total)
        {
            noMoneyText.gameObject.SetActive(visible);
            visible = !visible;
            yield return new WaitForSecondsRealtime(interval); // działa przy timeScale=0
            elapsed += interval;
        }
        noMoneyText.gameObject.SetActive(false);
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
            startButtonText.text = _queue.Count > 0 ? $"START  ({_queue.Count})" : "START";
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
        if (goldText != null) goldText.text = $"Złoto: {_currentGold}";
    }

    void UpdateRoundDisplay()
    {
        if (roundText != null) roundText.text = $"Runda: {_currentRound}";
    }

    void UpdatePhaseDisplay()
    {
        if (phaseText == null) return;
        if (_isPlanning)
        {
            phaseText.text  = "◉ PLANOWANIE";
            phaseText.color = new Color(0.25f, 0.9f, 0.45f);
        }
        else
        {
            phaseText.text  = "▶ ATAK";
            phaseText.color = new Color(1f, 0.4f, 0.15f);
        }
    }
}
