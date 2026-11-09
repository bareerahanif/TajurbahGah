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

    [Header("Ring")]
    [Tooltip("A pulsing glowing ring around the object. Shows up even on white objects, where the glow is hard to see.")]
    public bool showRing = true;
    [Tooltip("Ring size compared with the object's width")]
    public float ringScale = 1.15f;
    [Tooltip("Ring thickness in metres")]
    public float ringWidth = 0.004f;

    static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    readonly List<Material> _mats = new List<Material>();
    readonly List<Material> _glowMats = new List<Material>();
    Color _currentColor;
    bool _arrowOn;
    Renderer[] _renderers;
    Transform _arrow;
    Transform _ring;
    float _ringBaseRadius = 1f;
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
        _currentColor = glowColor;
        if (showArrow) _arrow = BuildArrow();
        if (showRing) _ring = BuildRing();
        SetHighlighted(false);
    }

    void OnDestroy()
    {
        if (_arrow) Destroy(_arrow.gameObject);
        if (_ring) Destroy(_ring.gameObject);
    }

    public void SetHighlighted(bool on) => SetHighlighted(on, glowColor, true);

    /// <summary>Turn the highlight on/off with a specific colour, with or without the bobbing arrow.</summary>
    public void SetHighlighted(bool on, Color color, bool withArrow)
    {
        _on = on;
        _arrowOn = withArrow;
        _currentColor = color;
        foreach (var m in _glowMats)
        {
            if (m.HasProperty(BaseColorId)) m.SetColor(BaseColorId, color); else m.color = color;
            if (m.HasProperty(EmissionId)) m.SetColor(EmissionId, color * 0.8f);
        }
        if (_arrow) _arrow.gameObject.SetActive(on && withArrow);
        if (_ring) _ring.gameObject.SetActive(on);
        if (!on) SetEmission(Color.black);
    }

    void Update()
    {
        if (!_on) return;
        float k = (Mathf.Sin(Time.time * pulseSpeed) * 0.5f + 0.5f) * maxIntensity;
        SetEmission(_currentColor * k);

        if (_arrow && _arrowOn)
        {
            Bounds b = WorldBounds();
            float bob = (Mathf.Sin(Time.time * 4f) * 0.5f + 0.5f) * 0.01f;
            _arrow.position = new Vector3(b.center.x, b.max.y + arrowGap + bob, b.center.z);
            _arrow.Rotate(Vector3.up, 90f * Time.deltaTime, Space.World);
        }

        if (_ring)
        {
            Bounds b = WorldBounds();
            float r = Mathf.Max(b.extents.x, b.extents.z) * ringScale;
            float pulse = 1f + (Mathf.Sin(Time.time * pulseSpeed) * 0.5f + 0.5f) * 0.12f;
            _ring.position = new Vector3(b.center.x, b.max.y + 0.003f, b.center.z);
            _ring.localScale = Vector3.one * (r * pulse / _ringBaseRadius);
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

    Transform BuildRing()
    {
        // A flat ring (outer radius 1 before scaling); its width is kept in metres using the first known size
        const int seg = 48;
        Bounds b = WorldBounds();
        float r = Mathf.Max(0.005f, Mathf.Max(b.extents.x, b.extents.z) * ringScale);
        _ringBaseRadius = 1f;
        float inner = Mathf.Clamp01(1f - ringWidth / r);
        var verts = new List<Vector3>();
        var tris = new List<int>();
        for (int i = 0; i < seg; i++)
        {
            float a = i / (float)seg * Mathf.PI * 2f;
            Vector3 d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            verts.Add(d);            // outer
            verts.Add(d * inner);    // inner
        }
        for (int i = 0; i < seg; i++)
        {
            int o0 = i * 2, i0 = o0 + 1, o1 = ((i + 1) % seg) * 2, i1 = o1 + 1;
            // top faces
            tris.Add(o0); tris.Add(i0); tris.Add(o1);
            tris.Add(o1); tris.Add(i0); tris.Add(i1);
            // bottom faces (so it is visible from any angle)
            tris.Add(o0); tris.Add(o1); tris.Add(i0);
            tris.Add(o1); tris.Add(i1); tris.Add(i0);
        }
        var mesh = new Mesh { name = "HighlightRing" };
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();

        var go = new GameObject(name + " Ring");
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>();
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.sharedMaterial = MakeGlowMaterial();
        return go.transform;
    }

    Material MakeGlowMaterial()
    {
        // Re-use this object's own shader so it is guaranteed to be included in the Android build
        Shader sh = null;
        foreach (var r in _renderers) if (r && r.sharedMaterial) { sh = r.sharedMaterial.shader; break; }
        if (!sh) return null;
        var m = new Material(sh);
        if (m.HasProperty(BaseColorId)) m.SetColor(BaseColorId, glowColor);
        else m.color = glowColor;
        if (m.HasProperty(EmissionId)) { m.EnableKeyword("_EMISSION"); m.SetColor(EmissionId, glowColor * 0.8f); }
        _glowMats.Add(m);
        return m;
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

        mr.sharedMaterial = MakeGlowMaterial();
        return go.transform;
    }
}