using UnityEngine;

/// Odtwarza zapętloną playlistę muzyki.
/// Dołącz do pustego GameObject w scenie MainMenu.
/// DontDestroyOnLoad — muzyka gra przez cały czas.
[RequireComponent(typeof(AudioSource))]
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Header("Playlista (kolejność odtwarzania)")]
    public AudioClip[] utwory = new AudioClip[3];

    [Header("Głośność")]
    [Range(0f, 1f)] public float glosnosc = 0.5f;

    private AudioSource _source;
    private int _aktualny = 0;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _source = GetComponent<AudioSource>();
        _source.loop   = false;
        _source.volume = glosnosc;
    }

    void Start()
    {
        GrajKolejny();
    }

    void Update()
    {
        // Gdy utwór się skończy, przejdź do następnego
        if (!_source.isPlaying)
            GrajKolejny();
    }

    void GrajKolejny()
    {
        if (utwory == null || utwory.Length == 0) return;

        // Pomiń puste sloty
        for (int i = 0; i < utwory.Length; i++)
        {
            if (utwory[_aktualny] != null)
                break;
            _aktualny = (_aktualny + 1) % utwory.Length;
        }

        AudioClip klip = utwory[_aktualny];
        if (klip == null) return;

        _source.clip = klip;
        _source.Play();

        _aktualny = (_aktualny + 1) % utwory.Length;
    }

    // ── Public API ────────────────────────────────────────────────────────

    public void UstawGlosnosc(float v)
    {
        glosnosc = Mathf.Clamp01(v);
        _source.volume = glosnosc;
    }

    public void Zatrzymaj()  => _source.Stop();
    public void Wzznow()     => _source.Play();
}
