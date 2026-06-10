using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class DecreeData
{
    public readonly int Id;
    public readonly string Title;
    public readonly Func<string> GetCurrentValue;
    public readonly Func<string> GetNewValue;
    public readonly Action Apply;

    public DecreeData(int id, string title,
        Func<string> getCurrent, Func<string> getNew, Action apply)
    {
        Id              = id;
        Title           = title;
        GetCurrentValue = getCurrent;
        GetNewValue     = getNew;
        Apply           = apply;
    }
}

public class DecreeManager : MonoBehaviour
{
    public static DecreeManager Instance { get; private set; }

    // ── Wartości bazowe – edytowalne w Inspektorze ────────────────────────
    // Używane TYLKO do podglądu dekretów (UI). Rzeczywiste statystyki pojazdów
    // bierzesz z Inspektora prefabu każdego pojazdu.

    [Header("Baza – Wóz Podstawowy (tylko podgląd dekretów)")]
    public float BASE_POD_HP  = 120f;
    public float BASE_POD_SPD = 4.5f;
    public float BASE_POD_ARM = 10f;

    [Header("Baza – Wóz Tank (tylko podgląd dekretów)")]
    public float BASE_TNK_HP  = 200f;
    public float BASE_TNK_SPD = 1.5f;
    public float BASE_TNK_ARM = 30f;

    [Header("Baza – Wóz Artyleria (tylko podgląd dekretów)")]
    public float BASE_ALT_HP  = 60f;
    public float BASE_ALT_SPD = 3.5f;
    public float BASE_ALT_ARM = 0f;
    public float BASE_ALT_DMG = 40f;

    [Header("Baza – Wóz Lustro (tylko podgląd dekretów)")]
    public float BASE_LUS_HP  = 90f;
    public float BASE_LUS_SPD = 3f;
    public float BASE_LUS_ARM = 5f;

    [Header("Baza – Wóz Kamikaze (tylko podgląd dekretów)")]
    public float BASE_KAM_HP  = 80f;
    public float BASE_KAM_SPD = 8f;
    public float BASE_KAM_ARM = 0f;
    public float BASE_KAM_RAD = 6f;
    public float BASE_KAM_DMG = 150f;

    [Header("Baza – Zdolności (tylko podgląd dekretów)")]
    public float BASE_NALOT_RAD  = 8f;
    public float BASE_NALOT_DMG  = 100f;
    public float BASE_SHIELD_DUR = 4f;
    public float BASE_SHIELD_RAD = 10f;
    public float BASE_BOOST_FLAT = 2.0f;  // flat bonus Boosta (= TacticalAbilities.boostFlatBonus)
    public float BASE_BOOST_DUR  = 10f;

    // ── Aktywne buffy ─────────────────────────────────────────────────────
    // Prędkość: wartości PŁASKIE (dodawane do prędkości bazowej).
    // HP i Pancerz: wartości PROCENTOWE (mnożnik bonus, np. 0.20 = +20%).

    public float PodstawowyHP    { get; private set; }
    public float PodstawowySpeed { get; private set; }   // flat
    public float PodstawowyArmor { get; private set; }

    public float TankHP          { get; private set; }
    public float TankSpeed       { get; private set; }   // flat
    public float TankArmor       { get; private set; }

    public float ArtileriaHP     { get; private set; }
    public float ArtileriaSpeed  { get; private set; }   // flat
    public float ArtileriaArmor  { get; private set; }

    public float LustroHP        { get; private set; }   // nigdy nie inkrementowany (brak dekretu)
    public float LustroSpeed     { get; private set; }   // flat
    public float LustroArmor     { get; private set; }   // nigdy nie inkrementowany (brak dekretu)

    public float KamikazeHP      { get; private set; }   // nigdy nie inkrementowany (brak dekretu)
    public float KamikazeSpeed   { get; private set; }   // flat
    public float KamikazeArmor   { get; private set; }   // nigdy nie inkrementowany (brak dekretu)

    public float ArtyleriaObrazeniaBonus { get; private set; }
    public float LustroOdbicieBonus      { get; private set; }
    public float KamikazeRadiusBonus     { get; private set; }
    public float KamikazeDamageBonus     { get; private set; }

    public float NalotRadiusBonus    { get; private set; }
    public float NalotDamageBonus    { get; private set; }
    public float ShieldDurationBonus { get; private set; }
    public float ShieldRadiusBonus   { get; private set; }
    public float BoostFlatBonus      { get; private set; }  // flat – dodawane do boostFlatBonus w TacticalAbilities
    public float BoostDurationBonus  { get; private set; }

    // ── Pula dekretów ─────────────────────────────────────────────────────

    private List<DecreeData> _pool;
    private HashSet<int> _queueFilter = new HashSet<int>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        BuildPool();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void BuildPool()
    {
        _pool = new List<DecreeData>(20);

        // PODSTAWOWY (0-2)
        Add(0, "Woz Podstawowy: Max HP (+20%)",
            () => Mathf.RoundToInt(BASE_POD_HP * (1f + PodstawowyHP)).ToString(),
            () => Mathf.RoundToInt(BASE_POD_HP * (1f + PodstawowyHP + 0.20f)).ToString(),
            () => { PodstawowyHP += 0.20f; });

        Add(1, "Woz Podstawowy: Predkosc +0.5",
            () => (BASE_POD_SPD + PodstawowySpeed).ToString("F1"),
            () => (BASE_POD_SPD + PodstawowySpeed + 0.5f).ToString("F1"),
            () => { PodstawowySpeed += 0.5f; });

        Add(2, "Woz Podstawowy: Pancerz (+10%)",
            () => Mathf.RoundToInt(BASE_POD_ARM * (1f + PodstawowyArmor)).ToString(),
            () => Mathf.RoundToInt(BASE_POD_ARM * (1f + PodstawowyArmor + 0.10f)).ToString(),
            () => { PodstawowyArmor += 0.10f; });

        // TANK (3-5)
        Add(3, "Woz Tank: Max HP (+20%)",
            () => Mathf.RoundToInt(BASE_TNK_HP * (1f + TankHP)).ToString(),
            () => Mathf.RoundToInt(BASE_TNK_HP * (1f + TankHP + 0.20f)).ToString(),
            () => { TankHP += 0.20f; });

        Add(4, "Woz Tank: Predkosc +0.5",
            () => (BASE_TNK_SPD + TankSpeed).ToString("F1"),
            () => (BASE_TNK_SPD + TankSpeed + 0.5f).ToString("F1"),
            () => { TankSpeed += 0.5f; });

        Add(5, "Woz Tank: Pancerz (+10%)",
            () => Mathf.RoundToInt(BASE_TNK_ARM * (1f + TankArmor)).ToString(),
            () => Mathf.RoundToInt(BASE_TNK_ARM * (1f + TankArmor + 0.10f)).ToString(),
            () => { TankArmor += 0.10f; });

        // ARTYLERIA (6-8)
        Add(6, "Woz Dalekosiez.: Max HP (+20%)",
            () => Mathf.RoundToInt(BASE_ALT_HP * (1f + ArtileriaHP)).ToString(),
            () => Mathf.RoundToInt(BASE_ALT_HP * (1f + ArtileriaHP + 0.20f)).ToString(),
            () => { ArtileriaHP += 0.20f; });

        Add(7, "Woz Dalekosiez.: Predkosc +0.5",
            () => (BASE_ALT_SPD + ArtileriaSpeed).ToString("F1"),
            () => (BASE_ALT_SPD + ArtileriaSpeed + 0.5f).ToString("F1"),
            () => { ArtileriaSpeed += 0.5f; });

        // LUSTRO (10 – tylko prędkość; HP i Pancerz usunięte z puli)
        Add(10, "Woz Lustrzany: Predkosc +0.5",
            () => (BASE_LUS_SPD + LustroSpeed).ToString("F1"),
            () => (BASE_LUS_SPD + LustroSpeed + 0.5f).ToString("F1"),
            () => { LustroSpeed += 0.5f; });

        // KAMIKAZE (13 – tylko prędkość; HP i Pancerz usunięte z puli)
        Add(13, "Woz Zasadzka: Predkosc +0.5",
            () => (BASE_KAM_SPD + KamikazeSpeed).ToString("F1"),
            () => (BASE_KAM_SPD + KamikazeSpeed + 0.5f).ToString("F1"),
            () => { KamikazeSpeed += 0.5f; });

        // SPECJALNE (15-24)
        Add(15, "Artyleria: Obrazenia (+25%)",
            () => Mathf.RoundToInt(BASE_ALT_DMG * (1f + ArtyleriaObrazeniaBonus)).ToString(),
            () => Mathf.RoundToInt(BASE_ALT_DMG * (1f + ArtyleriaObrazeniaBonus + 0.25f)).ToString(),
            () => { ArtyleriaObrazeniaBonus += 0.25f; });

        Add(16, "Lustro: Odbicie (+30%)",
            () => Mathf.RoundToInt(LustroOdbicieBonus * 100f) + "%",
            () => Mathf.RoundToInt((LustroOdbicieBonus + 0.30f) * 100f) + "%",
            () => { LustroOdbicieBonus += 0.30f; });

        Add(17, "Zasadzka: Promien wybuchu (+20%)",
            () => (BASE_KAM_RAD * (1f + KamikazeRadiusBonus)).ToString("F1"),
            () => (BASE_KAM_RAD * (1f + KamikazeRadiusBonus + 0.20f)).ToString("F1"),
            () => { KamikazeRadiusBonus += 0.20f; });

        Add(18, "Zasadzka: Obrazenia wybuchu (+50)",
            () => Mathf.RoundToInt(BASE_KAM_DMG + KamikazeDamageBonus).ToString(),
            () => Mathf.RoundToInt(BASE_KAM_DMG + KamikazeDamageBonus + 50f).ToString(),
            () => { KamikazeDamageBonus += 50f; });

        Add(19, "Nalot: Zasieg (+25%)",
            () => (BASE_NALOT_RAD * (1f + NalotRadiusBonus)).ToString("F1"),
            () => (BASE_NALOT_RAD * (1f + NalotRadiusBonus + 0.25f)).ToString("F1"),
            () => { NalotRadiusBonus += 0.25f; });

        Add(20, "Nalot: Obrazenia (+50)",
            () => Mathf.RoundToInt(BASE_NALOT_DMG + NalotDamageBonus).ToString(),
            () => Mathf.RoundToInt(BASE_NALOT_DMG + NalotDamageBonus + 50f).ToString(),
            () => { NalotDamageBonus += 50f; });

        Add(21, "Tarcza: Czas trwania (+2s)",
            () => (BASE_SHIELD_DUR + ShieldDurationBonus).ToString("F1") + "s",
            () => (BASE_SHIELD_DUR + ShieldDurationBonus + 2f).ToString("F1") + "s",
            () => { ShieldDurationBonus += 2f; });

        Add(22, "Tarcza: Zasieg (+20%)",
            () => (BASE_SHIELD_RAD * (1f + ShieldRadiusBonus)).ToString("F1"),
            () => (BASE_SHIELD_RAD * (1f + ShieldRadiusBonus + 0.20f)).ToString("F1"),
            () => { ShieldRadiusBonus += 0.20f; });

        Add(23, "Doladowanie: Predkosc +0.5",
            () => (BASE_BOOST_FLAT + BoostFlatBonus).ToString("F1"),
            () => (BASE_BOOST_FLAT + BoostFlatBonus + 0.5f).ToString("F1"),
            () => { BoostFlatBonus += 0.5f; });

        Add(24, "Doladowanie: Czas (+3s)",
            () => (BASE_BOOST_DUR + BoostDurationBonus).ToString("F1") + "s",
            () => (BASE_BOOST_DUR + BoostDurationBonus + 3f).ToString("F1") + "s",
            () => { BoostDurationBonus += 3f; });
    }

    void Add(int id, string title, Func<string> cur, Func<string> next, Action apply)
        => _pool.Add(new DecreeData(id, title, cur, next, apply));

    // ── API publiczne ─────────────────────────────────────────────────────

    /// Ustaw filtr dekretów na podstawie kolejki pojazdów gracza.
    /// vehicleIndices: indeksy z GameConfig.vehicles (2 = Artyleria).
    public void SetQueueFilter(int[] vehicleIndices)
    {
        _queueFilter.Clear();
    }

    public List<DecreeData> GetRandomThree()
    {
        // Pula bez dekretów odfiltrowanych przez kolejkę gracza
        var list = new List<DecreeData>(_pool.Count);
        foreach (var d in _pool)
            if (!_queueFilter.Contains(d.Id)) list.Add(d);

        // Niezależny RNG: w MP każdy klient losuje osobno (różny TickCount + instanceID).
        // W SP zachowanie identyczne jak wcześniej.
        bool isMP = NetworkClient.active || NetworkServer.active;
        if (isMP)
        {
            var rng = new System.Random(unchecked(Environment.TickCount + GetInstanceID()));
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(0, i + 1);
                DecreeData tmp = list[i];
                list[i] = list[j];
                list[j] = tmp;
            }
        }
        else
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                DecreeData tmp = list[i];
                list[i] = list[j];
                list[j] = tmp;
            }
        }

        return list.GetRange(0, Mathf.Min(3, list.Count));
    }

    public void ApplyDecree(int id)
    {
        DecreeData d = _pool.Find(x => x.Id == id);
        d?.Apply();
    }

    // ── Pomocniki dla spawnerów pojazdów ──────────────────────────────────
    // baseValue = wartość z Inspektora prefabu pojazdu

    public float FinalHP(string vehicleType, float baseValue)
        => baseValue * (1f + HPBonus(vehicleType));

    // Prędkość: flat – bonus dodawany wprost do prędkości bazowej.
    public float FinalSpeed(string vehicleType, float baseValue)
        => baseValue + SpeedBonus(vehicleType);

    public float FinalArmor(string vehicleType, float baseValue)
        => baseValue * (1f + ArmorBonus(vehicleType));

    float HPBonus(string t)
    {
        switch (t)
        {
            case "Podstawowy": return PodstawowyHP;
            case "Tank":       return TankHP;
            case "Artyleria":  return ArtileriaHP;
            case "Lustro":     return LustroHP;      // zawsze 0 – brak dekretu HP
            case "Kamikaze":   return KamikazeHP;    // zawsze 0 – brak dekretu HP
            default:           return 0f;
        }
    }

    float SpeedBonus(string t)
    {
        switch (t)
        {
            case "Podstawowy": return PodstawowySpeed;
            case "Tank":       return TankSpeed;
            case "Artyleria":  return ArtileriaSpeed;
            case "Lustro":     return LustroSpeed;
            case "Kamikaze":   return KamikazeSpeed;
            default:           return 0f;
        }
    }

    float ArmorBonus(string t)
    {
        switch (t)
        {
            case "Podstawowy": return PodstawowyArmor;
            case "Tank":       return TankArmor;
            case "Artyleria":  return ArtileriaArmor;
            case "Lustro":     return LustroArmor;   // zawsze 0 – brak dekretu Pancerza
            case "Kamikaze":   return KamikazeArmor; // zawsze 0 – brak dekretu Pancerza
            default:           return 0f;
        }
    }
}
