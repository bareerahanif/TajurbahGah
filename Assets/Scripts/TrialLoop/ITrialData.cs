/*
 * Layer: Trial loop (generic, reusable across future practicals)
 * Purpose: Minimal contract a practical's trial result must satisfy to be displayed by the
 * shared TrialDataTableUI, without TrialLoopController or the table UI ever needing to know
 * any practical-specific field names.
 */
public interface ITrialData
{
    // Column headers for the data table, e.g. ["L (cm)", "T (s)", "T² (s²)"].
    string[] ColumnLabels { get; }

    // Formatted values for one row, in the same order as ColumnLabels.
    string[] ToRowStrings();
}
