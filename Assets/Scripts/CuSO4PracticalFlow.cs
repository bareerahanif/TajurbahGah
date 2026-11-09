
using System.Collections;
using TMPro;
using UnityEngine;

public class CuSO4PracticalFlow : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Camera sceneCamera;
    [SerializeField] private Transform introPoint;
    [SerializeField] private Transform approachPoint;
    [SerializeField] private Transform practicalPoint;

    [SerializeField] private float openingDelay = 0.8f;
    [SerializeField] private float cameraMoveDuration = 2.0f;

    [Header("UI Panels")]
    [SerializeField] private GameObject promptPanel;
    [SerializeField] private GameObject instructionsPanel;
    [SerializeField] private GameObject tipPanel;

    [Header("UI Text")]
    [SerializeField] private TMP_Text tipText;
    [SerializeField] private TMP_Text stepTitleText;
    [SerializeField] private TMP_Text stepDescriptionText;

    private Coroutine cameraMovement;
    private int currentStep = -1;
    private bool practicalStarted = false;

    private readonly string[] stepTitles =
    {
        "Step 1 — Identify the sample",
        "Step 2 — Prepare the dropper",
        "Step 3 — Add the water sample",
        "Step 4 — Observe the colour change",
        "Step 5 — Record your conclusion"
    };

    private readonly string[] stepDescriptions =
    {
        "Locate the white anhydrous copper(II) sulfate sample and the water sample. Identify the apparatus before starting.",

        "Select the clean dropper. Use it to collect a small amount of the water sample. Avoid touching the sample with the dropper tip.",

        "Transfer a few drops of water onto the white anhydrous copper sulfate. Watch the sample closely.",

        "Observe the white solid turning blue as hydrated copper sulfate forms. In this simulation, the visual change represents the chemical process.",

        "Record your observation: the white solid turns blue. Conclude that the colour change indicates the presence of water."
    };

    private readonly string[] stepTips =
    {
        "TIP: Anhydrous means that the substance does not contain water of crystallisation. Anhydrous copper sulfate is white.",

        "SAFETY: Avoid direct contact with copper sulfate. In a real laboratory, use appropriate eye protection and follow your teacher's instructions.",

        "REMEMBER: Add the water to the sample in small amounts so the colour change is easy to observe.",

        "SCIENCE NOTE: Blue hydrated copper sulfate forms when anhydrous copper sulfate takes up water. The colour change is the key observation.",

        "CHECK YOUR UNDERSTANDING: The colour change indicates water is present in the tested sample. Record both the observation and the conclusion."
    };

    private void Start()
    {
        // Begin with the camera looking at the workstation.
        if (sceneCamera != null && introPoint != null)
        {
            sceneCamera.transform.SetPositionAndRotation(
                introPoint.position,
                introPoint.rotation
            );
        }

        if (promptPanel != null)
            promptPanel.SetActive(false);

        if (instructionsPanel != null)
            instructionsPanel.SetActive(false);

        if (tipPanel != null)
            tipPanel.SetActive(false);

        StartCoroutine(OpeningSequence());
    }

    private IEnumerator OpeningSequence()
    {
        yield return new WaitForSeconds(openingDelay);

        if (approachPoint != null)
            yield return MoveCameraTo(approachPoint);

        // Ask the student whether they want to begin.
        if (promptPanel != null)
            promptPanel.SetActive(true);
    }

    public void BeginPractical()
    {
        if (practicalStarted)
            return;

        practicalStarted = true;

        if (promptPanel != null)
            promptPanel.SetActive(false);

        if (cameraMovement != null)
            StopCoroutine(cameraMovement);

        cameraMovement = StartCoroutine(EnterPracticalView());
    }

    private IEnumerator EnterPracticalView()
    {
        if (practicalPoint != null)
            yield return MoveCameraTo(practicalPoint);

        if (instructionsPanel != null)
            instructionsPanel.SetActive(true);

        ShowStep(0);
    }

    private IEnumerator MoveCameraTo(Transform target)
    {
        if (sceneCamera == null || target == null)
            yield break;

        Transform cam = sceneCamera.transform;

        Vector3 startPosition = cam.position;
        Quaternion startRotation = cam.rotation;

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, cameraMoveDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            cam.position = Vector3.Lerp(
                startPosition,
                target.position,
                smoothT
            );

            cam.rotation = Quaternion.Slerp(
                startRotation,
                target.rotation,
                smoothT
            );

            yield return null;
        }

        cam.SetPositionAndRotation(
            target.position,
            target.rotation
        );
    }

    public void NextStep()
    {
        if (!practicalStarted)
            return;

        ShowStep(currentStep + 1);
    }

    public void PreviousStep()
    {
        if (!practicalStarted)
            return;

        ShowStep(currentStep - 1);
    }

    private void ShowStep(int index)
    {
        if (index < 0 || index >= stepTitles.Length)
            return;

        currentStep = index;

        if (stepTitleText != null)
            stepTitleText.text = stepTitles[index];

        if (stepDescriptionText != null)
            stepDescriptionText.text = stepDescriptions[index];

        if (tipText != null)
            tipText.text = stepTips[index];

        if (tipPanel != null)
            tipPanel.SetActive(true);
    }
}
