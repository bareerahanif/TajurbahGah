using UnityEngine;
using TMPro;

/*
 * Layer: UI / presentation
 * Purpose: Shows "Trial X of Y" near the data table, and relabels the Next-Trial button once the
 * target count is reached (TASK_pendulum_demo_v2.md item 3) - reuses the existing NextTrial()
 * call, just changes what the button says so the student isn't left pressing "Next Trial"
 * without knowing analysis is actually ready.
 * Talks to: reads TrialLoopController.ActiveCount/targetTrialCount/IsComplete; subscribes to
 * TrialAdded/TrialDiscarded; writes progress text and the Next-Trial button's label text.
 */
public class TrialProgressUI : MonoBehaviour
{
    public TrialLoopController trialLoop;
    public TextMeshProUGUI progressText;
    public TextMeshProUGUI nextTrialButtonLabel;

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
        int count = Mathf.Min(trialLoop.ActiveCount, trialLoop.targetTrialCount);
        progressText.text = $"Trial {count} of {trialLoop.targetTrialCount}";
        if (nextTrialButtonLabel != null)
            nextTrialButtonLabel.text = trialLoop.IsComplete ? "Finish & Analyze" : "Next Trial";
    }
}
