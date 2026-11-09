using UnityEngine;
using TMPro;

/*
 * Layer: UI / presentation
 * Purpose: Predict-observe-explain step (TASK_pendulum_demo_v2.md item 7). Shows three tappable
 * options (Up/Down/Same) during the Predicting step, calling SubmitPrediction so the choice is
 * recorded BEFORE the student sees the angle/swing - an honest prediction, not one answered with
 * hindsight. After the resulting trial logs, briefly reveals whether the prediction matched the
 * actual trend versus the previous non-discarded trial, normalized for whether L actually went
 * up or down between the two (a predicted "T goes up as L increases" is equally validated by
 * "T goes down as L decreases" - same physical relationship, so the comparison direction flips
 * to match whichever way L actually moved). The very first trial has no prior trial to compare
 * against, so it's just acknowledged rather than graded.
 * Talks to: subscribes to PendulumExperimentController.StepChanged and calls
 * controller.SubmitPrediction; reads TrialLoopController.Entries for the previous trial.
 */
public class PredictionPromptUI : MonoBehaviour
{
    public PendulumExperimentController controller;
    public TrialLoopController trialLoop;
    public GameObject promptPanel;
    public TextMeshProUGUI revealText;

    void OnEnable()
    {
        controller.StepChanged += HandleStepChanged;
        trialLoop.TrialAdded += HandleTrialAdded;
        HandleStepChanged(controller.CurrentStep); // sync visibility to whatever step we're already in
    }

    void OnDisable()
    {
        controller.StepChanged -= HandleStepChanged;
        trialLoop.TrialAdded -= HandleTrialAdded;
    }

    private void HandleStepChanged(PendulumStep step)
    {
        if (promptPanel != null) promptPanel.SetActive(step == PendulumStep.Predicting);
        if (step == PendulumStep.Predicting && revealText != null)
            revealText.text = string.Empty;
    }

    // Wired to the Up/Down/Same buttons' OnClick.
    public void PredictUp() => controller.SubmitPrediction(PredictionChoice.Up);
    public void PredictDown() => controller.SubmitPrediction(PredictionChoice.Down);
    public void PredictSame() => controller.SubmitPrediction(PredictionChoice.Same);

    private void HandleTrialAdded(TrialEntry entry)
    {
        if (revealText == null) return;
        var prediction = controller.LastTrialPrediction;
        if (prediction == null) return;

        // Find the previous non-discarded trial before this one, if any.
        PendulumTrialData previous = null;
        foreach (var e in trialLoop.Entries)
        {
            if (e.Number == entry.Number) break;
            if (!e.Discarded && e.Data is PendulumTrialData p) previous = p;
        }

        if (previous == null || !(entry.Data is PendulumTrialData current))
        {
            revealText.text = "Prediction recorded - let's see how it holds up as more trials come in.";
            return;
        }

        if (Mathf.Approximately(current.LengthMeters, previous.LengthMeters))
        {
            revealText.text = "Length barely changed from last trial - hard to compare this time.";
            return;
        }

        bool lengthIncreased = current.LengthMeters > previous.LengthMeters;
        float periodDelta = current.PeriodSeconds - previous.PeriodSeconds;

        // Normalize to "as L increases, T does..." regardless of which direction L actually moved.
        PredictionChoice actualTrend;
        if (Mathf.Abs(periodDelta) < 0.005f) actualTrend = PredictionChoice.Same;
        else if ((periodDelta > 0f) == lengthIncreased) actualTrend = PredictionChoice.Up;
        else actualTrend = PredictionChoice.Down;

        bool correct = actualTrend == prediction.Value;
        string trendWord = actualTrend switch
        {
            PredictionChoice.Up => "increased",
            PredictionChoice.Down => "decreased",
            _ => "stayed about the same"
        };
        revealText.text = correct
            ? $"Your prediction was right - T {trendWord} as L changed."
            : $"Not quite - T actually {trendWord} this time.";
    }
}
