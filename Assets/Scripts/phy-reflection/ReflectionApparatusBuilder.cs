using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// Builds every piece of apparatus for "Laws of Reflection (plane mirror)" in code, at real-world size (metres):
///   drawing board, white A4 sheet, 4 drawing pins, 4 common pins (P1-P4) in a pin tray,
///   plane mirror strip (front glass + silvered back + backing paint) on a wooden stand,
///   5 cm protractor with inner and outer 0-180 scales, 15 cm ruler with mm/cm marks, and a pencil.
///
/// How to use:
///   1. Create an empty GameObject on your bench, name it "ReflectionApparatus", add this component.
///   2. Click the three dots on the component > "Build apparatus".
///   3. (Optional) three dots > "Save as prefab" to reuse it in other scenes.
/// Pieces are named clearly (DrawingBoard, Paper, Mirror, Protractor, Ruler, Pin_P1...) so the practical script can find them.
/// </summary>
public class ReflectionApparatusBuilder : MonoBehaviour
{
    [Header("Colours")]
    public Color boardColor = new Color32(196, 150, 98, 255);
    public Color paperColor = new Color32(250, 250, 246, 255);
    public Color drawingPinColor = new Color32(214, 170, 64, 255);
    public Color pinHeadColorP1P2 = new Color32(220, 40, 40, 255);
    public Color pinHeadColorP3P4 = new Color32(40, 90, 220, 255);
    public Color standColor = new Color32(120, 82, 48, 255);
    public Color protractorTint = new Color(0.85f, 0.95f, 1f, 0.35f);
    public Color rulerColor = new Color32(250, 248, 235, 255);
    public Color pencilColor = new Color32(250, 196, 30, 255);

    [Header("Sizes (metres)")]
    public Vector3 boardSize = new Vector3(0.45f, 0.018f, 0.34f);
    public Vector2 paperSize = new Vector2(0.297f, 0.21f);          // A4 landscape
    public Vector3 mirrorSize = new Vector3(0.15f, 0.05f, 0.003f);   // width, height, glass thickness
    public float protractorRadius = 0.05f;
    public float rulerLength = 0.30f;
    [Tooltip("Makes the common pins a bit thicker than real so they are easy to see and tap on a phone (1 = real size)")]
    public float pinVisibility = 1.6f;

    readonly List<Object> _generated = new List<Object>();

    // =====================================================================

    [ContextMenu("Build apparatus")]
    public void Build()
    {
        Clear();
        _generated.Clear();

        var board = BuildBoard();
        var paper = BuildPaper(board);
        BuildDrawingPins(paper);
        BuildPinTray(board);
        var mirror = BuildMirror(board);
        var prot = BuildProtractor(board);
        var ruler = BuildRuler(board);
        BuildPencil(board);

#if UNITY_EDITOR
        if (!Application.isPlaying) EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
        Debug.Log("[Reflection] Apparatus built.");
    }

    [ContextMenu("Clear apparatus")]
    public void Clear()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var c = transform.GetChild(i).gameObject;
            if (Application.isPlaying) Destroy(c); else DestroyImmediate(c);
        }
    }

    // =========================== pieces ==================================

    Transform BuildBoard()
    {
        var go = Box("DrawingBoard", transform, boardSize, new Vector3(0, boardSize.y * 0.5f, 0),
                     Mat(boardColor, 0.25f, 0f, false, WoodTexture()));
        go.AddComponent<BoxCollider>();
        return go.transform;
    }

    Transform BuildPaper(Transform board)
    {
        float y = boardSize.y + 0.0002f;
        var go = Box("Paper", transform, new Vector3(paperSize.x, 0.0004f, paperSize.y), new Vector3(0, y, 0),
                     Mat(paperColor, 0.08f, 0f));
        go.AddComponent<BoxCollider>();
        return go.transform;
    }

    void BuildDrawingPins(Transform paper)
    {
        var parent = Group("DrawingPins");
        float y = boardSize.y + 0.0004f;
        float dx = paperSize.x * 0.5f - 0.012f, dz = paperSize.y * 0.5f - 0.012f;
        Vector3[] corners = { new Vector3(-dx, y, -dz), new Vector3(dx, y, -dz), new Vector3(dx, y, dz), new Vector3(-dx, y, dz) };
        var m = Mat(drawingPinColor, 0.75f, 0.8f);
        for (int i = 0; i < 4; i++)
        {
            var pin = Cylinder("DrawingPin_" + (i + 1), parent, 0.0055f, 0.0012f, corners[i] + Vector3.up * 0.0006f, m);
            // small dome on top
            var dome = Sphere("Dome", pin.transform, 0.006f, Vector3.up * 0.0006f, m);
            dome.transform.localScale = new Vector3(0.006f, 0.0025f, 0.006f);
        }
    }

    void BuildPinTray(Transform board)
    {
        // A small foam pin cushion beside the paper, holding P1-P4 upright until the student uses them
        var parent = Group("PinTray");
        float x = boardSize.x * 0.5f - 0.035f;
        float z = -boardSize.z * 0.5f + 0.03f;
        var tray = Box("Cushion", parent, new Vector3(0.05f, 0.012f, 0.03f),
                       new Vector3(x, boardSize.y + 0.006f, z), Mat(new Color32(60, 70, 90, 255), 0.1f, 0f));
        tray.AddComponent<BoxCollider>();

        string[] names = { "Pin_P1", "Pin_P2", "Pin_P3", "Pin_P4" };
        for (int i = 0; i < 4; i++)
        {
            Color head = i < 2 ? pinHeadColorP1P2 : pinHeadColorP3P4;
            var p = BuildCommonPin(names[i], head);
            p.transform.SetParent(transform, false);
            p.transform.localPosition = new Vector3(x - 0.018f + i * 0.012f, boardSize.y + 0.012f, z);
        }
    }

    /// <summary>A common pin: steel shaft, coloured ball head. Pivot at the point (bottom), so it stands on the paper.</summary>
    GameObject BuildCommonPin(string name, Color headColor)
    {
        float len = 0.03f;
        float r = 0.00035f * pinVisibility;
        var root = new GameObject(name);
        var shaftMat = Mat(new Color32(200, 205, 212, 255), 0.85f, 0.9f);
        var shaft = Cylinder("Shaft", root.transform, r, len, new Vector3(0, len * 0.5f, 0), shaftMat);
        var point = Cone("Point", root.transform, r, 0.002f, Vector3.zero, shaftMat);
        Sphere("Head", root.transform, 0.0018f * pinVisibility, new Vector3(0, len, 0), Mat(headColor, 0.6f, 0f));

        // Generous invisible collider so pins are easy to tap and drag on a phone
        var col = root.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0, len * 0.5f, 0);
        col.height = len + 0.006f;
        col.radius = 0.004f;
        return root;
    }

    Transform BuildMirror(Transform board)
    {
        // Pivot = bottom of the SILVERED surface, centred. That is the line that should sit on MM'.
        var root = new GameObject("Mirror").transform;
        root.SetParent(transform, false);
        root.localPosition = new Vector3(0, boardSize.y + 0.0004f, paperSize.y * 0.5f + 0.03f);   // starts off the paper
        root.localRotation = Quaternion.Euler(0, 180, 0);   // reflecting side faces the student (towards the front of the board)

        float w = mirrorSize.x, h = mirrorSize.y, t = mirrorSize.z;
        // Front glass (light passes through it to the silvering behind)
        var glass = Box("FrontGlass", root, new Vector3(w, h, t), new Vector3(0, h * 0.5f, t * 0.5f),
                        Mat(new Color(0.75f, 0.9f, 0.88f, 0.25f), 0.95f, 0f, true));
        // Silvered surface: where reflection really happens
        Box("SilveredSurface", root, new Vector3(w, h, 0.0004f), new Vector3(0, h * 0.5f, -0.0002f),
            Mat(new Color(0.92f, 0.93f, 0.95f), 0.97f, 1f));
        // Back paint (protects the silvering)
        Box("BackPaint", root, new Vector3(w, h, 0.0006f), new Vector3(0, h * 0.5f, -0.0007f),
            Mat(new Color32(110, 40, 36, 255), 0.3f, 0f));
        // Wooden stand behind it, holding it upright
        var standMat = Mat(standColor, 0.3f, 0f, false, WoodTexture());
        Box("Stand", root, new Vector3(w * 0.8f, 0.018f, 0.022f), new Vector3(0, 0.009f, -0.012f), standMat);
        Box("StandLip", root, new Vector3(w * 0.8f, 0.006f, 0.004f), new Vector3(0, 0.003f, t + 0.002f), standMat);

        var col = root.gameObject.AddComponent<BoxCollider>();
        col.center = new Vector3(0, h * 0.5f, -0.005f);
        col.size = new Vector3(w, h, 0.026f);
        return root;
    }

    Transform BuildProtractor(Transform board)
    {
        // Flat semicircle, baseline along X, arc towards +Z. Pivot = centre point of the baseline.
        var root = new GameObject("Protractor").transform;
        root.SetParent(transform, false);
        root.localPosition = new Vector3(-0.07f, boardSize.y + 0.0006f, -boardSize.z * 0.5f + 0.006f);

        float R = protractorRadius, th = 0.0015f;
        var body = new GameObject("Body");
        body.transform.SetParent(root, false);
        body.AddComponent<MeshFilter>().sharedMesh = Track(SemicircleMesh(R, th, 64));
        var bodyMat = Mat(protractorTint, 0.9f, 0f, true);
        bodyMat.renderQueue = (int)RenderQueue.Transparent - 50;   // always drawn BEFORE the numbers, so it never covers them
        body.AddComponent<MeshRenderer>().sharedMaterial = bodyMat;

        // Tick marks (one combined mesh)
        var verts = new List<Vector3>();
        var tris = new List<int>();
        float yTop = th + 0.0001f;
        for (int deg = 0; deg <= 180; deg++)
        {
            float len = deg % 10 == 0 ? 0.0045f : deg % 5 == 0 ? 0.003f : 0.0018f;
            float wdt = deg % 10 == 0 ? 0.00028f : 0.00018f;
            float a = deg * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
            AddFlatQuad(verts, tris, dir * (R - len * 0.5f - 0.0004f) + Vector3.up * yTop, dir, len, wdt);
        }
        // Baseline and centre mark
        AddFlatQuad(verts, tris, new Vector3(0, yTop, 0.0008f), Vector3.right, R * 2f - 0.004f, 0.00025f);
        AddFlatQuad(verts, tris, new Vector3(0, yTop, 0.004f), Vector3.forward, 0.008f, 0.00025f);
        // 90 degree guide line (the normal)
        AddFlatQuad(verts, tris, new Vector3(0, yTop, R * 0.35f), Vector3.forward, R * 0.5f, 0.00015f);
        var ticks = new GameObject("Scale");
        ticks.transform.SetParent(root, false);
        ticks.AddComponent<MeshFilter>().sharedMesh = Track(MakeMesh("ProtractorTicks", verts, tris));
        ticks.AddComponent<MeshRenderer>().sharedMaterial = Mat(new Color(0.08f, 0.1f, 0.15f), 0.2f, 0f);

        // Numbers: outer scale 0 (right) -> 180 (left); inner scale 180 (right) -> 0 (left)
        var numbers = new GameObject("Numbers").transform;
        numbers.SetParent(root, false);
        for (int deg = 0; deg <= 180; deg += 10)
        {
            // 0 and 180 sit right on the baseline, so nudge them up a little to keep them on the plastic
            float placeDeg = deg == 0 ? 5f : deg == 180 ? 175f : deg;
            float a = placeDeg * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
            Quaternion rot = Quaternion.Euler(0, 90f - deg, 0) * Quaternion.Euler(90, 0, 0);
            FlatText(numbers, deg.ToString(), dir * (R - 0.0078f) + Vector3.up * (yTop + 0.0003f), rot, 0.034f,
                     new Color(0.08f, 0.1f, 0.15f));
            FlatText(numbers, (180 - deg).ToString(), dir * (R - 0.0138f) + Vector3.up * (yTop + 0.0003f), rot, 0.026f,
                     new Color(0.75f, 0.1f, 0.1f));
        }

        var col = root.gameObject.AddComponent<BoxCollider>();
        col.center = new Vector3(0, th * 0.5f, R * 0.5f);
        col.size = new Vector3(R * 2f, 0.004f, R);
        return root;
    }

    Transform BuildRuler(Transform board)
    {
        // Length along X, marks on the -Z (front) edge. Pivot = the 0 mark on the front edge.
        var root = new GameObject("Ruler").transform;
        root.SetParent(transform, false);
        // Lies along the left edge of the board (30 cm fits the board's depth), marked edge facing the paper
        root.localPosition = new Vector3(-boardSize.x * 0.5f + 0.052f, boardSize.y + 0.0006f, -rulerLength * 0.5f);
        root.localRotation = Quaternion.Euler(0, -90, 0);

        float margin = 0.006f, L = rulerLength + margin * 2f, W = 0.03f, th = 0.002f;
        Box("Body", root, new Vector3(L, th, W), new Vector3(rulerLength * 0.5f, th * 0.5f, W * 0.5f), Mat(rulerColor, 0.5f, 0f));

        var verts = new List<Vector3>();
        var tris = new List<int>();
        float yTop = th + 0.0001f;
        int mm = Mathf.RoundToInt(rulerLength * 1000f);
        for (int i = 0; i <= mm; i++)
        {
            float len = i % 10 == 0 ? 0.006f : i % 5 == 0 ? 0.0045f : 0.003f;
            AddFlatQuad(verts, tris, new Vector3(i * 0.001f, yTop, len * 0.5f), Vector3.forward, len, i % 10 == 0 ? 0.00025f : 0.00015f);
        }
        var ticks = new GameObject("Scale");
        ticks.transform.SetParent(root, false);
        ticks.AddComponent<MeshFilter>().sharedMesh = Track(MakeMesh("RulerTicks", verts, tris));
        ticks.AddComponent<MeshRenderer>().sharedMaterial = Mat(new Color(0.08f, 0.1f, 0.15f), 0.2f, 0f);

        var numbers = new GameObject("Numbers").transform;
        numbers.SetParent(root, false);
        for (int cm = 0; cm <= mm / 10; cm++)
            FlatText(numbers, cm.ToString(), new Vector3(cm * 0.01f, yTop + 0.0001f, 0.0095f), Quaternion.Euler(90, 0, 0), 0.03f,
                     new Color(0.08f, 0.1f, 0.15f));
        FlatText(numbers, "cm", new Vector3(rulerLength * 0.5f, yTop + 0.0001f, 0.02f), Quaternion.Euler(90, 0, 0), 0.025f,
                 new Color(0.3f, 0.3f, 0.35f));

        var col = root.gameObject.AddComponent<BoxCollider>();
        col.center = new Vector3(rulerLength * 0.5f, th * 0.5f, W * 0.5f);
        col.size = new Vector3(L, 0.004f, W);
        return root;
    }

    void BuildPencil(Transform board)
    {
        var root = new GameObject("Pencil").transform;
        root.SetParent(transform, false);
        root.localPosition = new Vector3(boardSize.x * 0.5f - 0.03f, boardSize.y + 0.0035f, 0.02f);
        root.localRotation = Quaternion.Euler(0, 90, 0);          // lying along the board's right edge

        float len = 0.15f, r = 0.0035f;
        var body = new GameObject("Body");
        body.transform.SetParent(root, false);
        body.AddComponent<MeshFilter>().sharedMesh = Track(HexPrismMesh(r, len));
        body.AddComponent<MeshRenderer>().sharedMaterial = Mat(pencilColor, 0.5f, 0f);

        // Cone tips point along +X (sharpened end); rotating +90 about Z turns the cone's base towards the body
        var wood = Cone("Wood", root, r * 0.95f, 0.016f, new Vector3(len * 0.5f + 0.016f, 0, 0), Mat(new Color32(230, 190, 140, 255), 0.2f, 0f));
        wood.transform.localRotation = Quaternion.Euler(0, 0, 90);
        var lead = Cone("Lead", root, r * 0.32f, 0.0055f, new Vector3(len * 0.5f + 0.0175f, 0, 0), Mat(new Color(0.15f, 0.15f, 0.17f), 0.4f, 0.3f));
        lead.transform.localRotation = Quaternion.Euler(0, 0, 90);
        var ferrule = Cylinder("Ferrule", root, r * 1.02f, 0.008f, new Vector3(-len * 0.5f - 0.004f, 0, 0), Mat(new Color(0.78f, 0.78f, 0.8f), 0.8f, 0.9f));
        ferrule.transform.localRotation = Quaternion.Euler(0, 0, 90);
        var eraser = Cylinder("Eraser", root, r, 0.008f, new Vector3(-len * 0.5f - 0.012f, 0, 0), Mat(new Color32(240, 120, 140, 255), 0.1f, 0f));
        eraser.transform.localRotation = Quaternion.Euler(0, 0, 90);

        var col = root.gameObject.AddComponent<BoxCollider>();
        col.size = new Vector3(len + 0.03f, 0.008f, 0.008f);
    }

    // =========================== helpers =================================

    Transform Group(string name)
    {
        var g = new GameObject(name).transform;
        g.SetParent(transform, false);
        return g;
    }

    GameObject Box(string name, Transform parent, Vector3 size, Vector3 localPos, Material m)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.AddComponent<MeshFilter>().sharedMesh = Track(BoxMesh(size));
        go.AddComponent<MeshRenderer>().sharedMaterial = m;
        return go;
    }

    GameObject Cylinder(string name, Transform parent, float radius, float height, Vector3 localPos, Material m)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.AddComponent<MeshFilter>().sharedMesh = Track(CylinderMesh(radius, height, 20));
        go.AddComponent<MeshRenderer>().sharedMaterial = m;
        return go;
    }

    GameObject Cone(string name, Transform parent, float radius, float height, Vector3 localPos, Material m)
    {
        // Points DOWN from localPos (tip at localPos, base at localPos + up*height)
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.AddComponent<MeshFilter>().sharedMesh = Track(ConeMesh(radius, height, 16));
        go.AddComponent<MeshRenderer>().sharedMaterial = m;
        return go;
    }

    GameObject Sphere(string name, Transform parent, float diameter, Vector3 localPos, Material m)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name;
        var c = go.GetComponent<Collider>();
        if (Application.isPlaying) Destroy(c); else DestroyImmediate(c);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = Vector3.one * diameter;
        go.GetComponent<MeshRenderer>().sharedMaterial = m;
        return go;
    }

    void FlatText(Transform parent, string text, Vector3 localPos, Quaternion rot, float size, Color color)
    {
        var go = new GameObject("T_" + text);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = rot;
        var t = go.AddComponent<TextMeshPro>();
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.rectTransform.sizeDelta = new Vector2(0.02f, 0.006f);
        t.fontStyle = FontStyles.Bold;
    }

    Material Mat(Color c, float smooth, float metal, bool transparent = false, Texture2D tex = null)
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Lit");
        if (!sh) sh = Shader.Find("Standard");
        var m = new Material(sh) { name = "Gen_" + ColorUtility.ToHtmlStringRGB(c) };
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        m.color = c;
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metal);
        if (tex)
        {
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
            m.mainTexture = tex;
        }
        // Keep emission available so the glow highlights work on Android
        m.EnableKeyword("_EMISSION");
        if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", Color.black);

        if (transparent)
        {
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            if (m.HasProperty("_SrcBlendAlpha")) m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            if (m.HasProperty("_DstBlendAlpha")) m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.SetShaderPassEnabled("DepthOnly", false);
            m.SetShaderPassEnabled("ShadowCaster", false);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
        }
        _generated.Add(m);
        return m;
    }

    Texture2D WoodTexture()
    {
        const int w = 256, h = 64;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, true) { name = "Gen_Wood", wrapMode = TextureWrapMode.Repeat };
        var px = new Color32[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float grain = Mathf.PerlinNoise(x * 0.02f, y * 0.35f) * 0.6f + Mathf.PerlinNoise(x * 0.12f, y * 1.4f) * 0.4f;
                float v = 0.82f + Mathf.Sin(grain * 18f) * 0.08f;
                byte b = (byte)(Mathf.Clamp01(v) * 255f);
                px[y * w + x] = new Color32(b, b, b, 255);
            }
        tex.SetPixels32(px);
        tex.Apply(true);
        _generated.Add(tex);
        return tex;
    }

    Mesh Track(Mesh m)
    {
        _generated.Add(m);
        return m;
    }

    // =========================== meshes ==================================

    static Mesh MakeMesh(string name, List<Vector3> v, List<int> t)
    {
        var m = new Mesh { name = name };
        if (v.Count > 65000) m.indexFormat = IndexFormat.UInt32;
        m.SetVertices(v);
        m.SetTriangles(t, 0);
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }

    /// <summary>A thin flat rectangle lying on the XZ plane, facing up. dir = its long direction.</summary>
    static void AddFlatQuad(List<Vector3> v, List<int> t, Vector3 centre, Vector3 dir, float length, float width)
    {
        dir.y = 0; dir.Normalize();
        Vector3 side = Vector3.Cross(Vector3.up, dir) * (width * 0.5f);
        Vector3 along = dir * (length * 0.5f);
        int b = v.Count;
        v.Add(centre - along - side); v.Add(centre - along + side); v.Add(centre + along + side); v.Add(centre + along - side);
        // Make sure it faces up
        Vector3 n = Vector3.Cross(v[b + 1] - v[b], v[b + 2] - v[b]);
        if (n.y >= 0) { t.Add(b); t.Add(b + 1); t.Add(b + 2); t.Add(b); t.Add(b + 2); t.Add(b + 3); }
        else { t.Add(b); t.Add(b + 2); t.Add(b + 1); t.Add(b); t.Add(b + 3); t.Add(b + 2); }
    }

    static Mesh BoxMesh(Vector3 s)
    {
        Vector3 h = s * 0.5f;
        var v = new List<Vector3>();
        var uv = new List<Vector2>();
        var t = new List<int>();
        void Face(Vector3 c, Vector3 u, Vector3 w)   // centre, half-axis u, half-axis w (u x w = outward)
        {
            int b = v.Count;
            v.Add(c - u - w); v.Add(c - u + w); v.Add(c + u + w); v.Add(c + u - w);
            uv.Add(new Vector2(0, 0)); uv.Add(new Vector2(0, 1)); uv.Add(new Vector2(1, 1)); uv.Add(new Vector2(1, 0));
            t.Add(b); t.Add(b + 1); t.Add(b + 2); t.Add(b); t.Add(b + 2); t.Add(b + 3);
        }
        Face(new Vector3(0, h.y, 0), new Vector3(h.x, 0, 0), new Vector3(0, 0, h.z));     // top
        Face(new Vector3(0, -h.y, 0), new Vector3(0, 0, h.z), new Vector3(h.x, 0, 0));    // bottom
        Face(new Vector3(h.x, 0, 0), new Vector3(0, 0, h.z), new Vector3(0, h.y, 0));     // +x
        Face(new Vector3(-h.x, 0, 0), new Vector3(0, h.y, 0), new Vector3(0, 0, h.z));    // -x
        Face(new Vector3(0, 0, h.z), new Vector3(0, h.y, 0), new Vector3(h.x, 0, 0));     // +z
        Face(new Vector3(0, 0, -h.z), new Vector3(h.x, 0, 0), new Vector3(0, h.y, 0));    // -z
        var m = new Mesh { name = "Gen_Box" };
        m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(t, 0);
        m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }

    static Mesh CylinderMesh(float r, float hgt, int seg)
    {
        var v = new List<Vector3>();
        var t = new List<int>();
        float h = hgt * 0.5f;
        for (int i = 0; i < seg; i++)
        {
            float a = i / (float)seg * Mathf.PI * 2f;
            Vector3 d = new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r);
            v.Add(d + Vector3.up * h); v.Add(d - Vector3.up * h);
        }
        int top = v.Count; v.Add(Vector3.up * h);
        int bot = v.Count; v.Add(-Vector3.up * h);
        for (int i = 0; i < seg; i++)
        {
            int a0 = i * 2, b0 = a0 + 1, a1 = ((i + 1) % seg) * 2, b1 = a1 + 1;
            t.Add(a0); t.Add(a1); t.Add(b0);
            t.Add(a1); t.Add(b1); t.Add(b0);
            t.Add(top); t.Add(a1); t.Add(a0);
            t.Add(bot); t.Add(b0); t.Add(b1);
        }
        return FixOutward(MakeMesh("Gen_Cylinder", v, t), Vector3.zero);
    }

    static Mesh ConeMesh(float r, float hgt, int seg)
    {
        var v = new List<Vector3> { Vector3.zero };            // tip (bottom)
        var t = new List<int>();
        for (int i = 0; i < seg; i++)
        {
            float a = i / (float)seg * Mathf.PI * 2f;
            v.Add(new Vector3(Mathf.Cos(a) * r, hgt, Mathf.Sin(a) * r));
        }
        int cap = v.Count; v.Add(Vector3.up * hgt);
        for (int i = 0; i < seg; i++)
        {
            int a = 1 + i, b = 1 + (i + 1) % seg;
            t.Add(0); t.Add(a); t.Add(b);
            t.Add(cap); t.Add(b); t.Add(a);
        }
        return FixOutward(MakeMesh("Gen_Cone", v, t), Vector3.up * hgt * 0.6f);
    }

    static Mesh HexPrismMesh(float r, float len)
    {
        // Long axis along X
        var v = new List<Vector3>();
        var t = new List<int>();
        float h = len * 0.5f;
        for (int i = 0; i < 6; i++)
        {
            float a = i / 6f * Mathf.PI * 2f;
            Vector3 d = new Vector3(0, Mathf.Cos(a) * r, Mathf.Sin(a) * r);
            v.Add(d + Vector3.right * h); v.Add(d - Vector3.right * h);
        }
        int e1 = v.Count; v.Add(Vector3.right * h);
        int e2 = v.Count; v.Add(-Vector3.right * h);
        for (int i = 0; i < 6; i++)
        {
            int a0 = i * 2, b0 = a0 + 1, a1 = ((i + 1) % 6) * 2, b1 = a1 + 1;
            t.Add(a0); t.Add(b0); t.Add(a1);
            t.Add(a1); t.Add(b0); t.Add(b1);
            t.Add(e1); t.Add(a0); t.Add(a1);
            t.Add(e2); t.Add(b1); t.Add(b0);
        }
        return FixOutward(MakeMesh("Gen_Hex", v, t), Vector3.zero);
    }

    static Mesh SemicircleMesh(float R, float th, int seg)
    {
        var v = new List<Vector3>();
        var t = new List<int>();
        // top and bottom fans
        int cTop = v.Count; v.Add(new Vector3(0, th, 0));
        int cBot = v.Count; v.Add(Vector3.zero);
        int start = v.Count;
        for (int i = 0; i <= seg; i++)
        {
            float a = i / (float)seg * Mathf.PI;
            Vector3 d = new Vector3(Mathf.Cos(a) * R, 0, Mathf.Sin(a) * R);
            v.Add(d + Vector3.up * th); v.Add(d);
        }
        for (int i = 0; i < seg; i++)
        {
            int a0 = start + i * 2, b0 = a0 + 1, a1 = a0 + 2, b1 = a0 + 3;
            t.Add(cTop); t.Add(a1); t.Add(a0);          // top
            t.Add(cBot); t.Add(b0); t.Add(b1);          // bottom
            t.Add(a0); t.Add(a1); t.Add(b0);            // curved rim
            t.Add(a1); t.Add(b1); t.Add(b0);
        }
        // straight edge
        int r0 = start, r0b = start + 1, l0 = start + seg * 2, l0b = l0 + 1;
        t.Add(r0); t.Add(r0b); t.Add(l0);
        t.Add(l0); t.Add(r0b); t.Add(l0b);
        return FixOutward(MakeMesh("Gen_Protractor", v, t), new Vector3(0, th * 0.5f, R * 0.4f));
    }

    /// <summary>Flips triangles that face inwards (towards 'inside'), so every surface is visible from outside.</summary>
    static Mesh FixOutward(Mesh m, Vector3 inside)
    {
        var v = m.vertices;
        var t = m.triangles;
        for (int k = 0; k < t.Length; k += 3)
        {
            Vector3 a = v[t[k]], b = v[t[k + 1]], c = v[t[k + 2]];
            Vector3 n = Vector3.Cross(b - a, c - a);
            Vector3 centre = (a + b + c) / 3f;
            if (Vector3.Dot(n, centre - inside) < 0f) { int tmp = t[k + 1]; t[k + 1] = t[k + 2]; t[k + 2] = tmp; }
        }
        m.triangles = t;
        m.RecalculateNormals();
        return m;
    }

    // =========================== save as prefab ==========================

#if UNITY_EDITOR
    [ContextMenu("Save as prefab")]
    public void SaveAsPrefab()
    {
        if (transform.childCount == 0) Build();
        const string folder = "Assets/Generated";
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets", "Generated");
        string dataPath = folder + "/ReflectionApparatus_Data.asset";
        string prefabPath = folder + "/ReflectionApparatus.prefab";

        // Put every generated mesh, material and texture into one asset file so the prefab can reference them
        var container = ScriptableObject.CreateInstance<ScriptableObject>();
        AssetDatabase.CreateAsset(container, dataPath);
        var saved = new HashSet<Object>();
        void Save(Object o)
        {
            if (!o || saved.Contains(o) || EditorUtility.IsPersistent(o)) return;
            saved.Add(o);
            AssetDatabase.AddObjectToAsset(o, container);
        }
        foreach (var mf in GetComponentsInChildren<MeshFilter>(true)) Save(mf.sharedMesh);
        foreach (var r in GetComponentsInChildren<Renderer>(true))
            foreach (var m in r.sharedMaterials)
            {
                if (!m || EditorUtility.IsPersistent(m)) continue;
                if (m.mainTexture) Save(m.mainTexture);
                Save(m);
            }
        AssetDatabase.SaveAssets();
        PrefabUtility.SaveAsPrefabAssetAndConnect(gameObject, prefabPath, InteractionMode.UserAction);
        AssetDatabase.Refresh();
        Debug.Log("[Reflection] Saved prefab to " + prefabPath);
    }
#endif
}