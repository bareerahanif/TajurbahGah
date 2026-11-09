using System;
using System.Collections.Generic;
using UnityEngine;

/*
 * Layer: Trial loop (generic, reusable across future practicals)
 * Purpose: Generic "set up -> observe -> record -> repeat -> analyze" trial bookkeeping.
 * Knows nothing about pendulums, angles, or lengths - it only stores whatever ITrialData a
 * specific practical hands it. Future practicals are meant to reuse this component unchanged
 * by implementing their own ITrialData and calling AddTrial(); only the practical-specific
 * data class should ever need to change. Treat this file with extra care when editing -
 * per CLAUDE.md it is one of the most important reusable pieces in the whole project.
 *
 * v2 addition: each trial is now wrapped in a TrialEntry carrying a sequential number and a
 * discard flag, per TASK_pendulum_demo_v2.md item 5 - real labs involve throwing out a mistimed
 * run, and a discarded trial must not count toward the target or feed the analysis. Numbers are
 * never reused/renumbered on discard, so the table stays an honest record of everything the
 * student did. The old one-shot AllTrialsComplete event is removed: with discards, "complete"
 * is not monotonic (discarding a trial after reaching the target un-completes it), so callers
 * should just read the live IsComplete property instead of listening for a single edge.
 *
 * Talks to: fed trials by PendulumExperimentController (logic layer); read by
 * TrialDataTableUI, LiveGEstimateUI, TrialProgressUI and ResultsGraphUI (UI layer) via
 * Entries/ActiveTrials/TrialAdded/TrialDiscarded/IsComplete.
 */
public class TrialEntry
{
    public readonly int Number;
    public readonly ITrialData Data;
    public bool Discarded;

    public TrialEntry(int number, ITrialData data)
    {
        Number = number;
        Data = data;
    }
}

public class TrialLoopController : MonoBehaviour
{
    [Tooltip("How many non-discarded trials the student must complete before analysis is unlocked.")]
    public int targetTrialCount = 5;

    private readonly List<TrialEntry> entries = new List<TrialEntry>();
    public IReadOnlyList<TrialEntry> Entries => entries;

    // Convenience view used by analysis code (LiveGEstimateUI, ResultsGraphUI) that only ever
    // cares about the data, not the bookkeeping.
    public IEnumerable<ITrialData> ActiveTrials
    {
        get
        {
            foreach (var e in entries)
                if (!e.Discarded) yield return e.Data;
        }
    }

    public int ActiveCount
    {
        get
        {
            int count = 0;
            foreach (var e in entries)
                if (!e.Discarded) count++;
            return count;
        }
    }

    public event Action<TrialEntry> TrialAdded;
    public event Action<TrialEntry> TrialDiscarded;

    public bool IsComplete => ActiveCount >= targetTrialCount;

    public TrialEntry AddTrial(ITrialData trial)
    {
        var entry = new TrialEntry(entries.Count + 1, trial);
        entries.Add(entry);
        TrialAdded?.Invoke(entry);
        return entry;
    }

    public void DiscardTrial(int number)
    {
        foreach (var e in entries)
        {
            if (e.Number == number && !e.Discarded)
            {
                e.Discarded = true;
                TrialDiscarded?.Invoke(e);
                return;
            }
        }
    }

    public void ResetTrials()
    {
        entries.Clear();
    }
}
