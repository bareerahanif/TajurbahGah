using System.Collections.Generic;
using UnityEngine;
using TMPro;

/*
 * Layer: UI / presentation
 * Purpose: Once at least 2 non-discarded trials exist, shows a running computed g and % error
 * near the table, updating on every trial added/discarded (TASK_pendulum_demo_v2.md item 6) -
 * lets the student watch the estimate stabilize as data accumulates, rather than only revealing
 * it at the very end. Reuses the same LinearRegression utility and formula as the final results
 * graph, just recomputed live over whatever active trials exist so far.
 * Talks to: subscribes to TrialLoopController.TrialAdded/TrialDiscarded; reads ActiveTrials.
 */
public class LiveGEstimateUI : MonoBehaviour
{
    public TrialLoopController trialLoop;
    public TextMeshProUGUI estimateText;
    public GameObject panel;

    private const float AcceptedGravity = 9.8f;

    void OnEnable()
    {
        trialLoop.TrialAdded += HandleChanged;
        trialLoop.TrialDiscarded += HandleChanged;
        Refresh();
    }

    void OnDisable()
    {
        trialLoop.TrialAdded -= HandleChanged;
        trialLoop.TrialDiscarded -= HandleChanged;
    }

    private void HandleChanged(TrialEntry entry) => Refresh();

    private void Refresh()
    {
        var points = new List<Vector2>();
        foreach (var trial in trialLoop.ActiveTrials)
        {
            if (trial is PendulumTrialData p)
                points.Add(new Vector2(p.LengthMeters, p.PeriodSquared));
        }

        if (points.Count < 2)
        {
            if (panel != null) panel.SetActive(false);
            return;
        }

        LinearRegression.Fit(points, out float slope, out float intercept);
        float computedG = Mathf.Approximately(slope, 0f) ? 0f : (4f * Mathf.PI * Mathf.PI) / slope;
        float percentError = Mathf.Abs(computedG - AcceptedGravity) / AcceptedGravity * 100f;

        if (panel != null) panel.SetActive(true);
        estimateText.text = $"g ≈ {computedG:F2} m/s²  ({percentError:F0}% error)";
    }
}
