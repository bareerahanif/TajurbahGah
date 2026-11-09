using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Turns this object into a pile of powder and places it in the dish:
///  - a heap with fine grainy bumps (lots of small facets catch the light like powder)
///  - a speckled grain texture (generated in code)
///  - loose grains scattered over the heap and around its edge
/// Put it on CopperSulfateSample, drag ChinaDish into "Dish", then click the three dots > "Build powder pile".
/// It always builds upright and at real size, whatever rotation or scale the object has.
/// </summary>
[RequireComponent(typeof(MeshFilter))]
public class PowderPile : MonoBehaviour
{
    [Tooltip("The china dish. The pile is centred in it and dropped onto its inner surface.")]
    public Transform dish;

    [Header("Heap shape (metres)")]
    public float radius = 0.02f;
    public float height = 0.014f;
    [Range(0f, 1f)] public float roughness = 0.35f;
    [Tooltip("How far the outer edge curves down, so it sinks into the curved dish")]
    public float edgeSink = 0.008f;
    [Tooltip("Raise or lower the pile after it is dropped into the dish")]
    public float heightOffset = 0.001f;

    [Header("Powder look")]
    [Tooltip("Fine bumpiness of the surface. Higher looks grainier.")]
    [Range(0f, 1f)] public float surfaceGrain = 0.6f;
    [Tooltip("Loose grains on top of the heap")]
    public int grainsOnHeap = 140;
    [Tooltip("Loose grains scattered on the dish around the heap")]
    public int grainsAround = 45;
    [Tooltip("Size of one grain in metres")]
    public float grainSize = 0.0018f;
    [Tooltip("Adds a speckled grain texture to the powder")]
    public bool speckleTexture = true;
    public float speckleTiling = 3f;

    [Header("Detail")]
    public int rings = 30;
    public int segments = 96;
    public int seed = 7;
    public bool buildOnPlay = true;

    float _n;

    void Awake()
    {
        if (buildOnPlay && Application.isPlaying) Build();
    }

    [ContextMenu("Build powder pile")]
    public void Build()
    {
        rings = Mathf.Max(3, rings);
        segments = Mathf.Max(8, segments);
        _n = seed * 13.37f;

        // Remove old colliders and old grains first, so the dish raycast doesn't hit them
        foreach (var c in GetComponents<Collider>())
        {
            if (Application.isPlaying) Destroy(c); else DestroyImmediate(c);
        }
        var oldGrains = transform.Find("Grains");
        if (oldGrains) { if (Application.isPlaying) Destroy(oldGrains.gameObject); else DestroyImmediate(oldGrains.gameObject); }

        PlaceInDish();
        BuildHeap();
        BuildGrains();
        if (speckleTexture) ApplySpeckle();
    }

    // ------------------------------------------------------------------ heap

    /// <summary>A point on the heap surface, in world-space offsets from the pivot. t: 0 centre to 1 edge, a: angle.</summary>
    Vector3 SurfacePoint(float t, float a)
    {
        float ca = Mathf.Cos(a), sa = Mathf.Sin(a);
        float edgeNoise = Mathf.PerlinNoise(_n + ca * 1.5f + 3f, _n + sa * 1.5f + 3f) - 0.5f;
        float r = radius * t * (1f + edgeNoise * 0.35f * roughness * t);
        float x = ca * r, z = sa * r;

        float h = height * Mathf.Pow(Mathf.Max(0f, 1f - t * t), 0.7f);
        float bump = Mathf.PerlinNoise(_n + x / radius * 4f, _n + z / radius * 4f) - 0.5f;
        float lumps = Mathf.PerlinNoise(_n + 50f + x / radius * 10f, _n + 50f + z / radius * 10f) - 0.5f;
        h += (bump * 0.45f + lumps * 0.2f) * height * roughness * (1f - t * 0.8f);

        // Fine grain: high-frequency bumps so the surface sparkles and looks granular
        float g1 = Mathf.PerlinNoise(_n + 100f + x * 900f, _n + 100f + z * 900f) - 0.5f;
        float g2 = Mathf.PerlinNoise(_n + 200f + x * 2300f, _n + 200f + z * 2300f) - 0.5f;
        h += (g1 * 0.7f + g2 * 0.5f) * 0.0012f * surfaceGrain * (1f - t * 0.5f);

        h = Mathf.Max(0f, h);
        if (t >= 0.999f) h = 0f;
        h -= edgeSink * t * t;
        return new Vector3(x, h, z);
    }

    void BuildHeap()
    {
        int vertCount = 1 + rings * segments;
        var verts = new Vector3[vertCount];
        var uvs = new Vector2[vertCount];

        verts[0] = ToLocal(SurfacePoint(0f, 0f));
        uvs[0] = new Vector2(0.5f, 0.5f);
        for (int i = 1; i <= rings; i++)
        {
            float t = (float)i / rings;
            for (int j = 0; j < segments; j++)
            {
                float a = (float)j / segments * Mathf.PI * 2f;
                Vector3 p = SurfacePoint(t, a);
                int idx = 1 + (i - 1) * segments + j;
                verts[idx] = ToLocal(p);
                uvs[idx] = new Vector2(p.x / radius * 0.5f + 0.5f, p.z / radius * 0.5f + 0.5f);
            }
        }

        var tris = new List<int>();
        for (int j = 0; j < segments; j++)
        {
            int a = 1 + j, b = 1 + (j + 1) % segments;
            tris.Add(0); tris.Add(b); tris.Add(a);
        }
        for (int i = 1; i < rings; i++)
        {
            int inner = 1 + (i - 1) * segments;
            int outer = 1 + i * segments;
            for (int j = 0; j < segments; j++)
            {
                int j2 = (j + 1) % segments;
                tris.Add(inner + j); tris.Add(inner + j2); tris.Add(outer + j);
                tris.Add(inner + j2); tris.Add(outer + j2); tris.Add(outer + j);
            }
        }
        FixMirroring(tris);

        var mesh = new Mesh { name = "PowderPile" };
        mesh.vertices = verts;
        mesh.uv = uvs;
        mesh.triangles = tris.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        GetComponent<MeshFilter>().sharedMesh = mesh;

        var mc = gameObject.AddComponent<MeshCollider>();
        mc.sharedMesh = mesh;
        mc.convex = true;
        mc.isTrigger = false;
    }

    // ---------------------------------------------------------------- grains

    void BuildGrains()
    {
        if (grainsOnHeap <= 0 && grainsAround <= 0) return;
        var rnd = new System.Random(seed);
        var verts = new List<Vector3>();
        var tris = new List<int>();

        // On the heap
        for (int k = 0; k < grainsOnHeap; k++)
        {
            float t = Mathf.Sqrt((float)rnd.NextDouble()) * 0.92f;          // spread evenly over the area
            float a = (float)rnd.NextDouble() * Mathf.PI * 2f;
            Vector3 p = SurfacePoint(t, a);
            float s = grainSize * Mathf.Lerp(0.6f, 1.4f, (float)rnd.NextDouble());
            AddGrain(verts, tris, p + Vector3.up * s * 0.15f, s, rnd);
        }

        // Around the heap, on the dish surface
        if (grainsAround > 0)
        {
            Physics.SyncTransforms();
            for (int k = 0; k < grainsAround; k++)
            {
                float a = (float)rnd.NextDouble() * Mathf.PI * 2f;
                float r = radius * Mathf.Lerp(0.95f, 1.6f, Mathf.Pow((float)rnd.NextDouble(), 1.8f));
                Vector3 offset = new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                if (!DishHeightAt(transform.position + offset, out float worldY)) continue;
                offset.y = worldY - transform.position.y;
                float s = grainSize * Mathf.Lerp(0.5f, 1.2f, (float)rnd.NextDouble());
                AddGrain(verts, tris, offset + Vector3.up * s * 0.2f, s, rnd);
            }
        }

        FixMirroring(tris);
        var mesh = new Mesh { name = "PowderGrains" };
        if (verts.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        var go = new GameObject("Grains");
        go.transform.SetParent(transform, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>();
        var myRenderer = GetComponent<Renderer>();
        if (myRenderer) mr.sharedMaterials = myRenderer.sharedMaterials;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    // A small irregular 8-sided grain (flat-shaded, so it glints like a crystal)
    void AddGrain(List<Vector3> verts, List<int> tris, Vector3 centreWorld, float size, System.Random rnd)
    {
        Vector3[] dirs = { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };
        Quaternion rot = Quaternion.Euler((float)rnd.NextDouble() * 360f, (float)rnd.NextDouble() * 360f, (float)rnd.NextDouble() * 360f);
        var p = new Vector3[6];
        for (int i = 0; i < 6; i++)
            p[i] = ToLocal(centreWorld + rot * dirs[i] * size * 0.5f * Mathf.Lerp(0.6f, 1.2f, (float)rnd.NextDouble()));

        int[,] faces = { { 0, 4, 3 }, { 0, 3, 5 }, { 0, 5, 2 }, { 0, 2, 4 }, { 1, 3, 4 }, { 1, 5, 3 }, { 1, 2, 5 }, { 1, 4, 2 } };
        for (int f = 0; f < 8; f++)
        {
            int b = verts.Count;
            verts.Add(p[faces[f, 0]]); verts.Add(p[faces[f, 1]]); verts.Add(p[faces[f, 2]]);
            tris.Add(b); tris.Add(b + 1); tris.Add(b + 2);
        }
    }

    // --------------------------------------------------------------- texture

    void ApplySpeckle()
    {
        const int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, true) { name = "PowderSpeckle", wrapMode = TextureWrapMode.Repeat };
        var rnd = new System.Random(seed + 1);
        var px = new Color32[size * size];
        for (int i = 0; i < px.Length; i++)
        {
            float v = 0.86f + (float)rnd.NextDouble() * 0.14f;            // fine grain
            if (rnd.NextDouble() < 0.04) v -= 0.12f;                        // a few darker specks
            byte b = (byte)(Mathf.Clamp01(v) * 255f);
            px[i] = new Color32(b, b, b, 255);
        }
        tex.SetPixels32(px);
        tex.Apply(true);

        foreach (var r in GetComponentsInChildren<Renderer>())
        {
            var mpb = new MaterialPropertyBlock();
            r.GetPropertyBlock(mpb);
            mpb.SetTexture("_BaseMap", tex);
            mpb.SetTexture("_MainTex", tex);
            mpb.SetVector("_BaseMap_ST", new Vector4(speckleTiling, speckleTiling, 0f, 0f));
            r.SetPropertyBlock(mpb);
        }
    }

    // --------------------------------------------------------------- helpers

    Vector3 ToLocal(Vector3 worldOffset) => transform.InverseTransformVector(worldOffset);

    void FixMirroring(List<int> tris)
    {
        Vector3 ls = transform.lossyScale;
        if (ls.x * ls.y * ls.z < 0f)
            for (int k = 0; k < tris.Count; k += 3) { int tmp = tris[k + 1]; tris[k + 1] = tris[k + 2]; tris[k + 2] = tmp; }
    }

    bool DishHeightAt(Vector3 worldPos, out float y)
    {
        y = 0f;
        if (!dish) return false;
        Vector3 start = new Vector3(worldPos.x, worldPos.y + 0.3f, worldPos.z);
        float best = float.MaxValue;
        bool found = false;
        foreach (var h in Physics.RaycastAll(start, Vector3.down, 1f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (h.transform == transform || h.transform.IsChildOf(transform)) continue;
            if (h.transform != dish && !h.transform.IsChildOf(dish)) continue;
            if (h.distance < best) { best = h.distance; y = h.point.y; found = true; }
        }
        return found;
    }

    void PlaceInDish()
    {
        if (!dish) return;
        Vector3 centre = dish.position;
        var dr = dish.GetComponentInChildren<Renderer>();
        if (dr) centre = dr.bounds.center;

        Physics.SyncTransforms();
        if (DishHeightAt(centre, out float y))
            transform.position = new Vector3(centre.x, y + heightOffset + edgeSink * 0.5f, centre.z);
        else if (dr)
            transform.position = new Vector3(centre.x, dr.bounds.min.y + edgeSink + heightOffset + 0.004f, centre.z);
        else
            Debug.LogWarning("PowderPile: couldn't find the dish surface. Move the pile into the dish by hand.");
    }
}