using System.Collections.Generic;
using UnityEngine;

/// Własna obramówka wieży metodą Inverted Hull.
/// Dodawana automatycznie przez WiezaBaza — nie musisz nic ustawiać ręcznie.
[DisallowMultipleComponent]
public class TowerOutline : MonoBehaviour
{
    [Header("Ustawienia (opcjonalne — można zostawić domyślne)")]
    public Color outlineColor = new Color(0.3f, 0.85f, 1f, 1f);
    [Range(0.01f, 0.15f)]
    public float outlineWidth = 0.04f;

    private readonly List<GameObject> _copies   = new List<GameObject>();
    private Material                  _mat;
    private bool                      _built;

    // ── API ───────────────────────────────────────────────────────────────

    public void Pokaz()
    {
        if (!_built) Build();
        foreach (var c in _copies) if (c != null) c.SetActive(true);
    }

    public void Ukryj()
    {
        foreach (var c in _copies) if (c != null) c.SetActive(false);
    }

    // ── Budowanie kopii meshów ────────────────────────────────────────────

    void Build()
    {
        _built = true;

        var shader = Shader.Find("Custom/TowerOutline");
        if (shader == null)
        {
            Debug.LogWarning("[TowerOutline] Shader 'Custom/TowerOutline' nie został znaleziony. Sprawdź czy plik TowerOutlineShader.shader jest w Assets.");
            return;
        }

        _mat = new Material(shader)
        {
            name = "TowerOutlineMat"
        };
        _mat.SetColor("_OutlineColor", outlineColor);
        _mat.SetFloat("_OutlineWidth", outlineWidth);

        foreach (var mr in GetComponentsInChildren<MeshRenderer>())
        {
            // Pomiń TMP i LineRenderer
            if (mr.GetComponent<TMPro.TMP_Text>() != null) continue;
            var mf = mr.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) continue;

            var copy = new GameObject("_Outline_" + mr.gameObject.name);
            copy.transform.SetParent(mr.transform, false);
            copy.transform.localPosition = Vector3.zero;
            copy.transform.localRotation = Quaternion.identity;
            copy.transform.localScale    = Vector3.one;

            var copyMF        = copy.AddComponent<MeshFilter>();
            copyMF.sharedMesh = mf.sharedMesh;

            var copyMR                   = copy.AddComponent<MeshRenderer>();
            copyMR.sharedMaterial        = _mat;
            copyMR.shadowCastingMode     = UnityEngine.Rendering.ShadowCastingMode.Off;
            copyMR.receiveShadows        = false;
            copyMR.lightProbeUsage       = UnityEngine.Rendering.LightProbeUsage.Off;
            copyMR.reflectionProbeUsage  = UnityEngine.Rendering.ReflectionProbeUsage.Off;

            copy.SetActive(false);
            _copies.Add(copy);
        }
    }

    void OnDestroy()
    {
        if (_mat != null) Destroy(_mat);
    }
}
