using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

// WKOLCE – krótki zasięg. Rozkłada kolce-pułapki na ścieżce NavMesh w swoim zasięgu.
public class WiezaKolcowa : WiezaBaza
{
    [Header("Parametry Kolców")]
    public int iloscKolcow = 6;
    public float promienRozmieszczenia = 4.5f; // ZWIĘKSZONO! Żeby łatwiej łapał drogę
    public float obrazeniaKolca = 30f;
    public float cooldownPrzeladowania = 8f;

    [Header("Prefab Kolca (opcjonalnie — jeśli pusty, użyje żółtej kapsuły)")]
    public GameObject prefabKolca;

    private readonly List<GameObject> _kolce = new List<GameObject>();
    private bool _przeladowuje = false;

    protected override void Start()
    {
        base.Start();
        UtworzKragZasiegu(promienRozmieszczenia, new Color(1f, 0.9f, 0f, 0.85f));
        RozstawKolce();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        _kolce.RemoveAll(k => k == null);
        _przeladowuje = false;
    }

    protected override void Update()
    {
        base.Update();
        if (!_przeladowuje && WszystkieKolceUzyte())
            StartCoroutine(Przeladuj());
    }

    void RozstawKolce()
    {
        // Seed deterministyczny z lokalnej pozycji TowerPlate.
        Vector3 lp = transform.parent != null
            ? transform.parent.localPosition
            : transform.localPosition;
        int sx = Mathf.RoundToInt(lp.x * 137f);
        int sz = Mathf.RoundToInt(lp.z * 173f);
        var rng = new System.Random(sx * 100003 + sz);

        // Szukamy NavMesh w bliskim sąsiedztwie punktu kandydatowego.
        // Promień wyszukiwania = promienRozmieszczenia, żeby nie wyciągać kolców
        // poza obszar wieży. Y kandydata = wysokość wieży (nie hardkodowane 0).
        float szukajPromien = promienRozmieszczenia;
        float wiezaY        = transform.position.y;

        int rozmieszczono = 0;
        int maxProb       = iloscKolcow * 60;

        for (int proba = 0; proba < maxProb && rozmieszczono < iloscKolcow; proba++)
        {
            double angle    = rng.NextDouble() * System.Math.PI * 2.0;
            double r        = System.Math.Sqrt(rng.NextDouble()) * promienRozmieszczenia;
            var    offset2D = new Vector2(
                (float)(System.Math.Cos(angle) * r),
                (float)(System.Math.Sin(angle) * r));

            // Kandydat na poziomie wieży — prawidłowa wysokość dla SamplePosition
            Vector3 kandydat = new Vector3(
                transform.position.x + offset2D.x,
                wiezaY,
                transform.position.z + offset2D.y);

            NavMeshHit hit;
            if (!NavMesh.SamplePosition(kandydat, out hit, szukajPromien, NavMesh.AllAreas))
                continue;

            // Akceptujemy każdy punkt znaleziony w promieniu SamplePosition — drugi
            // filtr XZ jest zbędny skoro promień wyszukiwania = promienRozmieszczenia.
            _kolce.Add(UtworzKolec(hit.position + Vector3.up * 0.4f));
            rozmieszczono++;
        }

        if (rozmieszczono == 0)
            Debug.LogWarning($"[WiezaKolcowa] Nie znaleziono NavMesh w promieniu {szukajPromien}m od {transform.position}. Sprawdź czy NavMesh jest wypalony przy TowerPlate.");
    }

    GameObject UtworzKolec(Vector3 pozycja)
    {
        GameObject kolcGO;

        if (prefabKolca != null)
        {
            // BARDZO WAŻNE: Kolce NIE są dziećmi wieży!
            kolcGO = Instantiate(prefabKolca, pozycja, Quaternion.identity);
            kolcGO.name = "Kolec";
            kolcGO.transform.SetParent(null);

            var col = kolcGO.GetComponent<Collider>();
            if (col != null) col.isTrigger = true;

            Kolec skrypt = kolcGO.GetComponent<Kolec>() ?? kolcGO.AddComponent<Kolec>();
            skrypt.obrazenia = obrazeniaKolca;
        }
        else
        {
            // Fallback: żółta kapsuła (placeholder do czasu przypisania prefabu)
            kolcGO = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            kolcGO.name = "Kolec";
            kolcGO.transform.SetParent(null);
            kolcGO.transform.position = pozycja;
            kolcGO.transform.localScale = new Vector3(0.25f, 0.5f, 0.25f);
            kolcGO.GetComponent<CapsuleCollider>().isTrigger = true;
            kolcGO.GetComponent<Renderer>().material.color = Color.yellow;
            Kolec skrypt = kolcGO.AddComponent<Kolec>();
            skrypt.obrazenia = obrazeniaKolca;
        }

        return kolcGO;
    }

    bool WszystkieKolceUzyte()
    {
        if (_kolce.Count == 0) return false;
        foreach (var k in _kolce)
            if (k != null) return false;
        return true;
    }

    IEnumerator Przeladuj()
    {
        _przeladowuje = true;
        yield return new WaitForSeconds(cooldownPrzeladowania);
        _kolce.Clear();
        RozstawKolce();
        _przeladowuje = false;
    }

    protected override void OnZniszcz()
    {
        StopAllCoroutines();
        // Kolce rozstawione przed zniszczeniem wieży zostają na mapie do końca rundy.
        // CleanupBattlefield() w GameplayUIManager usuwa je po zakończeniu rundy.
        _kolce.Clear();
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.9f, 0f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, promienRozmieszczenia);
    }
}