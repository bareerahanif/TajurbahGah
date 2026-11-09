using UnityEngine;
using TMPro;

/*
 * Layer: UI / presentation
 * Purpose: Small "digital gauge" readout shown while the student is actively setting length or
 * angle, updating every frame from the controller's already-live CurrentLengthMeters/
 * CurrentAngleDegrees (TASK_pendulum_demo_v2.md item 1) - these values already update mid-drag
 * in the logic layer, this just surfaces them continuously instead of only after Confirm.
 * Talks to: reads PendulumExperimentController.CurrentStep/CurrentLengthMeters/CurrentAngleDegrees
 * every frame; writes to a TMP text field. Visibility toggled via its own panel GameObject.
 */
public class LiveReadoutUI : MonoBehaviour
{
    public PendulumExperimentController controller;
    public TextMeshProUGUI readoutText;
    public GameObject panel;

    void Update()
    {
        switch (controller.CurrentStep)
        {
            case PendulumStep.SettingLength:
                panel.SetActive(true);
                readoutText.text = $"L = {controller.CurrentLengthMeters * 100f:F1} cm";
                break;
            case PendulumStep.SettingAngle:
                panel.SetActive(true);
                readoutText.text = $"θ = {controller.CurrentAngleDegrees:F1}°";
                break;
            default:
                panel.SetActive(false);
                break;
        }
    }
}
