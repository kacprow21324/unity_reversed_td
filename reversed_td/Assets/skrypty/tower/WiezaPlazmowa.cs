using UnityEngine;
using System.Collections;

// WPLAZMOWA – ciągła wiązka z rosnącym mnożnikiem obrażeń.
// Im dłużej razi ten sam pojazd, tym jest silniejsza.
public class WiezaPlazmowa : WiezaBaza
{
    [Header("Parametry Wiązki")]
    public float zasieg = 10f;
    public float obrazeniaBazowe = 4f;
    public float tickRate = 0.15f;
    public float mnoznikRastu = 0.3f;
    public float maxMnoznik = 4f;
    public LayerMask warstwaWroga;

    [Header("Punkt strzału (opcjonalny)")]
    public Transform punktStrzalu;

    private pojazd _aktualnycel;
    private Coroutine _wiazkaCoroutine;
    private Coroutine _skanCoroutine;
    private LineRenderer _lr;
    private float _aktualnyMnoznik = 1f;

    protected override void Start()
    {
        base.Start();
        UtworzKragZasiegu(zasieg, new Color(0.7f, 0f, 1f, 0.85f));
        SetupLineRenderer();
        _skanCoroutine = StartCoroutine(SkanujCel());
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        StopAllCoroutines();
        _wiazkaCoroutine = null;
        _aktualnycel = null;
        _aktualnyMnoznik = 1f;
        if (_lr != null) _lr.enabled = false;
        if (gameObject.activeInHierarchy)
            _skanCoroutine = StartCoroutine(SkanujCel());
    }

    void SetupLineRenderer()
    {
        _lr = gameObject.AddComponent<LineRenderer>();
        _lr.positionCount = 2;
        _lr.startWidth = 0.12f;
        _lr.endWidth = 0.04f;
        _lr.material = new Material(Shader.Find("Sprites/Default"));
        _lr.startColor = new Color(0.7f, 0f, 1f, 0.9f);
        _lr.endColor = new Color(1f, 0.2f, 0.8f, 0.9f);
        _lr.enabled = false;
    }

    IEnumerator SkanujCel()
    {
        while (true)
        {
            bool celNieAktywny = _aktualnycel == null || !_aktualnycel.gameObject.activeInHierarchy;
            bool pozaZasiegiem = _aktualnycel != null &&
                                 Vector3.Distance(transform.position, _aktualnycel.transform.position) > zasieg;
            bool celNieDocelowy = _aktualnycel != null && !_aktualnycel.IsTargetable;

            if (celNieAktywny || pozaZasiegiem || celNieDocelowy)
            {
                if (_wiazkaCoroutine != null)
                {
                    StopCoroutine(_wiazkaCoroutine);
                    _wiazkaCoroutine = null;
                    _lr.enabled = false;
                    _aktualnyMnoznik = 1f;
                }
                _aktualnycel = null;

                // Szukaj nowego celu z priorytetem dla tauntera (Czołg)
                _aktualnycel = pojazd.ZnajdzCelWZasiegu(transform.position, zasieg, warstwaWroga);

                if (_aktualnycel != null)
                    _wiazkaCoroutine = StartCoroutine(Wiazka());
            }

            yield return new WaitForSeconds(0.2f);
        }
    }

    IEnumerator Wiazka()
    {
        _aktualnyMnoznik = 1f;
        _lr.enabled = true;

        while (_aktualnycel != null && _aktualnycel.gameObject.activeInHierarchy && _aktualnycel.IsTargetable)
        {
            if (Vector3.Distance(transform.position, _aktualnycel.transform.position) > zasieg)
                break;

            float dmg = obrazeniaBazowe * _aktualnyMnoznik;
            if (!_aktualnycel.ReflektujLaser(dmg, this))
                _aktualnycel.OdejmijHp(dmg, przebijaPancerz: false);

            _aktualnyMnoznik = Mathf.Min(_aktualnyMnoznik + mnoznikRastu * tickRate, maxMnoznik);

            // Aktualizuj wizualizację wiązki
            Vector3 start = punktStrzalu != null ? punktStrzalu.position : transform.position + Vector3.up;
            _lr.SetPosition(0, start);
            _lr.SetPosition(1, _aktualnycel.transform.position);

            // Intensyfikuj kolor wiązki wraz ze wzrostem mnożnika
            float t = (_aktualnyMnoznik - 1f) / (maxMnoznik - 1f);
            _lr.startColor = Color.Lerp(new Color(0.7f, 0f, 1f), new Color(1f, 0.8f, 0f), t);
            _lr.endColor = Color.Lerp(new Color(1f, 0.2f, 0.8f), new Color(1f, 0.3f, 0f), t);

            yield return new WaitForSeconds(tickRate);
        }

        _lr.enabled = false;
        _aktualnyMnoznik = 1f;
        _aktualnycel = null;
        _wiazkaCoroutine = null;
    }

    protected override void OnZniszcz()
    {
        StopAllCoroutines();
        if (_lr != null) _lr.enabled = false;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.7f, 0f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, zasieg);
    }
}
