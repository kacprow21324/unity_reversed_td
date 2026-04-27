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

    private readonly List<GameObject> _kolce = new List<GameObject>();
    private bool _przeladowuje = false;

    protected override void Start()
    {
        maxHP = 200f;
        nagrodaZlota = 60;
        base.Start();
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
        int rozmieszczono = 0;
        int maxProb = iloscKolcow * 40; 

        for (int proba = 0; proba < maxProb && rozmieszczono < iloscKolcow; proba++)
        {
            Vector2 offset2D = Random.insideUnitCircle * promienRozmieszczenia;
         
            Vector3 kandydat = new Vector3(transform.position.x + offset2D.x, 0f, transform.position.z + offset2D.y);

            NavMeshHit hit;
            if (!NavMesh.SamplePosition(kandydat, out hit, 20f, NavMesh.AllAreas)) continue;
            Vector2 pozycjaWiezy2D = new Vector2(transform.position.x, transform.position.z);
            Vector2 pozycjaHita2D = new Vector2(hit.position.x, hit.position.z);
            
            if (Vector2.Distance(pozycjaWiezy2D, pozycjaHita2D) > promienRozmieszczenia + 1f) continue;

            _kolce.Add(UtworzKolec(hit.position + Vector3.up * 0.4f));
            rozmieszczono++;
        }
    }

    GameObject UtworzKolec(Vector3 pozycja)
    {
        GameObject kolcGO = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        kolcGO.name = "Kolec";
        
        // BARDZO WAŻNE: Kolce NIE są dziećmi wieży!
        kolcGO.transform.SetParent(null); 
        
        kolcGO.transform.position = pozycja;
        kolcGO.transform.localScale = new Vector3(0.25f, 0.5f, 0.25f);

        kolcGO.GetComponent<CapsuleCollider>().isTrigger = true;
        kolcGO.GetComponent<Renderer>().material.color = Color.yellow;

        Kolec skrypt = kolcGO.AddComponent<Kolec>();
        skrypt.obrazenia = obrazeniaKolca;

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
        foreach (var k in _kolce)
            if (k != null) Destroy(k);
        _kolce.Clear();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, promienRozmieszczenia);
    }
}