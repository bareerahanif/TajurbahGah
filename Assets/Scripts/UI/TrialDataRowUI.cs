using UnityEngine;
using UnityEngine.UI;
using TMPro;

/*
 * Layer: UI / presentation
 * Purpose: Fills in one row prefab instance with a TrialEntry's values (trial number + an
 * ITrialData's formatted columns) and owns that row's discard ("x") button. Generic over
 * ITrialData - never references pendulum-specific fields directly.
 * v2 addition: trial numbering and the discard control (TASK_pendulum_demo_v2.md item 5) - a
 * discarded row grays out and strikes through rather than disappearing, so the table stays an
 * honest record of everything the student did, including redos.
 * Talks to: driven by TrialDataTableUI; calls TrialLoopController.DiscardTrial on click.
 */
public class TrialDataRowUI : MonoBehaviour
{
    public TextMeshProUGUI trialNumberText;
    public TextMeshProUGUI[] columnTexts;
    public Button discardButton;

    private static readonly Color NormalColor = new Color(0.12f, 0.14f, 0.18f);
    private static readonly Color DiscardedColor = new Color(0.65f, 0.65f, 0.65f);

    private TrialLoopController trialLoop;
    private int entryNumber;

    public int EntryNumber => entryNumber;

    public void Initialize(TrialLoopController loop, TrialEntry entry)
    {
        trialLoop = loop;
        entryNumber = entry.Number;

        trialNumberText.text = entry.Number.ToString();
        SetValues(entry.Data.ToRowStrings());

        discardButton.onClick.RemoveAllListeners();
        discardButton.onClick.AddListener(() => trialLoop.DiscardTrial(entryNumber));

        SetDiscarded(entry.Discarded);
        SetDiscardable(true);
    }

    public void SetValues(string[] rowStrings)
    {
        for (int i = 0; i < columnTexts.Length && i < rowStrings.Length; i++)
            columnTexts[i].text = rowStrings[i];
    }

    public void SetDiscarded(bool discarded)
    {
        Color color = discarded ? DiscardedColor : NormalColor;
        FontStyles style = discarded ? FontStyles.Strikethrough : FontStyles.Normal;

        trialNumberText.color = color;
        trialNumberText.fontStyle = style;
        foreach (var t in columnTexts)
        {
            t.color = color;
            t.fontStyle = style;
        }

        if (discarded) SetDiscardable(false);
    }

    // Once analysis has been shown (Finished/Reflecting), history is locked - discarding stops
    // being offered so the graph/g the student already saw can't silently go stale underneath them.
    public void SetDiscardable(bool discardable)
    {
        discardButton.gameObject.SetActive(discardable);
    }
}
