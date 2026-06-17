using System.Collections;
using UnityEngine;
using TMPro;

// Bazowa klasa dla wszystkich wież – zarządza HP, wyświetlaczem i nagrodą złota.
public abstract class WiezaBaza : MonoBehaviour
{
    [Header("Zdrowie Wieży")]
    public float maxHP = 100f;
    public int nagrodaZlota = 50;

    [HideInInspector] public bool  isInvulnerable       = false;
    [HideInInspector] public float attackSpeedMultiplier = 1f;

    /// Stosunek aktualnego HP do maksymalnego (0–1). Używany przez LowHPSmoke.
    public float HPRatio => maxHP > 0f ? currentHP / maxHP : 1f;

    protected float currentHP;
    private TextMeshPro _hpText;
    private Transform _kamera;
    private GameObject  _kragZasiegu;
    private TowerOutline _towerOutline;

    protected virtual void Start()
    {
        _kamera = Camera.main?.transform;
        CreateHPDisplay();
        currentHP = maxHP;
        UpdateHPDisplay();
    }

    protected virtual void OnEnable()
    {
        currentHP             = maxHP;
        isInvulnerable        = false;
        attackSpeedMultiplier = 1f;
        UpdateHPDisplay();
    }

    protected virtual void Update()
    {
        if (_hpText != null && _kamera != null)
            _hpText.transform.rotation = _kamera.rotation;
    }

    public void TakeDamage(float amount)
    {
        if (isInvulnerable) return;
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

    public void AktywujTarcze(float czas)
    {
        StartCoroutine(TarczaWiezyKoroutyna(czas));
    }

    IEnumerator TarczaWiezyKoroutyna(float czas)
    {
        isInvulnerable = true;
        yield return new WaitForSeconds(czas);
        isInvulnerable = false;
    }

    public void BoostAttackSpeed(float multiplier, float duration)
    {
        StartCoroutine(BoostAttackSpeedKoroutyna(multiplier, duration));
    }

    IEnumerator BoostAttackSpeedKoroutyna(float multiplier, float duration)
    {
        attackSpeedMultiplier *= multiplier;
        yield return new WaitForSeconds(duration);
        if (this != null) attackSpeedMultiplier /= multiplier;
    }

    public void PokazZasieg()  => _kragZasiegu?.SetActive(true);
    public void UkryjZasieg() => _kragZasiegu?.SetActive(false);

    public void PokazPodswietlenie()
    {
        if (_towerOutline == null)
            _towerOutline = gameObject.AddComponent<TowerOutline>();
        _towerOutline.Pokaz();
    }

    public void UkryjPodswietlenie()
    {
        _towerOutline?.Ukryj();
    }

    protected void UtworzKragZasiegu(float promien, Color kolor)
    {
        var go = new GameObject("KragZasiegu");
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        _kragZasiegu = go;

        const int punkty = 64;
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop = true;
        lr.widthMultiplier = 0.12f;
        lr.positionCount = punkty;

        // Szukamy shadera kompatybilnego z aktywnym pipeline (Built-in / URP / HDRP)
        Shader sh = Shader.Find("Sprites/Default");
        if (sh == null) sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (sh == null) sh = Shader.Find("Unlit/Color");
        if (sh == null) sh = Shader.Find("Hidden/Internal-Colored");

        if (sh != null)
        {
            var mat = new Material(sh);
            mat.renderQueue = 4000;
            lr.material = mat;
        }

        lr.startColor = kolor;
        lr.endColor   = kolor;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows    = false;
        lr.sortingOrder      = 10;

        for (int i = 0; i < punkty; i++)
        {
            float kat = i / (float)punkty * Mathf.PI * 2f;
            lr.SetPosition(i, new Vector3(Mathf.Cos(kat) * promien, 0.08f, Mathf.Sin(kat) * promien));
        }

        go.SetActive(false);
    }

    void CreateHPDisplay()
    {
        var hpObj = new GameObject("HP_Display");
        hpObj.transform.SetParent(transform);
        hpObj.transform.localRotation = Quaternion.identity;

        // Pozycja w world space — zawsze 0.6 m nad szczytem modelu, niezależnie od skali prefabu
        hpObj.transform.position = new Vector3(
            transform.position.x,
            GetModelTopY() + 0.6f,
            transform.position.z);

        _hpText = hpObj.AddComponent<TextMeshPro>();
        _hpText.fontSize = 7f;
        _hpText.alignment = TextAlignmentOptions.Center;
        _hpText.fontStyle = FontStyles.Bold;
        _hpText.color = Color.green;
        _hpText.fontMaterial.SetFloat("_ZTestMode", 8f);
        _hpText.sortingOrder = 10;
    }

    float GetModelTopY()
    {
        float maxY = transform.position.y + 1f; // fallback gdy brak rendererów
        foreach (var r in GetComponentsInChildren<Renderer>())
        {
            if (r is LineRenderer) continue;
            if (r.bounds.size == Vector3.zero) continue;
            if (r.bounds.max.y > maxY) maxY = r.bounds.max.y;
        }
        return maxY;
    }

    void UpdateHPDisplay()
    {
        if (_hpText == null) return;
        _hpText.text = $"{currentHP:0}/{maxHP:0} HP";
    }
}
