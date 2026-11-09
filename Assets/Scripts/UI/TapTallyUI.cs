using UnityEngine;
using TMPro;

/*
 * Layer: UI / presentation
 * Purpose: Shows the running oscillation tap tally ("14 / 20") during the Swinging step
 * (TASK_pendulum_demo_v2.md item 4), so the student always knows where they are in the count.
 * Talks to: subscribes to PendulumExperimentController.OscillationTapped/StepChanged.
 */
public class TapTallyUI : MonoBehaviour
{
    public PendulumExperimentController controller;
    public TextMeshProUGUI tallyText;
    public GameObject panel;

    void OnEnable()
    {
        controller.OscillationTapped += HandleTapped;
        controller.StepChanged += HandleStepChanged;
        HandleStepChanged(controller.CurrentStep); // sync visibility to whatever step we're already in
    }

    void OnDisable()
    {
        controller.OscillationTapped -= HandleTapped;
        controller.StepChanged -= HandleStepChanged;
    }

    private void HandleStepChanged(PendulumStep step)
    {
        bool active = step == PendulumStep.Swinging;
        if (panel != null) panel.SetActive(active);
        if (active) tallyText.text = $"0 / {controller.oscillationCount}";
    }

    private void HandleTapped(int count)
    {
        tallyText.text = $"{count} / {controller.oscillationCount}";
    }
}
