using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Makes an object softly glow and shows a small bobbing arrow above it, so the student knows what to tap next.
/// Put it on WaterDropper, WaterSample (the beaker) and CopperSulfateSample.
/// CuSO4PracticalSteps turns it on and off; you don't need to call anything yourself.
/// </summary>
public class InteractableHighlight : MonoBehaviour
{
    public Color glowColor = new Color(1f, 0.82f, 0.30f);
    [Range(0f, 3f)] public float maxIntensity = 0.9f;
    public float pulseSpeed = 3f;

    [Header("Arrow")]
    public bool showArrow = true;
    [Tooltip("Gap between the top of the object and the arrow tip, in metres")]
    public float arrowGap = 0.03f;
    [Tooltip("Arrow height in metres")]
    public float arrowSize = 0.025f;

    static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    readonly List<Material> _mats = new List<Material>();
    Renderer[] _renderers;
    Transform _arrow;
    bool _on;

    void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>();
        foreach (var r in _renderers)
        {
            foreach (var m in r.materials)            // per-object copies, made once
            {
                if (!m.HasProperty(EmissionId)) continue;
                m.EnableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                m.SetColor(EmissionId, Color.black);
                _mats.Add(m);
            }
        }
        if (showArrow) _arrow = BuildArrow();
        SetHighlighted(false);
    }

    void OnDestroy()
    {
        if (_arrow) Destroy(_arrow.gameObject);
    }

    public void SetHighlighted(bool on)
    {
        _on = on;
        if (_arrow) _arrow.gameObject.SetActive(on);
        if (!on) SetEmission(Color.black);
    }

    void Update()
    {
        if (!_on) return;
        float k = (Mathf.Sin(Time.time * pulseSpeed) * 0.5f + 0.5f) * maxIntensity;
        SetEmission(glowColor * k);

        if (_arrow)
        {
            Bounds b = WorldBounds();
            float bob = (Mathf.Sin(Time.time * 4f) * 0.5f + 0.5f) * 0.01f;
            _arrow.position = new Vector3(b.center.x, b.max.y + arrowGap + bob, b.center.z);
            _arrow.Rotate(Vector3.up, 90f * Time.deltaTime, Space.World);
        }
    }

    void SetEmission(Color c)
    {
        foreach (var m in _mats) m.SetColor(EmissionId, c);
    }

    Bounds WorldBounds()
    {
        Bounds b = new Bounds(transform.position, Vector3.zero);
        bool any = false;
        foreach (var r in _renderers)
        {
            if (!r || !r.enabled) continue;
            if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
        }
        return b;
    }

    Transform BuildArrow()
    {
        // A small cone pointing down, built in code
        const int seg = 20;
        float h = arrowSize, rad = arrowSize * 0.45f;
        var verts = new List<Vector3> { Vector3.zero, new Vector3(0f, h, 0f) };   // 0 = tip (bottom), 1 = top centre
        for (int i = 0; i < seg; i++)
        {
            float a = i / (float)seg * Mathf.PI * 2f;
            verts.Add(new Vector3(Mathf.Cos(a) * rad, h, Mathf.Sin(a) * rad));
        }
        var tris = new List<int>();
        for (int i = 0; i < seg; i++)
        {
            int a = 2 + i, b = 2 + (i + 1) % seg;
            tris.Add(0); tris.Add(a); tris.Add(b);     // sides
            tris.Add(1); tris.Add(b); tris.Add(a);     // top cap
        }
        var mesh = new Mesh { name = "HighlightArrow" };
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();

        var go = new GameObject(name + " Arrow");
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>();
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // Re-use this object's own shader so it is guaranteed to be included in the Android build
        Shader sh = null;
        foreach (var r in _renderers) if (r && r.sharedMaterial) { sh = r.sharedMaterial.shader; break; }
        if (sh)
        {
            var m = new Material(sh);
            if (m.HasProperty(BaseColorId)) m.SetColor(BaseColorId, glowColor);
            else m.color = glowColor;
            if (m.HasProperty(EmissionId)) { m.EnableKeyword("_EMISSION"); m.SetColor(EmissionId, glowColor * 0.8f); }
            mr.sharedMaterial = m;
        }
        return go.transform;
    }
}
