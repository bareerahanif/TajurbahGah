using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Draws pencil lines, dots and small labels flat on the paper, in world space.
/// Created automatically by ReflectionPractical; you don't add it yourself.
/// </summary>
public class PaperDrawing : MonoBehaviour
{
    public float lineWidth = 0.001f;       // 1 mm pencil line (slightly thicker than real so it shows on a phone)
    public Color pencilColor = new Color(0.12f, 0.12f, 0.16f);
    public Color labelColor = new Color(0.15f, 0.15f, 0.35f);

    Transform _paper;
    float _surfaceY;
    Material _mat;
    readonly List<GameObject> _all = new List<GameObject>();

    public float SurfaceY => _surfaceY;

    public void Init(Transform paper, Shader shader)
    {
        _paper = paper;
        var r = paper.GetComponent<Renderer>();
        _surfaceY = r ? r.bounds.max.y + 0.00012f : paper.position.y + 0.0003f;
        _mat = new Material(shader);
        if (_mat.HasProperty("_BaseColor")) _mat.SetColor("_BaseColor", pencilColor);
        _mat.color = pencilColor;
        if (_mat.HasProperty("_Smoothness")) _mat.SetFloat("_Smoothness", 0.1f);
    }

    public class Line
    {
        public GameObject go;
        public Vector3 a, b;
        public Mesh mesh;
        public float width;
    }

    /// <summary>A straight pencil line from a to b (heights are ignored; it lies on the paper).</summary>
    public Line DrawLine(Vector3 a, Vector3 b, float widthScale = 1f)
    {
        var go = new GameObject("PencilLine");
        go.transform.SetParent(transform, false);
        var l = new Line { go = go, mesh = new Mesh { name = "PencilLine" }, width = lineWidth * widthScale };
        go.AddComponent<MeshFilter>().sharedMesh = l.mesh;
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = _mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        SetLine(l, a, b);
        _all.Add(go);
        return l;
    }

    public void SetLine(Line l, Vector3 a, Vector3 b)
    {
        l.a = Flat(a); l.b = Flat(b);
        Vector3 d = l.b - l.a;
        if (d.sqrMagnitude < 1e-8f) d = Vector3.right * 1e-4f;
        Vector3 side = Vector3.Cross(Vector3.up, d.normalized) * (l.width * 0.5f);
        var v = new[] { l.a - side, l.a + side, l.b + side, l.b - side };
        l.mesh.Clear();
        l.mesh.vertices = v;
        Vector3 n = Vector3.Cross(v[1] - v[0], v[2] - v[0]);
        l.mesh.triangles = n.y >= 0 ? new[] { 0, 1, 2, 0, 2, 3 } : new[] { 0, 2, 1, 0, 3, 2 };
        l.mesh.RecalculateNormals();
        l.mesh.RecalculateBounds();
    }

    /// <summary>A small round pencil dot (or a pin hole when 'hole' is true).</summary>
    public GameObject Dot(Vector3 p, bool hole = false)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = hole ? "PinHole" : "Dot";
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(transform, false);
        float d = hole ? 0.0016f : 0.0022f;
        go.transform.position = Flat(p) + Vector3.up * 0.00005f;
        go.transform.localScale = new Vector3(d, 0.00005f, d);
        go.GetComponent<Renderer>().sharedMaterial = _mat;
        _all.Add(go);
        return go;
    }

    /// <summary>A small pencil-written label, e.g. "O", "N", "P<sub>1</sub>".</summary>
    public GameObject Label(string text, Vector3 p, float size = 0.075f)
    {
        var go = new GameObject("Label_" + text);
        go.transform.SetParent(transform, false);
        go.transform.position = Flat(p) + Vector3.up * 0.0002f;
        go.transform.rotation = Quaternion.Euler(90, _paper ? _paper.eulerAngles.y : 0f, 0);
        var t = go.AddComponent<TextMeshPro>();
        t.text = text;
        t.fontSize = size;
        t.color = labelColor;
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.fontStyle = FontStyles.Italic | FontStyles.Bold;
        t.rectTransform.sizeDelta = new Vector2(0.03f, 0.01f);
        _all.Add(go);
        return go;
    }

    /// <summary>Makes a line stand out (colour + thicker) while the student has to use it. Pass null colour to restore.</summary>
    public void Highlight(Line l, Color? color, float widthScale = 2.5f)
    {
        if (l == null || !l.go) return;
        var mr = l.go.GetComponent<MeshRenderer>();
        if (color.HasValue)
        {
            var m = new Material(_mat);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color.Value);
            m.color = color.Value;
            m.EnableKeyword("_EMISSION");
            if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", color.Value * 0.5f);
            mr.sharedMaterial = m;
            l.width = lineWidth * widthScale;
        }
        else
        {
            mr.sharedMaterial = _mat;
            l.width = lineWidth;
        }
        SetLine(l, l.a, l.b);
    }

    public void Remove(GameObject go)
    {
        if (!go) return;
        _all.Remove(go);
        Destroy(go);
    }

    public void ClearAll()
    {
        foreach (var g in _all) if (g) Destroy(g);
        _all.Clear();
    }

    public Vector3 Flat(Vector3 p) => new Vector3(p.x, _surfaceY, p.z);
}