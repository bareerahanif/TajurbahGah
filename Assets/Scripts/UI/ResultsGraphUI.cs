using System.Collections.Generic;
using UnityEngine;
using TMPro;

/*
 * Layer: UI / presentation
 * Purpose: Once the experiment reaches the Finished step, computes the T^2-vs-L best fit over
 * the non-discarded trials and derives g from the pendulum relation T^2 = (4*pi^2/g)*L, i.e.
 * g = 4*pi^2/slope, then displays the plot, computed g, and its percent error against the
 * accepted 9.8 m/s^2.
 * v2 change: triggers off PendulumExperimentController.StepChanged == Finished instead of the
 * old TrialLoopController.AllTrialsComplete event, and reads ActiveTrials instead of Trials -
 * with discards now possible, "all trials complete" is a live check (trialLoop.IsComplete), not
 * a one-shot event, and results should only appear once the student explicitly finishes (via
 * NextTrial once complete), not silently the instant the count is first reached. The computed
 * g/error are cached (LastComputedG/LastComputedPercentError) so ReflectionScreenUI can restate
 * them without recomputing.
 * Talks to: subscribes to PendulumExperimentController.StepChanged; reads
 * TrialLoopController.ActiveTrials (PendulumTrialData via ITrialData); uses LinearRegression;
 * drives UIGraphPlotter; writes result text; also wires the "Continue" button to
 * controller.ContinueToReflection().
 */
public class ResultsGraphUI : MonoBehaviour
{
    public TrialLoopController trialLoop;
    public PendulumExperimentController controller;
    public UIGraphPlotter plotter;
    public GameObject resultsPanel;
    public TextMeshProUGUI gResultText;
    public TextMeshProUGUI errorText;

    private const float AcceptedGravity = 9.8f;

    public float LastComputedG { get; private set; }
    public float LastComputedPercentError { get; private set; }

    void OnEnable()
    {
        controller.StepChanged += HandleStepChanged;
    }

    void OnDisable()
    {
        controller.StepChanged -= HandleStepChanged;
    }

    private void HandleStepChanged(PendulumStep step)
    {
        if (step == PendulumStep.Finished)
            ShowResults();
        else if (resultsPanel != null)
            resultsPanel.SetActive(false);
    }

    private void ShowResults()
    {
        var points = new List<Vector2>();
        float xMax = 0f, yMax = 0f;

        foreach (var trial in trialLoop.ActiveTrials)
        {
            if (trial is PendulumTrialData pendulumTrial)
            {
                var point = new Vector2(pendulumTrial.LengthMeters, pendulumTrial.PeriodSquared);
                points.Add(point);
                xMax = Mathf.Max(xMax, point.x);
                yMax = Mathf.Max(yMax, point.y);
            }
        }

        LinearRegression.Fit(points, out float slope, out float intercept);

        // T^2 = (4*pi^2/g) * L  =>  g = 4*pi^2 / slope
        float computedG = Mathf.Approximately(slope, 0f) ? 0f : (4f * Mathf.PI * Mathf.PI) / slope;
        float percentError = Mathf.Abs(computedG - AcceptedGravity) / AcceptedGravity * 100f;
        LastComputedG = computedG;
        LastComputedPercentError = percentError;

        gResultText.text = $"Computed g = {computedG:F2} m/s²";
        errorText.text = $"% error vs 9.8 m/s² = {percentError:F1}%";

        plotter.Plot(points, 0f, xMax * 1.1f, 0f, yMax * 1.1f, slope, intercept);

        if (resultsPanel != null) resultsPanel.SetActive(true);
    }
}
