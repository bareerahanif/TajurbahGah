using UnityEngine;
using TMPro;

/*
 * Layer: UI / presentation
 * Purpose: Shows a plain-language instruction for whatever step the experiment is currently in.
 * v2 adds text for the new Predicting/Reflecting steps and switches the Swinging instruction
 * from "press Stop" to tap-based counting, per TASK_pendulum_demo_v2.md.
 * Talks to: subscribes to PendulumExperimentController.StepChanged; writes to a TMP text field.
 */
public class StepInstructionUI : MonoBehaviour
{
    public PendulumExperimentController controller;
    public TextMeshProUGUI instructionText;

    void OnEnable()
    {
        controller.StepChanged += HandleStepChanged;
        HandleStepChanged(controller.CurrentStep);
    }

    void OnDisable()
    {
        controller.StepChanged -= HandleStepChanged;
    }

    private void HandleStepChanged(PendulumStep step)
    {
        switch (step)
        {
            case PendulumStep.SettingLength:
                instructionText.text = "Drag the bob up or down to set the string length, then press Confirm Length.";
                break;
            case PendulumStep.Predicting:
                instructionText.text = "Before you swing: as L increases, do you think T will go up, down, or stay the same?";
                break;
            case PendulumStep.SettingAngle:
                // Immediately acknowledges the prediction that was just tapped in, rather than
                // leaving the student wondering whether pressing Up/Down/Same did anything until
                // the reveal appears later, after the whole swing/tap sequence.
                string predictedWord = controller.PendingPrediction switch
                {
                    PredictionChoice.Up => "go up",
                    PredictionChoice.Down => "go down",
                    PredictionChoice.Same => "stay the same",
                    _ => null
                };
                instructionText.text = predictedWord != null
                    ? $"Predicted: T will {predictedWord}. Now drag the bob sideways to set the starting angle (max 15°), then press Confirm Angle."
                    : "Drag the bob sideways to set the starting angle (max 15°), then press Confirm Angle.";
                break;
            case PendulumStep.ReadyToRelease:
                instructionText.text = "Press Start to release the pendulum.";
                break;
            case PendulumStep.Swinging:
                instructionText.text = $"Tap once each time the bob swings back through center, up to {controller.oscillationCount}.";
                break;
            case PendulumStep.Stopped:
                instructionText.text = "Trial logged. Press Next Trial to continue.";
                break;
            case PendulumStep.Finished:
                instructionText.text = "All trials complete. See your results below.";
                break;
            case PendulumStep.Reflecting:
                instructionText.text = "One last thing before you finish up.";
                break;
        }
    }
}
