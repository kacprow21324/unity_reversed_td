using UnityEngine;
using TMPro;

// Bazowa klasa dla wszystkich wież – zarządza HP, wyświetlaczem i nagrodą złota.
public abstract class WiezaBaza : MonoBehaviour
{
    [Header("Zdrowie Wieży")]
    public float maxHP = 100f;
    public int nagrodaZlota = 50;

    protected float currentHP;
    private TextMeshPro _hpText;
    private Transform _kamera;

    protected virtual void Start()
    {
        _kamera = Camera.main?.transform;
        CreateHPDisplay();
        currentHP = maxHP;
        UpdateHPDisplay();
    }

    protected virtual void OnEnable()
    {
        currentHP = maxHP;
        UpdateHPDisplay();
    }

    protected virtual void Update()
    {
        if (_hpText != null && _kamera != null)
            _hpText.transform.rotation = _kamera.rotation;
    }

    public void TakeDamage(float amount)
    {
        currentHP = Mathf.Max(currentHP - amount, 0f);

        if (_hpText != null)
            _hpText.color = Color.Lerp(Color.red, Color.green, currentHP / maxHP);

        UpdateHPDisplay();

        if (currentHP <= 0f)
            Zniszcz();
    }

    void Zniszcz()
    {
        OnZniszcz();
        if (GameplayUIManager.Instance != null)
            GameplayUIManager.Instance.AddGold(nagrodaZlota);
        gameObject.SetActive(false);
    }

    protected virtual void OnZniszcz() { }

    void CreateHPDisplay()
    {
        var hpObj = new GameObject("HP_Display");
        hpObj.transform.SetParent(transform);
        hpObj.transform.localPosition = new Vector3(0f, 2.5f, 0f);
        hpObj.transform.localRotation = Quaternion.identity;

        _hpText = hpObj.AddComponent<TextMeshPro>();
        _hpText.fontSize = 4f;
        _hpText.alignment = TextAlignmentOptions.Center;
        _hpText.fontStyle = FontStyles.Bold;
        _hpText.color = Color.green;
    }

    void UpdateHPDisplay()
    {
        if (_hpText == null) return;
        _hpText.text = $"{currentHP:0}/{maxHP:0} HP";
    }
}
