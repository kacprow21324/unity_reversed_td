using UnityEngine;

/// Aktywuje efekt dymu gdy HP wieży lub pojazdu spadnie poniżej progu.
///
/// Jak użyć:
///   1. Dodaj ten komponent jako dziecko obiektu wieży lub pojazdu.
///   2. Przypisz swój ParticleSystem dymu do pola smokeEffect.
///   3. Ustaw progHP (domyślnie 0.35 = 35% HP).
///
/// Skrypt automatycznie wykrywa czy rodzic to WiezaBaza czy pojazd.
public class LowHPSmoke : MonoBehaviour
{
    [Header("Efekt Dymu")]
    public ParticleSystem smokeEffect;

    [Header("Próg HP")]
    [Range(0.01f, 0.99f)]
    [Tooltip("Dym pojawia się gdy HP < próg. 0.35 = poniżej 35% zdrowia.")]
    public float progHP = 0.35f;

    // ── Referencje do komponentów HP ──────────────────────────────────────

    WiezaBaza _wieza;
    pojazd    _pojazd;

    // ── Unity lifecycle ────────────────────────────────────────────────────

    void Start()
    {
        _wieza  = GetComponentInParent<WiezaBaza>();
        _pojazd = GetComponentInParent<pojazd>();

        if (_wieza == null && _pojazd == null)
            Debug.LogWarning("[LowHPSmoke] Brak WiezaBaza ani pojazd w hierarchii nadrzędnej!", this);

        if (smokeEffect != null)
            smokeEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    void Update()
    {
        if (smokeEffect == null) return;

        float ratio         = PobierzRatioHP();
        bool  powiniendymić = ratio > 0f && ratio < progHP;

        if (powiniendymić && !smokeEffect.isPlaying)
            smokeEffect.Play();
        else if (!powiniendymić && smokeEffect.isPlaying)
            smokeEffect.Stop();
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    float PobierzRatioHP()
    {
        if (_wieza  != null) return _wieza.HPRatio;
        if (_pojazd != null && _pojazd.maxHp > 0f)
            return _pojazd.PobierzAktualneHP() / _pojazd.maxHp;
        return 1f; // brak komponentu = nie dym
    }
}
