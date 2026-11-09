using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Put this on the anhydrous CuSO4 powder (CopperSulfateSample).
/// Fades the powder from white (anhydrous) to blue (hydrated), one step per drop of water.
/// Works with URP/Lit materials (_BaseColor). Falls back to _Color for other shaders.
/// </summary>
public class CopperSulfateSample : MonoBehaviour
{
    [Header("Colours")]
    [Tooltip("Anhydrous copper(II) sulfate: off-white")]
    public Color anhydrousColor = new Color(0.93f, 0.93f, 0.90f);
    [Tooltip("Hydrated copper(II) sulfate: bright blue")]
    public Color hydratedColor = new Color(0.10f, 0.42f, 0.85f);

    [Header("Behaviour")]
    [Tooltip("How many drops make it fully blue. Keep the same as the dropper's Drop Count.")]
    public int dropsToFullyHydrate = 3;
    [Tooltip("Seconds each drop's colour change takes")]
    public float fadeTimePerDrop = 1.2f;

    /// <summary>Fires once, after the last fade has completely finished.</summary>
    public event Action OnFullyHydrated;

    public bool IsHydrated => _dropsReceived >= dropsToFullyHydrate;

    Renderer _renderer;
    MaterialPropertyBlock _mpb;
    int _colorId;
    int _dropsReceived;
    float _progress;
    Coroutine _fade;

    void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _mpb = new MaterialPropertyBlock();
        _colorId = _renderer.sharedMaterial != null && _renderer.sharedMaterial.HasProperty("_BaseColor")
            ? Shader.PropertyToID("_BaseColor")
            : Shader.PropertyToID("_Color");
        ResetSample();
    }

    [ContextMenu("Reset sample")]
    public void ResetSample()
    {
        if (_fade != null) { StopCoroutine(_fade); _fade = null; }
        _dropsReceived = 0;
        _progress = 0f;
        if (_renderer) Apply(0f);
    }

    [ContextMenu("Test one drop")]
    public void ReceiveDrop()
    {
        if (IsHydrated) return;
        _dropsReceived++;
        float target = Mathf.Clamp01((float)_dropsReceived / dropsToFullyHydrate);
        if (_fade != null) StopCoroutine(_fade);
        _fade = StartCoroutine(FadeTo(target));
    }

    IEnumerator FadeTo(float target)
    {
        float start = _progress;
        float t = 0f;
        while (t < fadeTimePerDrop)
        {
            t += Time.deltaTime;
            _progress = Mathf.Lerp(start, target, Mathf.SmoothStep(0f, 1f, t / fadeTimePerDrop));
            Apply(_progress);
            yield return null;
        }
        _progress = target;
        Apply(_progress);
        _fade = null;

        if (IsHydrated) OnFullyHydrated?.Invoke();
    }

    void Apply(float p)
    {
        // All renderers, including the loose "Grains" made by PowderPile, change colour together
        Color c = Color.Lerp(anhydrousColor, hydratedColor, p);
        foreach (var r in GetComponentsInChildren<Renderer>())
        {
            r.GetPropertyBlock(_mpb);
            _mpb.SetColor(_colorId, c);
            r.SetPropertyBlock(_mpb);
        }
    }
}