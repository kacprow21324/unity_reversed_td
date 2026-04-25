using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// WKOLCE – krótki zasięg. Rozkłada wokół siebie kolce-pułapki.
// Po zużyciu wszystkich kolców czeka z przeładowaniem.
public class WiezaKolcowa : WiezaBaza
{
    [Header("Parametry Kolców")]
    public int iloscKolcow = 6;
    public float promienRozmieszczenia = 3.5f;
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
        // Resetuj kolce przy re-aktywacji
        foreach (var k in _kolce)
            if (k != null) k.SetActive(true);
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
        for (int i = 0; i < iloscKolcow; i++)
        {
            float kat = (360f / iloscKolcow) * i * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(kat), 0f, Mathf.Sin(kat)) * promienRozmieszczenia;
            Vector3 pozycja = transform.position + offset + Vector3.up * 0.4f;

            GameObject kolcGO = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            kolcGO.name = "Kolec";
            kolcGO.transform.SetParent(transform);
            kolcGO.transform.position = pozycja;
            kolcGO.transform.localScale = new Vector3(0.25f, 0.5f, 0.25f);

            CapsuleCollider col = kolcGO.GetComponent<CapsuleCollider>();
            col.isTrigger = true;

            Renderer r = kolcGO.GetComponent<Renderer>();
            r.material.color = new Color(0.3f, 0.3f, 0.35f);

            Kolec skrypt = kolcGO.AddComponent<Kolec>();
            skrypt.obrazenia = obrazeniaKolca;

            _kolce.Add(kolcGO);
        }
    }

    bool WszystkieKolceUzyte()
    {
        foreach (var k in _kolce)
            if (k != null && k.activeInHierarchy) return false;
        return _kolce.Count > 0;
    }

    IEnumerator Przeladuj()
    {
        _przeladowuje = true;
        yield return new WaitForSeconds(cooldownPrzeladowania);
        foreach (var k in _kolce)
            if (k != null) k.SetActive(true);
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
        Gizmos.color = Color.grey;
        Gizmos.DrawWireSphere(transform.position, promienRozmieszczenia + 0.5f);
    }
}
