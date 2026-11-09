using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Animates the dropper: picks it up and stands it upright, dips it into the beaker and fills it,
/// carries it over the china dish and forms drops at the tip that fall onto the powder, then puts it back.
///
/// Minimal setup: put it on WaterDropper, drag "Beaker liquid" into Water Liquid and CopperSulfateSample into Sample.
/// Everything else (tip position, waypoints, drops) is worked out automatically.
/// If the dropper turns the wrong way up when picked up, tick "Flip Tip".
/// </summary>
public class DropperController : MonoBehaviour
{
    [Header("Required")]
    [Tooltip("The water inside the beaker (your 'Beaker liquid' object)")]
    public Renderer waterLiquid;
    [Tooltip("The CuSO4 powder (CopperSulfateSample)")]
    public CopperSulfateSample sample;

    [Header("Tip")]
    [Tooltip("Leave empty: the tip is found automatically at one end of the dropper")]
    public Transform tip;
    [Tooltip("Tick this if the dropper ends up with the rubber bulb pointing DOWN")]
    public bool flipTip;

    [Header("Optional parts")]
    [Tooltip("The rubber bulb, if it is a separate object. It gets squeezed.")]
    public Transform bulb;
    [Tooltip("Material for the drops. Leave empty to use the beaker water's material.")]
    public Material dropletMaterial;
    [Tooltip("Drop diameter in metres")]
    public float dropletSize = 0.008f;

    [Header("Motion")]
    public float liftHeight = 0.12f;
    public float moveTime = 0.9f;
    [Tooltip("Height above the powder where the drops are released (metres)")]
    public float dropHeight = 0.06f;
    [Tooltip("Keep the same as the sample's Drops To Fully Hydrate")]
    public int dropCount = 3;
    public float timeBetweenDrops = 0.6f;
    public float dropGrowTime = 0.35f;

    [Header("Audio (optional)")]
    public AudioSource audioSource;
    public AudioClip suckClip;
    public AudioClip dripClip;

    public bool HasWater { get; private set; }
    public bool IsHeld { get; private set; }
    public bool IsBusy { get; private set; }

    Vector3 _homePos;
    Quaternion _homeRot;
    Quaternion _uprightRot;
    Vector3 _bulbScale;
    Collider[] _myColliders;
    readonly List<WaterDroplet> _liveDrops = new List<WaterDroplet>();

    void Awake()
    {
        _homePos = transform.position;
        _homeRot = transform.rotation;
        _myColliders = GetComponentsInChildren<Collider>();
        if (bulb) _bulbScale = bulb.localScale;

        Vector3 centreLocal;
        if (!tip) tip = CreateAutoTip(out centreLocal);
        else centreLocal = LocalBounds().center;

        // Rotation that makes the tip point straight down
        Vector3 tipDir = (tip.position - transform.TransformPoint(centreLocal)).normalized;
        _uprightRot = Quaternion.FromToRotation(tipDir, Vector3.down) * transform.rotation;
    }

    // ================= steps =================

    public IEnumerator PickUp()
    {
        if (IsHeld) yield break;
        IsBusy = true;
        yield return MoveTo(_homePos + Vector3.up * liftHeight, _uprightRot, moveTime);
        IsHeld = true;
        IsBusy = false;
    }

    public IEnumerator CollectWater()
    {
        if (!waterLiquid) { Debug.LogWarning("DropperController: Water Liquid is empty."); yield break; }
        if (!IsHeld) yield return PickUp();
        IsBusy = true;

        Bounds b = waterLiquid.bounds;
        Vector3 above = new Vector3(b.center.x, b.max.y + liftHeight * 0.6f, b.center.z);
        Vector3 dip = new Vector3(b.center.x, Mathf.Max(b.min.y + 0.01f, b.max.y - 0.025f), b.center.z);

        yield return MoveTip(above, moveTime, 0.04f);
        yield return MoveTip(dip, moveTime * 0.6f, 0f);
        yield return Squeeze(0.3f);
        Play(suckClip);
        yield return new WaitForSeconds(0.35f);
        yield return MoveTip(above, moveTime * 0.6f, 0f);

        HasWater = true;
        IsBusy = false;
    }

    public IEnumerator AddDrops()
    {
        if (!HasWater || !sample) yield break;
        IsBusy = true;

        Bounds sb = SampleBounds();
        Vector3 release = new Vector3(sb.center.x, sb.max.y + dropHeight, sb.center.z);

        yield return MoveTip(release + Vector3.up * 0.04f, moveTime, 0.05f);
        yield return MoveTip(release, moveTime * 0.4f, 0f);

        for (int i = 0; i < dropCount; i++)
        {
            yield return FormAndReleaseDrop();
            yield return new WaitForSeconds(timeBetweenDrops);
        }

        HasWater = false;
        yield return new WaitForSeconds(0.3f);
        yield return ReturnHome();
        IsBusy = false;
    }

    public IEnumerator ReturnHome()
    {
        IsBusy = true;
        yield return MoveTo(_homePos + Vector3.up * liftHeight, _uprightRot, moveTime, 0.03f);
        yield return MoveTo(_homePos + Vector3.up * liftHeight * 0.5f, _homeRot, moveTime * 0.6f);
        yield return MoveTo(_homePos, _homeRot, moveTime * 0.4f);
        IsHeld = false;
        IsBusy = false;
    }

    /// <summary>Instantly put everything back to the start (used by Restart).</summary>
    public void ResetImmediate()
    {
        StopAllCoroutines();
        transform.SetPositionAndRotation(_homePos, _homeRot);
        if (bulb) bulb.localScale = _bulbScale;
        foreach (var d in _liveDrops) if (d) d.Cancel();
        _liveDrops.Clear();
        HasWater = false;
        IsHeld = false;
        IsBusy = false;
    }

    // ================= drops =================

    IEnumerator FormAndReleaseDrop()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "WaterDroplet";
        var rend = go.GetComponent<Renderer>();
        Material mat = dropletMaterial ? dropletMaterial : (waterLiquid ? waterLiquid.sharedMaterial : null);
        if (mat) rend.sharedMaterial = mat;
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        var col = go.GetComponent<Collider>();
        foreach (var c in _myColliders) if (c) Physics.IgnoreCollision(col, c);

        var rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.mass = 0.01f;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        var drop = go.AddComponent<WaterDroplet>();
        drop.target = sample;
        drop.dripClip = dripClip;
        _liveDrops.Add(drop);

        // Grow at the tip while the bulb is squeezed
        if (bulb) StartCoroutine(Squeeze(dropGrowTime + 0.1f));
        float t = 0f;
        while (t < dropGrowTime)
        {
            t += Time.deltaTime;
            float size = dropletSize * Mathf.SmoothStep(0.2f, 1f, t / dropGrowTime);
            go.transform.localScale = Vector3.one * size;
            go.transform.position = tip.position + Vector3.down * size * 0.5f;
            yield return null;
        }

        rb.isKinematic = false;
        rb.linearVelocity = Vector3.down * 0.15f;
        drop.Release();
    }

    // ================= motion helpers =================

    IEnumerator MoveTip(Vector3 tipTarget, float time, float arc)
    {
        Vector3 offset = transform.position - tip.position;   // valid while upright
        yield return MoveTo(tipTarget + offset, _uprightRot, time, arc);
    }

    IEnumerator MoveTo(Vector3 pos, Quaternion rot, float time, float arc = 0f)
    {
        Vector3 p0 = transform.position;
        Quaternion r0 = transform.rotation;
        yield return Animate(time, t =>
        {
            float e = Mathf.SmoothStep(0f, 1f, t);
            Vector3 p = Vector3.Lerp(p0, pos, e) + Vector3.up * Mathf.Sin(t * Mathf.PI) * arc;
            transform.SetPositionAndRotation(p, Quaternion.Slerp(r0, rot, e));
        });
    }

    IEnumerator Squeeze(float time)
    {
        if (!bulb) yield break;
        Vector3 squeezed = new Vector3(_bulbScale.x * 1.15f, _bulbScale.y * 0.75f, _bulbScale.z * 1.15f);
        yield return Animate(time * 0.5f, t => bulb.localScale = Vector3.Lerp(_bulbScale, squeezed, t));
        yield return Animate(time * 0.5f, t => bulb.localScale = Vector3.Lerp(squeezed, _bulbScale, t));
    }

    static IEnumerator Animate(float time, Action<float> step)
    {
        float t = 0f;
        while (t < time)
        {
            t += Time.deltaTime;
            step(Mathf.Clamp01(t / time));
            yield return null;
        }
        step(1f);
    }

    void Play(AudioClip clip)
    {
        if (audioSource && clip) audioSource.PlayOneShot(clip);
    }

    // ================= auto setup =================

    Bounds SampleBounds()
    {
        var r = sample.GetComponent<Renderer>();
        if (r) return r.bounds;
        return new Bounds(sample.transform.position, Vector3.one * 0.02f);
    }

    /// <summary>Bounds of all meshes in this object's local space.</summary>
    Bounds LocalBounds()
    {
        bool any = false;
        Bounds lb = new Bounds();
        foreach (var mf in GetComponentsInChildren<MeshFilter>())
        {
            if (!mf.sharedMesh) continue;
            Bounds mb = mf.sharedMesh.bounds;
            Vector3 mn = mb.min, mx = mb.max;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = new Vector3((i & 1) == 0 ? mn.x : mx.x, (i & 2) == 0 ? mn.y : mx.y, (i & 4) == 0 ? mn.z : mx.z);
                Vector3 local = transform.InverseTransformPoint(mf.transform.TransformPoint(corner));
                if (!any) { lb = new Bounds(local, Vector3.zero); any = true; }
                else lb.Encapsulate(local);
            }
        }
        if (!any) lb = new Bounds(Vector3.zero, Vector3.one * 0.1f);
        return lb;
    }

    Transform CreateAutoTip(out Vector3 centreLocal)
    {
        Bounds lb = LocalBounds();
        centreLocal = lb.center;

        // Find the longest axis in real-world length
        Vector3 ls = transform.lossyScale;
        Vector3 worldSize = new Vector3(lb.size.x * Mathf.Abs(ls.x), lb.size.y * Mathf.Abs(ls.y), lb.size.z * Mathf.Abs(ls.z));
        int axis = 0;
        if (worldSize.y > worldSize[axis]) axis = 1;
        if (worldSize.z > worldSize[axis]) axis = 2;

        float sign = flipTip ? 1f : -1f;
        Vector3 dir = Vector3.zero;
        dir[axis] = sign;

        // 1.2 cm past the end, so drops don't start inside the glass
        float extra = 0.012f / Mathf.Max(1e-5f, Mathf.Abs(ls[axis]));
        Vector3 tipLocal = lb.center + dir * (lb.extents[axis] + extra);

        var go = new GameObject("Tip (auto)");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = tipLocal;
        return go.transform;
    }

    void OnDrawGizmosSelected()
    {
        if (tip)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(tip.position, 0.006f);
        }
    }
}
