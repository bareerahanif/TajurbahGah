using UnityEngine;

public class LabAudioController : MonoBehaviour
{
    public static LabAudioController Instance;

    [Header("Clips")]
    [SerializeField] private AudioClip liquidDipSound;
    [SerializeField] private AudioClip saltScrapeSound;
    [SerializeField] private AudioClip gasSizzleSound;

    private AudioSource audioSource;

    private void Awake()
    {
        Instance = this;
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0.0f; // 2D crisp UI audio for mobile
    }

    public void PlayDip() { if (liquidDipSound) audioSource.PlayOneShot(liquidDipSound, 0.7f); }
    public void PlayScrape() { if (saltScrapeSound) audioSource.PlayOneShot(saltScrapeSound, 0.8f); }
    public void PlaySizzle() { if (gasSizzleSound) audioSource.PlayOneShot(gasSizzleSound, 0.9f); }
}