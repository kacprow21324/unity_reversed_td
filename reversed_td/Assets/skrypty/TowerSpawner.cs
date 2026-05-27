using System.Collections.Generic;
using UnityEngine;

public class TowerSpawner : MonoBehaviour
{
    public static TowerSpawner Instance;

    [System.Serializable]
    public class TowerProgression
    {
        public GameObject prefab;
        [Min(1)]
        [Tooltip("Minimalna runda wymagana do odblokowania tej wiezy")]
        public int minRound = 1;
    }

    [Header("Progresja Wież")]
    public List<TowerProgression> towerProgressions = new List<TowerProgression>();

    [Header("Pozycjonowanie")]
    [Tooltip("Dodatkowe przesuniecie w gore po auto-wyrownaniu do powierzchni plyty")]
    [SerializeField] private float spawnYOffset = 0f;

    [Header("Izolacja Mapy (tylko Multiplayer)")]
    [Tooltip("Ustaw na korzeń swojej mapy w scenie MP. Null = tryb SP (globalne wyszukiwanie).")]
    public Transform mapRoot;

    private readonly List<GameObject> _activeTowers = new List<GameObject>();

    void Awake() => Instance = this;

    /// Punkt wejścia dla trybu multiplayer — inicjalizuje RNG seedem zanim
    /// cokolwiek zostanie wygenerowane, gwarantując identyczny układ u obu graczy.
    public void GenerateMapWithSeed(int seed, int round = 1)
    {
        Random.InitState(seed);
        GenerateTowers(round);
    }

    public void GenerateTowers(int round)
    {
        DestroyExistingTowers();

        GameObject[] plates = GetPlatesInScope();
        if (plates.Length == 0)
        {
            Debug.LogWarning("TowerSpawner: Brak obiektow z tagiem TowerPlate" +
                (mapRoot != null ? $" w '{mapRoot.name}'" : " na scenie") + ".");
            return;
        }

        // Liczba wież = round + 3 (min 4 w rundzie 1), nie więcej niż dostępnych płyt
        int towerCount = Mathf.Min(round + 3, plates.Length);

        List<TowerProgression> available = towerProgressions.FindAll(
            t => t.prefab != null && t.minRound <= round);

        if (available.Count == 0)
        {
            Debug.LogWarning("TowerSpawner: Brak odblokowanych wież dla rundy " + round + ".");
            return;
        }

        Shuffle(plates);

        for (int i = 0; i < towerCount; i++)
        {
            // Ogranicz WiezaSonar do max 2 na mapie
            List<TowerProgression> options = available;
            if (WiezaSonar.ActiveRadarsCount >= 2)
            {
                var bezSonara = available.FindAll(t => t.prefab.GetComponent<WiezaSonar>() == null);
                if (bezSonara.Count > 0) options = bezSonara;
            }

            TowerProgression chosen = options[Random.Range(0, options.Count)];
            Transform plate = plates[i].transform;
            GameObject tower = Instantiate(chosen.prefab, plate.position, plate.rotation);
            tower.transform.SetParent(plate);
            UstawWysokoscWiezy(tower, plate.position.y + spawnYOffset);
            _activeTowers.Add(tower);
        }
    }

    // Singleplayer (mapRoot == null): globalne FindGameObjectsWithTag.
    // Multiplayer  (mapRoot != null): przeszukuje dzieci swojego korzenia mapy;
    //   jezeli mapRoot nie zawiera TowerPlate (bledna konfiguracja SP), fallback globalny.
    GameObject[] GetPlatesInScope()
    {
        if (mapRoot != null)
        {
            var result = new List<GameObject>();
            foreach (Transform t in mapRoot.GetComponentsInChildren<Transform>(true))
                if (t.CompareTag("TowerPlate")) result.Add(t.gameObject);

            if (result.Count > 0)
                return result.ToArray();

            Debug.LogWarning($"[TowerSpawner] mapRoot '{mapRoot.name}' nie zawiera obiektow z tagiem TowerPlate. Szukam globalnie (SP fallback).");
        }

        return GameObject.FindGameObjectsWithTag("TowerPlate");
    }

    void DestroyExistingTowers()
    {
        foreach (var t in _activeTowers)
            if (t != null) t.SetActive(false); // SetActive(false) wywołuje OnDisable natychmiast
        foreach (var t in _activeTowers)
            if (t != null) Destroy(t);
        _activeTowers.Clear();
    }

    static void UstawWysokoscWiezy(GameObject tower, float targetBottomY)
    {
        // Zbierz granice wszystkich rendererów dziecka, by znaleźć dno modelu
        Renderer[] renderers = tower.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            b.Encapsulate(renderers[i].bounds);

        // Przesuń wieżę tak, żeby jej dno znalazło się na targetBottomY
        tower.transform.position += Vector3.up * (targetBottomY - b.min.y);
    }

    static void Shuffle<T>(T[] array)
    {
        for (int i = array.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (array[i], array[j]) = (array[j], array[i]);
        }
    }
}
