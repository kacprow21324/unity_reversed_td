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
        string nazwaWiezy = gameObject.name.Replace("(Clone)", "").Trim();
        GameStatistics.Instance?.RegisterDestroyedTower(nazwaWiezy);
        if (GameplayUIManager.Instance != null)
            GameplayUIManager.Instance.AddGold(nagrodaZlota);
        gameObject.SetActive(false);
    }

    protected virtual void OnZniszcz() { }

    protected void UtworzKragZasiegu(float promien, Color kolor)
    {
        var go = new GameObject("KragZasiegu");
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;

        const int punkty = 64;
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop = true;
        lr.widthMultiplier = 0.12f;
        lr.positionCount = punkty;
        var mat = new Material(Shader.Find("Sprites/Default"));
        mat.renderQueue = 4000;
        mat.SetInt("unity_GUIZTestMode", (int)UnityEngine.Rendering.CompareFunction.Always);
        lr.material = mat;
        lr.startColor = kolor;
        lr.endColor = kolor;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        lr.sortingOrder = 10;

        for (int i = 0; i < punkty; i++)
        {
            float kat = i / (float)punkty * Mathf.PI * 2f;
            lr.SetPosition(i, new Vector3(Mathf.Cos(kat) * promien, 0.08f, Mathf.Sin(kat) * promien));
        }
    }

    void CreateHPDisplay()
    {
        var hpObj = new GameObject("HP_Display");
        hpObj.transform.SetParent(transform);
        hpObj.transform.localPosition = new Vector3(0f, 4f, 0f);
        hpObj.transform.localRotation = Quaternion.identity;

        _hpText = hpObj.AddComponent<TextMeshPro>();
        _hpText.fontSize = 7f;
        _hpText.alignment = TextAlignmentOptions.Center;
        _hpText.fontStyle = FontStyles.Bold;
        _hpText.color = Color.green;
        _hpText.fontMaterial.SetFloat("_ZTestMode", 8f); // 8 = CompareFunction.Always
        _hpText.sortingOrder = 10;
    }

    void UpdateHPDisplay()
    {
        if (_hpText == null) return;
        _hpText.text = $"{currentHP:0}/{maxHP:0} HP";
    }
}
