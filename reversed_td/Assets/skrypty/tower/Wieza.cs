using UnityEngine;
using TMPro;

public class WiezaAtak : MonoBehaviour
{
    [Header("Ustawienia Wieży")]
    public float zasieg = 10f;
    public float szybkoscAtaku = 1f;
    public LayerMask warstwaWroga;

    [Header("Ustawienia Pocisku")]
    public GameObject prefabPocisku;
    public Transform punktStrzalu;

    [Header("Zdrowie")]
    public float maxHP = 100f;
    public int nagrodaZlota = 50;

    private float currentHP;
    private float licznikAtaku = 0f;
    private TextMeshPro _hpText;
    private Transform _kamera;

    void Start()
    {
        _kamera = Camera.main?.transform;
        CreateHPDisplay();
        currentHP = maxHP;
        UpdateHPDisplay();
    }

    void OnEnable()
    {
        currentHP = maxHP;
        UpdateHPDisplay();
    }

    void Update()
    {
        licznikAtaku -= Time.deltaTime;
        if (licznikAtaku <= 0f)
            SzukajIAtakuj();

        if (_hpText != null && _kamera != null)
            _hpText.transform.rotation = _kamera.rotation;
    }

    public void TakeDamage(float amount)
    {
        currentHP = Mathf.Max(currentHP - amount, 0f);

        if (_hpText != null)
        {
            float ratio = currentHP / maxHP;
            _hpText.color = Color.Lerp(Color.red, Color.green, ratio);
        }

        UpdateHPDisplay();

        if (currentHP <= 0f)
            Zniszcz();
    }

    void SzukajIAtakuj()
    {
        Collider[] wrogowieWZasiegu = Physics.OverlapSphere(transform.position, zasieg, warstwaWroga);

        if (wrogowieWZasiegu.Length > 0)
        {
            Transform cel = wrogowieWZasiegu[0].transform;
            if (prefabPocisku != null && punktStrzalu != null)
                Strzelaj(cel);

            licznikAtaku = 1f / szybkoscAtaku;
        }
    }

    void Strzelaj(Transform cel)
    {
        Debug.Log("Wieża strzela do: " + cel.name);
        GameObject nowyPociskGO = Instantiate(prefabPocisku, punktStrzalu.position, punktStrzalu.rotation);
        Pocisk skryptPocisku = nowyPociskGO.GetComponent<Pocisk>();
        if (skryptPocisku != null)
            skryptPocisku.UstawCel(cel);
    }

    void Zniszcz()
    {
        if (GameplayUIManager.Instance != null)
            GameplayUIManager.Instance.AddGold(nagrodaZlota);

        gameObject.SetActive(false);
    }

    void CreateHPDisplay()
    {
        GameObject hpObj = new GameObject("HP_Display");
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
        _hpText.text = $"{currentHP}/{maxHP} HP";
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, zasieg);
    }
}
