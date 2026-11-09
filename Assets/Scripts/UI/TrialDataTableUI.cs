using System.Collections.Generic;
using UnityEngine;
using TMPro;

/*
 * Layer: UI / presentation
 * Purpose: Appends a row to the visible data table whenever the TrialLoopController logs a new
 * trial, and grays out a row when it's discarded. Generic over ITrialData - never references
 * pendulum-specific fields directly.
 * v2 addition: a Trial # header/column, and locking discard once the experiment reaches
 * Finished/Reflecting (TASK_pendulum_demo_v2.md item 5) - once the student has seen the
 * graph/g, further discarding is disallowed rather than letting the shown analysis go stale.
 * Talks to: subscribes to TrialLoopController.TrialAdded/TrialDiscarded; instantiates rowPrefab
 * into rowContainer; reads PendulumExperimentController.StepChanged to lock discard.
 */
public class TrialDataTableUI : MonoBehaviour
{
    public TrialLoopController trialLoop;
    public PendulumExperimentController controller;
    public TrialDataRowUI rowPrefab;
    public Transform rowContainer;
    [Tooltip("Header texts in order: Trial #, then one per ITrialData column.")]
    public TextMeshProUGUI[] headerTexts;

    private bool headersSet;
    private readonly Dictionary<int, TrialDataRowUI> rowsByNumber = new Dictionary<int, TrialDataRowUI>();

    void OnEnable()
    {
        trialLoop.TrialAdded += HandleTrialAdded;
        trialLoop.TrialDiscarded += HandleTrialDiscarded;
        if (controller != null) controller.StepChanged += HandleStepChanged;
    }

    void OnDisable()
    {
        trialLoop.TrialAdded -= HandleTrialAdded;
        trialLoop.TrialDiscarded -= HandleTrialDiscarded;
        if (controller != null) controller.StepChanged -= HandleStepChanged;
    }

    private void HandleTrialAdded(TrialEntry entry)
    {
        if (!headersSet)
        {
            SetHeaders(entry.Data.ColumnLabels);
            headersSet = true;
        }

        TrialDataRowUI row = Instantiate(rowPrefab, rowContainer);
        row.Initialize(trialLoop, entry);
        row.gameObject.SetActive(true);
        rowsByNumber[entry.Number] = row;
    }

    private void HandleTrialDiscarded(TrialEntry entry)
    {
        if (rowsByNumber.TryGetValue(entry.Number, out var row))
            row.SetDiscarded(true);
    }

    private void HandleStepChanged(PendulumStep step)
    {
        if (step != PendulumStep.Finished && step != PendulumStep.Reflecting) return;
        foreach (var row in rowsByNumber.Values)
            row.SetDiscardable(false);
    }

    private void SetHeaders(string[] labels)
    {
        if (headerTexts.Length == 0) return;
        headerTexts[0].text = "#";
        for (int i = 0; i < labels.Length && i + 1 < headerTexts.Length; i++)
            headerTexts[i + 1].text = labels[i];
    }
}
