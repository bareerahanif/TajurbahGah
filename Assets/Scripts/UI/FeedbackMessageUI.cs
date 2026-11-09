using UnityEngine;
using TMPro;
using System.Collections;

/*
 * Layer: UI / presentation
 * Purpose: Shows a transient warning message when the logic layer rejects an input (e.g. the
 * student tried to exceed the 15-degree small-angle limit). Per CLAUDE.md, invalid setups must
 * be explicitly explained, not silently clamped - this is where that explanation is displayed.
 * v2 addition (TASK_pendulum_demo_v2.md item 8): the FIRST time the student exceeds the angle
 * limit in a session, a longer plain-language explanation of *why* the limit exists is shown
 * instead of the routine short message (which now stays visible via the visual angle-limit arc,
 * see AngleLimitArcPresenter, so it can be brief). Every time after that, the routine short
 * message from ValidationFailed is enough - repeating the long explanation every nudge would be
 * naggy rather than helpful.
 * Talks to: subscribes to PendulumExperimentController.ValidationFailed/AngleLimitExceeded.
 */
public class FeedbackMessageUI : MonoBehaviour
{
    public PendulumExperimentController controller;
    public TextMeshProUGUI feedbackText;
    public float displaySeconds = 3f;
    public float whyExplanationDisplaySeconds = 6f;

    private const string WhyExplanation =
        "Above about 15°, the simple pendulum formula starts to become inaccurate - it relies on " +
        "an approximation that only holds for small swings.";

    private Coroutine hideRoutine;
    private bool hasShownWhyExplanation;

    void OnEnable()
    {
        controller.ValidationFailed += HandleValidationFailed;
        controller.AngleLimitExceeded += HandleAngleLimitExceeded;
        feedbackText.text = string.Empty;
    }

    void OnDisable()
    {
        controller.ValidationFailed -= HandleValidationFailed;
        controller.AngleLimitExceeded -= HandleAngleLimitExceeded;
    }

    private void HandleValidationFailed(string message)
    {
        ShowMessage(message, displaySeconds);
    }

    // Fires alongside ValidationFailed specifically for angle-limit violations; synchronously
    // runs after HandleValidationFailed for the same event (see ApplyAngleDrag), so on the first
    // occurrence this correctly overrides the routine short message with the longer explanation.
    private void HandleAngleLimitExceeded()
    {
        if (hasShownWhyExplanation) return;
        hasShownWhyExplanation = true;
        ShowMessage(WhyExplanation, whyExplanationDisplaySeconds);
    }

    private void ShowMessage(string message, float duration)
    {
        feedbackText.text = message;
        if (hideRoutine != null) StopCoroutine(hideRoutine);
        hideRoutine = StartCoroutine(HideAfterDelay(duration));
    }

    private IEnumerator HideAfterDelay(float duration)
    {
        yield return new WaitForSeconds(duration);
        feedbackText.text = string.Empty;
    }
}
