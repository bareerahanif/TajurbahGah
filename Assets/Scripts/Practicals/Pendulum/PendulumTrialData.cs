/*
 * Layer: Practical logic
 * Purpose: One completed pendulum trial (a single length setting, timed and logged).
 * Implements the generic ITrialData contract so the shared TrialLoopController/
 * TrialDataTableUI can display it without any pendulum-specific code.
 */
public class PendulumTrialData : ITrialData
{
    public readonly float LengthMeters;
    public readonly float PeriodSeconds;
    public readonly float PeriodSquared;

    public PendulumTrialData(float lengthMeters, float periodSeconds)
    {
        LengthMeters = lengthMeters;
        PeriodSeconds = periodSeconds;
        PeriodSquared = periodSeconds * periodSeconds;
    }

    public string[] ColumnLabels => new[] { "L (cm)", "T (s)", "T² (s²)" };

    public string[] ToRowStrings() => new[]
    {
        (LengthMeters * 100f).ToString("F1"),
        PeriodSeconds.ToString("F2"),
        PeriodSquared.ToString("F3")
    };
}
