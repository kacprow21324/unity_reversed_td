using UnityEngine;

/// Obraca głowicę/turet wieży wyłącznie wokół osi Y (yaw).
/// Po namierzeniu pojazdu śledzi go każdą klatkę bez przerwy.
/// Cel jest zwalniany tylko gdy wyraźnie wyjdzie poza zasięg lub zginie.
public class TurretRotation : MonoBehaviour
{
    [Header("Śledzenie celu")]
    [Tooltip("Czas wygładzania (s). Mniejszy = szybszy, bardziej responsywny. ~0.05–0.15")]
    public float czasWygładzania = 0.10f;

    [Tooltip("Maks. prędkość obrotu (stopni/s).")]
    public float maxPredkosc = 360f;

    [Header("Wyrównanie modelu")]
    [Tooltip("Kręć w Play Mode aż niebieska linia Gizmo wskazuje na pojazd.")]
    [Range(-180f, 180f)]
    public float offsetKata = 0f;

    [Header("Zasięg")]
    public LayerMask warstwaWroga;
    public float zasieg = 15f;

    [Tooltip("Cel jest zwalniany dopiero gdy wyjdzie poza zasieg + margines. " +
             "Zapobiega migotaniu na granicy zasięgu.")]
    public float marginesUtraty = 2f;

    // ── Stan wewnętrzny ────────────────────────────────────────────────────

    Transform _cel;
    float     _predkoscKatowa;
    float     _szukajTimer;
    const float INTERWAL_SZUKANIA = 0.15f; // szukanie NOWEGO celu — rzadko, oszczędnie

    // ── Unity lifecycle ────────────────────────────────────────────────────

    void Update()
    {
        // 1. Jeśli mamy cel — waliduj go każdą klatkę (zero opóźnień)
        if (_cel != null)
        {
            if (CelNieważny(_cel))
                _cel = null;
        }

        // 2. Jeśli nie ma celu — szukaj co INTERWAL_SZUKANIA (nie każdą klatkę — optymalizacja)
        if (_cel == null)
        {
            _szukajTimer -= Time.deltaTime;
            if (_szukajTimer <= 0f)
            {
                _cel         = SzukajNowegoCelu();
                _szukajTimer = INTERWAL_SZUKANIA;
            }
        }

        // 3. Śledź lub stój
        if (_cel != null)
            SledzCel();
        else
            _predkoscKatowa = 0f;
    }

    // ── Walidacja ─────────────────────────────────────────────────────────

    bool CelNieważny(Transform cel)
    {
        if (cel == null || !cel.gameObject.activeInHierarchy) return true;

        // Cel zwalniany dopiero gdy wyraźnie przekroczy zasięg + margines (histereza)
        float dystans = Vector3.Distance(transform.position, cel.position);
        if (dystans > zasieg + marginesUtraty) return true;

        // Kamikaze i inne ukryte pojazdy — zwolnij gdy staną się niewidzialne
        var p = cel.GetComponent<pojazd>();
        if (p != null && !p.IsTargetable) return true;

        return false;
    }

    // ── Szukanie ──────────────────────────────────────────────────────────

    Transform SzukajNowegoCelu()
    {
        var znaleziony = pojazd.ZnajdzCelWZasiegu(transform.position, zasieg, warstwaWroga);
        return znaleziony?.transform;
    }

    // ── Śledzenie ─────────────────────────────────────────────────────────

    void SledzCel()
    {
        Vector3 kierunek = _cel.position - transform.position;
        kierunek.y = 0f;
        if (kierunek.sqrMagnitude < 0.001f) return;

        float katDocelowy = Quaternion.LookRotation(kierunek).eulerAngles.y - offsetKata;
        float katAktualny = transform.eulerAngles.y;

        float nowyKat = Mathf.SmoothDampAngle(
            katAktualny,
            katDocelowy,
            ref _predkoscKatowa,
            czasWygładzania,
            maxPredkosc > 0f ? maxPredkosc : Mathf.Infinity);

        transform.rotation = Quaternion.Euler(
            transform.eulerAngles.x,
            nowyKat,
            transform.eulerAngles.z);
    }

    // ── Gizmo ─────────────────────────────────────────────────────────────

    void OnDrawGizmos()
    {
        // Niebieska linia = gdzie turet faktycznie patrzy
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, transform.forward * 3f);

        // Zielona sfera = zasięg namierzania
        Gizmos.color = new Color(0f, 1f, 0f, 0.10f);
        Gizmos.DrawWireSphere(transform.position, zasieg);

        // Żółta sfera = strefa utraty celu (zasieg + margines)
        Gizmos.color = new Color(1f, 1f, 0f, 0.06f);
        Gizmos.DrawWireSphere(transform.position, zasieg + marginesUtraty);
    }
}
