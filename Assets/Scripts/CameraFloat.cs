using UnityEngine;

public class CameraFloat : MonoBehaviour
{
    [Header("Position Float")]
    [Tooltip("Maximum positional displacement (meters).")]
    [SerializeField] private float posIntensity = 0.025f;
    [Tooltip("Speed of the floating position cycle.")]
    [SerializeField] private float posFrequency = 0.65f;

    [Header("Rotational Sway")]
    [Tooltip("Maximum rotational tilt (degrees).")]
    [SerializeField] private float rotIntensity = 0.45f;
    [Tooltip("Speed of the rotational sway cycle.")]
    [SerializeField] private float rotFrequency = 0.5f;

    // Unique random seeds to desynchronize X, Y, and Z axes
    private float seedX;
    private float seedY;
    private float seedRotX;
    private float seedRotZ;

    private Vector3 initialLocalPos;
    private Quaternion initialLocalRot;

    private void Awake()
    {
        initialLocalPos = transform.localPosition;
        initialLocalRot = transform.localRotation;

        // Generate random seeds so axes do not move in sync
        seedX = Random.Range(0f, 100f);
        seedY = Random.Range(100f, 200f);
        seedRotX = Random.Range(200f, 300f);
        seedRotZ = Random.Range(300f, 400f);
    }

    private void LateUpdate()
    {
        float time = Time.time;

        // Continuous organic positional offset using Perlin Noise (-1 to 1)
        float offsetX = (Mathf.PerlinNoise(seedX, time * posFrequency) * 2f - 1f) * posIntensity;
        float offsetY = (Mathf.PerlinNoise(seedY, time * posFrequency) * 2f - 1f) * posIntensity;

        // Continuous rotational tilt (subtle handheld roll and pitch)
        float tiltX = (Mathf.PerlinNoise(seedRotX, time * rotFrequency) * 2f - 1f) * rotIntensity;
        float tiltZ = (Mathf.PerlinNoise(seedRotZ, time * rotFrequency) * 2f - 1f) * rotIntensity;

        // Apply relative to the initial local transform
        transform.localPosition = initialLocalPos + new Vector3(offsetX, offsetY, 0f);
        transform.localRotation = initialLocalRot * Quaternion.Euler(tiltX, 0f, tiltZ);
    }

    // Call this if your player moves or your camera resets its default anchor
    public void UpdateBaselineTransform()
    {
        initialLocalPos = transform.localPosition;
        initialLocalRot = transform.localRotation;
    }
}