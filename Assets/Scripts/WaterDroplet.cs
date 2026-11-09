using System.Collections;
using UnityEngine;

/// <summary>
/// A single water drop. Created automatically by DropperController (you don't need to set this up).
/// Hangs at the dropper tip until released, falls, splats on the powder and tells it a drop arrived.
/// </summary>
public class WaterDroplet : MonoBehaviour
{
    [HideInInspector] public CopperSulfateSample target;
    [HideInInspector] public AudioClip dripClip;
    public float lifetime = 3f;

    bool _released, _done, _cancelled;

    public void Release()
    {
        _released = true;
        var s = transform.localScale;
        transform.localScale = new Vector3(s.x * 0.9f, s.y * 1.25f, s.z * 0.9f);   // teardrop stretch
        Destroy(gameObject, lifetime);
    }

    /// <summary>Remove without counting (used by Restart).</summary>
    public void Cancel()
    {
        _cancelled = true;
        Destroy(gameObject);
    }

    void OnCollisionEnter(Collision c)
    {
        if (!_released || _done) return;
        _done = true;

        var sample = c.collider.GetComponentInParent<CopperSulfateSample>();
        if (sample == null) sample = target;      // landed on the dish rim: still count it
        if (sample != null) sample.ReceiveDrop();

        if (dripClip) AudioSource.PlayClipAtPoint(dripClip, transform.position, 0.6f);
        StartCoroutine(Splat());
    }

    IEnumerator Splat()
    {
        var rb = GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = true;
        var col = GetComponent<Collider>();
        if (col) col.enabled = false;

        Vector3 start = transform.localScale;
        Vector3 flat = new Vector3(start.x * 1.8f, start.y * 0.15f, start.z * 1.8f);
        float t = 0f;
        while (t < 0.18f)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(start, flat, t / 0.18f);
            yield return null;
        }
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        // If it fell without hitting anything (missed the collider), still count the drop
        if (_released && !_done && !_cancelled && target != null && target.isActiveAndEnabled)
            target.ReceiveDrop();
    }
}
