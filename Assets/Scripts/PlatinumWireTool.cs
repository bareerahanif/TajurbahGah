using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PlatinumWireTool : MonoBehaviour
{
    [Header("State Tracking")]
    public WireState currentState = WireState.Contaminated;
    public RadicalType loadedRadical = RadicalType.Contaminated;

    [Header("Visual Tip References")]
    [SerializeField] private Renderer wireTipRenderer;
    [SerializeField] private ParticleSystem saltGritParticles;
    [SerializeField] private TrailRenderer wetTrailRenderer;

    [Header("Tip Colors")]
    [SerializeField] private Color dirtyTipColor = new Color(0.35f, 0.3f, 0.25f);
    [SerializeField] private Color wetAcidTipColor = new Color(0.9f, 0.95f, 1.0f);
    [SerializeField] private Color cleanTipColor = new Color(0.85f, 0.85f, 0.9f);

    [Header("Drag Movement")]
    [SerializeField] private float hoverHeight = 0.96f;
    [SerializeField] private float followSpeed = 20.0f;
    [SerializeField] private Vector3 heldTiltAngle = new Vector3(35f, 0f, 0f);

    public Animator anim;

    private bool isGrabbed = false;
    private bool isBusyWithAction = false; // Prevents drag override while snapping/animating
    private Vector3 targetWorldPos;
    private Vector3 initialRestPos;
    private Quaternion initialRestRot;

    private BunsenBurnerFX activeBurner;
    private Coroutine burnCoroutine;

    private void Start()
    {
        initialRestPos = transform.position;
        initialRestRot = transform.rotation;
        targetWorldPos = initialRestPos;

        UpdateTipVisual();
    }

    private void Update()
    {
        // Only follow pointer if grabbed AND not currently locked in a snap/animation sequence
        if (isGrabbed && !isBusyWithAction)
        {
            Vector3 desiredPosition = new Vector3(targetWorldPos.x, hoverHeight, targetWorldPos.z);
            transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * followSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(heldTiltAngle), Time.deltaTime * 12f);
        }
    }

    public void GrabTool(Vector3 initialHitPoint)
    {
        if (isBusyWithAction) return;

        isGrabbed = true;
        targetWorldPos = initialHitPoint;
        StopAllCoroutines();
    }

    public void UpdateDragPosition(Vector3 newWorldPosition)
    {
        if (!isGrabbed || isBusyWithAction) return;
        targetWorldPos = newWorldPosition;
    }

    public void ReleaseTool()
    {
        isGrabbed = false;

        if (burnCoroutine != null)
        {
            StopCoroutine(burnCoroutine);
            burnCoroutine = null;
        }

        if (activeBurner != null)
        {
            activeBurner.ResetToBaseFlame();
        }

        if (!isBusyWithAction)
        {
            StartCoroutine(ReturnToRestRoutine());
        }
    }

    public void NotifyStationEntered(LabStation station)
    {
        if (!isGrabbed || isBusyWithAction) return;

        switch (station.stationType)
        {
            case StationType.HClBeaker:
                anim.SetTrigger("PlayDip");
                if (LabAudioController.Instance != null) LabAudioController.Instance.PlayDip();
#if UNITY_ANDROID && !UNITY_EDITOR
                Handheld.Vibrate();
#endif
                currentState = WireState.DippedInHCl;
                UpdateTipVisual();
                if (wetTrailRenderer != null) wetTrailRenderer.emitting = true;
                break;

            case StationType.SaltDish:
                if (currentState == WireState.Clean || currentState == WireState.DippedInHCl)
                {
                    StartCoroutine(SnapAndScrapeRoutine(station));
                }
                break;

            case StationType.BunsenBurnerTip:
                if (burnCoroutine != null) StopCoroutine(burnCoroutine);
                burnCoroutine = StartCoroutine(BurnRoutine());
                break;
        }
    }

    public void NotifyStationExited(LabStation station)
    {
        if (station.stationType == StationType.BunsenBurnerTip)
        {
            if (burnCoroutine != null)
            {
                StopCoroutine(burnCoroutine);
                burnCoroutine = null;
            }

            if (activeBurner != null)
            {
                activeBurner.ResetToBaseFlame();
            }
        }
    }

    private IEnumerator SnapAndScrapeRoutine(LabStation station)
    {
        isBusyWithAction = true;

        // 1. Target transform: use wireApproachPoint if assigned, otherwise station center
        Vector3 targetPos = station.wireApproachPoint != null
            ? station.wireApproachPoint.position
            : new Vector3(station.transform.position.x, hoverHeight, station.transform.position.z);

        Quaternion targetRot = station.wireApproachPoint != null
            ? station.wireApproachPoint.rotation
            : Quaternion.Euler(heldTiltAngle);

        // 2. Smoothly glide to the target alignment point
        float elapsed = 0f;
        float snapDuration = 0.2f;
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        while (elapsed < snapDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / snapDuration);
            t = t * t * (3f - 2f * t); // Smooth step

            transform.position = Vector3.Lerp(startPos, targetPos, t);
            transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }

        transform.position = targetPos;
        transform.rotation = targetRot;

        // 3. Fire the trigger to play Scrape_Dish locally at this dish
        if (anim != null) anim.SetTrigger("PlayScrape");
        if (LabAudioController.Instance != null) LabAudioController.Instance.PlayScrape();
#if UNITY_ANDROID && !UNITY_EDITOR
        Handheld.Vibrate();
#endif

        // 4. Wait for the animation to play out (match your clip length)
        yield return new WaitForSeconds(1.0f);

        // 5. Update chemical state based on this dish's salt
        currentState = WireState.LoadedWithSample;
        loadedRadical = station.saltContained;
        UpdateTipVisual();
        if (saltGritParticles != null) saltGritParticles.Play();

        // 6. Sync targetWorldPos to current position so dragging resumes without snapping
        targetWorldPos = transform.position;
        isBusyWithAction = false;

        // If the user released their touch/mouse during the animation, return to rest
        if (!isGrabbed)
        {
            StartCoroutine(ReturnToRestRoutine());
        }
    }

    private IEnumerator BurnRoutine()
    {
        if (activeBurner != null)
            activeBurner.SetFlameForRadical(loadedRadical);

        if (LabAudioController.Instance != null)
            LabAudioController.Instance.PlaySizzle();

        yield return new WaitForSeconds(2.5f);

        currentState = WireState.Clean;
        loadedRadical = RadicalType.None;
        UpdateTipVisual();

        if (wetTrailRenderer != null) wetTrailRenderer.emitting = false;
        if (activeBurner != null) activeBurner.ResetToBaseFlame();

        burnCoroutine = null;
    }

    private void UpdateTipVisual()
    {
        if (wireTipRenderer == null) return;

        Material mat = wireTipRenderer.material;
        switch (currentState)
        {
            case WireState.Contaminated:
                mat.color = dirtyTipColor;
                mat.SetFloat("_Smoothness", 0.1f);
                break;

            case WireState.DippedInHCl:
                mat.color = wetAcidTipColor;
                mat.SetFloat("_Smoothness", 0.95f);
                break;

            case WireState.LoadedWithSample:
                mat.color = (loadedRadical == RadicalType.Copper) ? new Color(0.2f, 0.85f, 0.7f) : Color.white;
                mat.SetFloat("_Smoothness", 0.05f);
                break;

            case WireState.Clean:
                mat.color = cleanTipColor;
                mat.SetFloat("_Smoothness", 0.85f);
                break;
        }
    }

    public void SetBurnerReference(BunsenBurnerFX burner) => activeBurner = burner;

    private IEnumerator ReturnToRestRoutine()
    {
        while (Vector3.Distance(transform.position, initialRestPos) > 0.002f)
        {
            transform.position = Vector3.Lerp(transform.position, initialRestPos, Time.deltaTime * 8f);
            transform.rotation = Quaternion.Slerp(transform.rotation, initialRestRot, Time.deltaTime * 8f);
            yield return null;
        }

        transform.position = initialRestPos;
        transform.rotation = initialRestRot;
    }
}