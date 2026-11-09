using UnityEngine;
using UnityEngine.UI;
using TMPro;

/*
 * Layer: UI / presentation
 * Purpose: Closing reflection screen shown on the Reflecting step (TASK_pendulum_demo_v2.md
 * item 9) - restates the final computed g/% error (cached from ResultsGraphUI, not recomputed,
 * so it can never disagree with what the student already saw on the graph), asks why their
 * value might differ from 9.8 m/s^2, and offers a few tappable common-cause options that simply
 * toggle a selected highlight. No grading/scoring - this is a reflection prompt, not a quiz, so
 * nothing here is right/wrong or fed back into the logic layer.
 * Talks to: subscribes to PendulumExperimentController.StepChanged; reads
 * ResultsGraphUI.LastComputedG/LastComputedPercentError.
 */
public class ReflectionScreenUI : MonoBehaviour
{
    public PendulumExperimentController controller;
    public ResultsGraphUI resultsGraph;
    public GameObject panel;
    public TextMeshProUGUI summaryText;
    public TextMeshProUGUI promptText;
    public Button[] causeButtons;

    private const string QuestionPrompt = "Why might your measured value differ from 9.8 m/s²?";

    private static readonly Color UnselectedColor = new Color(0.85f, 0.85f, 0.88f);
    private static readonly Color SelectedColor = new Color(0.55f, 0.75f, 0.55f);

    void OnEnable()
    {
        controller.StepChanged += HandleStepChanged;
        foreach (var button in causeButtons)
        {
            var captured = button;
            captured.onClick.AddListener(() => SelectCause(captured));
        }
        HandleStepChanged(controller.CurrentStep); // sync visibility to whatever step we're already in
    }

    void OnDisable()
    {
        controller.StepChanged -= HandleStepChanged;
    }

    private void HandleStepChanged(PendulumStep step)
    {
        bool active = step == PendulumStep.Reflecting;
        if (panel != null) panel.SetActive(active);
        if (!active) return;

        summaryText.text =
            $"Your result: g = {resultsGraph.LastComputedG:F2} m/s² " +
            $"({resultsGraph.LastComputedPercentError:F1}% error vs 9.8 m/s²)";

        if (promptText != null) promptText.text = QuestionPrompt;
        foreach (var b in causeButtons)
            b.image.color = UnselectedColor;
    }

    // Selecting a cause has no right/wrong outcome - the only "effect" is the highlight plus a
    // short acknowledgment line, so the tap doesn't feel like it did nothing (the original
    // silent-highlight-only version was confusing to test, per user feedback).
    private void SelectCause(Button selected)
    {
        foreach (var b in causeButtons)
            b.image.color = (b == selected) ? SelectedColor : UnselectedColor;

        if (promptText != null)
            promptText.text = "Thanks for reflecting on your result - that's a completely valid factor.";
    }
}
