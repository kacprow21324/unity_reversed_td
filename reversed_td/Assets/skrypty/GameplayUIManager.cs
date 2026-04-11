using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GameplayUIManager : MonoBehaviour
{
    public static GameplayUIManager Instance;

    [Header("Plik z danymi")]
    public GameConfig config;

    [Header("Teksty Statystyk")]
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI roundText;

    private int _currentGold;

    [System.Serializable]
    public class VehicleUISlot
    {
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI costText;
        public Image iconImage;
        public Button button;
    }

    public VehicleUISlot[] vehicleSlots = new VehicleUISlot[5];

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (config == null) return;

        _currentGold = config.startingGold;
        UpdateGoldDisplay();

        for (int i = 0; i < vehicleSlots.Length; i++)
        {
            if (i < config.vehicles.Length)
            {
                vehicleSlots[i].nameText.text = config.vehicles[i].vehicleName;
                vehicleSlots[i].costText.text = "Koszt: " + config.vehicles[i].cost;
                if (config.vehicles[i].icon != null)
                    vehicleSlots[i].iconImage.sprite = config.vehicles[i].icon;

                // --- NOWE: Podpiêcie klikniêcia przycisku ---
                int index = i;
                int cost = config.vehicles[i].cost;
                vehicleSlots[i].button.onClick.AddListener(() =>
                {
                    if (VehicleSpawner.Instance != null)
                    {
                        VehicleSpawner.Instance.TrySpawnVehicle(index, cost);
                    }
                    else
                    {
                        Debug.LogWarning("Brak Spawnera na mapie!");
                    }
                });
            }
        }
    }

    public void AddGoldForEscapedVehicle()
    {
        _currentGold += config.goldPerEscapedVehicle;
        UpdateGoldDisplay();
    }

    public bool TrySpendGold(int amount)
    {
        if (_currentGold >= amount)
        {
            _currentGold -= amount;
            UpdateGoldDisplay();
            return true; // Udany zakup
        }
        return false; // Brak œrodków
    }

    private void UpdateGoldDisplay()
    {
        goldText.text = "Z³oto: " + _currentGold;
    }
}