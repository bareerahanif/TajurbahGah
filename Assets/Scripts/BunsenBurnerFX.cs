using System.Collections;
using UnityEngine;

public class BunsenBurnerFX : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private ParticleSystem flameParticles;
    [SerializeField] private Light flameLight;

    [Header("Base Non-Luminous Flame (Clean Gas)")]
    [ColorUsage(true, true)]
    [SerializeField] private Color baseFlameColor = new Color(0.15f, 0.45f, 1.0f, 1.0f); // Bright blue
    [SerializeField] private float baseLightIntensity = 0.8f;

    [Header("Transition Settings")]
    [SerializeField] private float colorTransitionSpeed = 6f;

    private Coroutine colorLerpRoutine;

    private void Start()
    {
        if (flameParticles == null) flameParticles = GetComponent<ParticleSystem>();
        if (flameLight == null) flameLight = GetComponentInChildren<Light>();

        ResetToBaseFlame();
    }

    public void SetFlameForRadical(RadicalType radical)
    {
        Color targetColor = GetRadicalFlameColor(radical);
        float targetIntensity = (radical == RadicalType.None) ? baseLightIntensity : 2.2f;

        if (colorLerpRoutine != null) StopCoroutine(colorLerpRoutine);
        colorLerpRoutine = StartCoroutine(TransitionFlame(targetColor, targetIntensity));
    }

    public void ResetToBaseFlame()
    {
        if (colorLerpRoutine != null) StopCoroutine(colorLerpRoutine);
        colorLerpRoutine = StartCoroutine(TransitionFlame(baseFlameColor, baseLightIntensity));
    }

    private Color GetRadicalFlameColor(RadicalType radical)
    {
        switch (radical)
        {
            case RadicalType.Sodium:     // Intense persistent golden-yellow
                return new Color(1.0f, 0.75f, 0.05f, 1.0f);

            case RadicalType.Potassium:  // Delicate lilac/violet
                return new Color(0.85f, 0.5f, 1.0f, 1.0f);

            case RadicalType.Calcium:    // Brick red / orange-red
                return new Color(1.0f, 0.32f, 0.08f, 1.0f);

            case RadicalType.Strontium:  // Vivid crimson / deep carmine
                return new Color(1.0f, 0.08f, 0.15f, 1.0f);

            case RadicalType.Barium:     // Apple pale green
                return new Color(0.65f, 0.95f, 0.2f, 1.0f);

            case RadicalType.Copper:     // Vibrant emerald green / blue-green
                return new Color(0.1f, 0.95f, 0.65f, 1.0f);

            case RadicalType.Contaminated: // Dirty faint orange flicker
                return new Color(0.9f, 0.45f, 0.1f, 1.0f);

            default:                     // Pure blue base gas flame
                return baseFlameColor;
        }
    }

    private IEnumerator TransitionFlame(Color targetColor, float targetLightIntensity)
    {
        var main = flameParticles.main;
        Color startColor = main.startColor.color;
        float startIntensity = (flameLight != null) ? flameLight.intensity : targetLightIntensity;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * colorTransitionSpeed;
            Color currentColor = Color.Lerp(startColor, targetColor, t);

            main.startColor = new ParticleSystem.MinMaxGradient(currentColor);

            if (flameLight != null)
            {
                flameLight.color = currentColor;
                flameLight.intensity = Mathf.Lerp(startIntensity, targetLightIntensity, t);
            }

            yield return null;
        }

        main.startColor = new ParticleSystem.MinMaxGradient(targetColor);
        if (flameLight != null)
        {
            flameLight.color = targetColor;
            flameLight.intensity = targetLightIntensity;
        }
    }
}