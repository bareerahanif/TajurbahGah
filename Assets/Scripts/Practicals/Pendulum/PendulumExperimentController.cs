using UnityEngine;

/*
 * Layer: Practical logic
 * Purpose: The pendulum experiment's state machine. Owns the current trial step, the
 * length/angle/prediction the student has set, the tap-based oscillation counter, and turns a
 * completed swing into a logged ITrialData. Deliberately has NO knowledge of mouse/touch/gaze,
 * cameras, or UI widgets: it only receives (a) drag deltas already decomposed into
 * vertical/horizontal axes from DraggableBobHandle, and (b) discrete Confirm/Start/Tap/Next
 * calls from UI Buttons. This separation is what lets VR input be swapped in later without any
 * changes here.
 *
 * v2 additions over v1 (see TASK_pendulum_demo_v2.md):
 * - A "Predicting" step between length and angle setup, where the student predicts how T will
 *   respond to L before seeing the result (predict-observe-explain learning pattern).
 * - Tap-based oscillation counting replaces the old free Start/Stop timer: the student taps once
 *   per oscillation, and the trial auto-finalizes once the tap count reaches oscillationCount.
 *   This was chosen over keeping a manual early-stop because the SHM model has no damping (a
 *   swing never "runs out" on its own), so nothing is lost by requiring the full tap count, and
 *   it avoids two competing ways to end a trial. A mistimed trial is fixed by discarding the
 *   logged row afterward (see TrialLoopController), not by aborting mid-swing.
 * - A "Reflecting" step after the results graph, for a closing reflection prompt.
 *
 * Talks to: reads drag axes from a DraggableBobHandle (interaction layer), uses
 * PendulumPhysicsModel for the swing math, logs finished trials into a TrialLoopController
 * (trial-loop layer) as PendulumTrialData. Raises StepChanged/ValidationFailed/
 * AngleLimitExceeded/OscillationTapped for the UI layer and exposes CurrentAngleDegrees/
 * CurrentLengthMeters/CurrentSwingAngleDegrees/TapCount/LastTrialPrediction for presentation
 * scripts to read each frame or on event.
 */
public enum PendulumStep
{
    SettingLength,
    Predicting,
    SettingAngle,
    ReadyToRelease,
    Swinging,
    Stopped,
    Finished,
    Reflecting
}

// Student's prediction of how the period T responds as the string length L increases. Recorded
// before the swing so the later reveal (see PredictionPromptUI) is an honest before/after check,
// not something answered with hindsight.
public enum PredictionChoice
{
    Up,
    Down,
    Same
}

public class PendulumExperimentController : MonoBehaviour
{
    [Header("Dependencies")]
    public DraggableBobHandle bobHandle;
    public TrialLoopController trialLoop;

    [Header("Physics (true value the student must recover experimentally)")]
    public float gravity = 9.8f;

    [Header("Length setup")]
    public float startingLengthMeters = 0.6f;
    public float minLengthMeters = 0.2f;
    public float maxLengthMeters = 1.0f;
    [Tooltip("Meters of length change per pixel of vertical drag.")]
    public float lengthMetersPerPixel = 0.002f;

    [Header("Angle setup")]
    [Tooltip("Small-angle approximation limit in degrees - matches the real-lab assumption the SHM formula relies on.")]
    public float maxAngleDegrees = 15f;
    [Tooltip("Smallest release angle allowed, so a trial always produces a visible, measurable swing.")]
    public float minAngleDegrees = 3f;
    [Tooltip("Degrees of angle change per pixel of horizontal drag.")]
    public float angleDegreesPerPixel = 0.15f;

    [Header("Timing")]
    [Tooltip("Number of oscillations the student taps through before the trial auto-finalizes. 20 is a standard lab convention that keeps timing error small without making a trial too slow for a demo.")]
    public int oscillationCount = 20;

    public event System.Action<PendulumStep> StepChanged;
    public event System.Action<string> ValidationFailed;
    // Fired specifically (alongside ValidationFailed) when the angle-limit is exceeded, so the UI
    // can show a longer one-time explanation without having to pattern-match message text.
    public event System.Action AngleLimitExceeded;
    public event System.Action<int> OscillationTapped;

    public PendulumStep CurrentStep { get; private set; } = PendulumStep.SettingLength;
    public float CurrentLengthMeters { get; private set; }
    public float CurrentAngleDegrees { get; private set; }
    public float ElapsedSeconds { get; private set; }
    public int TapCount { get; private set; }

    // The prediction recorded for whichever trial was most recently logged - read by the UI right
    // after TrialLoopController.TrialAdded fires, to reveal predicted-vs-actual.
    public PredictionChoice? LastTrialPrediction { get; private set; }
    private PredictionChoice? currentPrediction;

    // The prediction for the trial currently in progress (set the moment the student taps
    // Up/Down/Same, well before the trial resolves). Exposed so the UI can immediately
    // acknowledge the choice was registered, rather than the student only finding out via the
    // reveal text after the whole swing/tap sequence completes.
    public PredictionChoice? PendingPrediction => currentPrediction;

    // Amplitude the swing was actually released at - frozen at StartSwing so the SHM formula
    // always uses the angle the student committed to, even though CurrentAngleDegrees is not
    // updated while swinging.
    private float releaseAngleDegrees;
    private float swingElapsed;
    private bool hasWarnedAngleLimit;

    // Current angle of the bob mid-swing, derived from the SHM model. Only meaningful while
    // CurrentStep == Swinging; PendulumVisualPresenter checks the step before reading this.
    public float CurrentSwingAngleDegrees =>
        PendulumPhysicsModel.AngleAtTime(releaseAngleDegrees, CurrentLengthMeters, gravity, swingElapsed);

    void Awake()
    {
        CurrentLengthMeters = startingLengthMeters;
        CurrentAngleDegrees = 0f;
    }

    void OnEnable()
    {
        if (bobHandle != null)
            bobHandle.DragAxes += HandleDragAxes;
    }

    void OnDisable()
    {
        if (bobHandle != null)
            bobHandle.DragAxes -= HandleDragAxes;
    }

    void Update()
    {
        if (CurrentStep == PendulumStep.Swinging)
        {
            swingElapsed += Time.deltaTime;
            ElapsedSeconds = swingElapsed;
        }
    }

    // Called by DraggableBobHandle while the bob is held and dragged. The same physical drag
    // means different things depending on which step the experiment is in - that
    // interpretation is exactly why this logic must live here rather than in the interaction
    // layer.
    private void HandleDragAxes(float verticalPixels, float horizontalPixels)
    {
        switch (CurrentStep)
        {
            case PendulumStep.SettingLength:
                ApplyLengthDrag(verticalPixels);
                break;
            case PendulumStep.SettingAngle:
                ApplyAngleDrag(horizontalPixels);
                break;
            default:
                // Dragging the bob outside these two steps (e.g. mid-swing) has no effect.
                break;
        }
    }

    private void ApplyLengthDrag(float verticalPixels)
    {
        // Dragging down (negative screen delta) lengthens the string, matching pulling the bob
        // further from the pivot; dragging up shortens it.
        float newLength = CurrentLengthMeters - (verticalPixels * lengthMetersPerPixel);
        CurrentLengthMeters = Mathf.Clamp(newLength, minLengthMeters, maxLengthMeters);
    }

    private void ApplyAngleDrag(float horizontalPixels)
    {
        float proposedAngle = CurrentAngleDegrees + (horizontalPixels * angleDegreesPerPixel);

        if (Mathf.Abs(proposedAngle) > maxAngleDegrees)
        {
            // Reject the increment entirely rather than silently clamping, per CLAUDE.md's
            // corrective-feedback requirement - the student needs to notice they overshot, not
            // just feel the drag "stop working". The visual angle-limit arc (see
            // AngleLimitArcPresenter) now shows this boundary live, so the routine message here
            // can stay short; a longer one-time explanation is shown separately by the UI on the
            // first AngleLimitExceeded of the session.
            if (!hasWarnedAngleLimit)
            {
                ValidationFailed?.Invoke($"Angle limit exceeded (max {maxAngleDegrees:F0}°).");
                AngleLimitExceeded?.Invoke();
                hasWarnedAngleLimit = true;
            }
            return;
        }

        hasWarnedAngleLimit = false;
        CurrentAngleDegrees = proposedAngle;
    }

    // --- Explicit step transitions, wired to UI Buttons' OnClick. Buttons go through Unity's
    // own device-agnostic EventSystem (mouse/touch/gaze-raycaster all produce the same OnClick),
    // so they intentionally do not go through the custom IPracticalInput path. ---

    public void ConfirmLength()
    {
        if (CurrentStep != PendulumStep.SettingLength) return;
        SetStep(PendulumStep.Predicting);
    }

    public void SubmitPrediction(PredictionChoice choice)
    {
        if (CurrentStep != PendulumStep.Predicting) return;
        currentPrediction = choice;
        SetStep(PendulumStep.SettingAngle);
    }

    public void ConfirmAngle()
    {
        if (CurrentStep != PendulumStep.SettingAngle) return;
        if (Mathf.Abs(CurrentAngleDegrees) < minAngleDegrees)
        {
            ValidationFailed?.Invoke($"Drag the bob sideways at least {minAngleDegrees:F0}° so the swing is measurable.");
            return;
        }
        SetStep(PendulumStep.ReadyToRelease);
    }

    public void StartSwing()
    {
        if (CurrentStep != PendulumStep.ReadyToRelease) return;
        releaseAngleDegrees = CurrentAngleDegrees;
        swingElapsed = 0f;
        ElapsedSeconds = 0f;
        TapCount = 0;
        SetStep(PendulumStep.Swinging);
    }

    // Called once per oscillation the student counts (bob passing back through center). Tying
    // the logged N directly to the student's own taps - rather than a hidden separately-running
    // count - means what gets logged is exactly what the student experienced, per
    // TASK_pendulum_demo_v2.md item 4.
    public void TapOscillation()
    {
        if (CurrentStep != PendulumStep.Swinging) return;

        TapCount++;
        OscillationTapped?.Invoke(TapCount);

        if (TapCount >= oscillationCount)
            FinalizeSwing();
    }

    private void FinalizeSwing()
    {
        // Freeze the visual angle where the swing was stopped, so the bob doesn't visually jump
        // before the student presses Next Trial.
        CurrentAngleDegrees = CurrentSwingAngleDegrees;

        float periodSeconds = swingElapsed / TapCount;
        LastTrialPrediction = currentPrediction;
        trialLoop.AddTrial(new PendulumTrialData(CurrentLengthMeters, periodSeconds));
        SetStep(PendulumStep.Stopped);
    }

    public void NextTrial()
    {
        if (CurrentStep != PendulumStep.Stopped) return;

        if (trialLoop.IsComplete)
        {
            SetStep(PendulumStep.Finished);
            return;
        }

        CurrentAngleDegrees = 0f;
        currentPrediction = null;
        SetStep(PendulumStep.SettingLength);
    }

    public void ContinueToReflection()
    {
        if (CurrentStep != PendulumStep.Finished) return;
        SetStep(PendulumStep.Reflecting);
    }

    private void SetStep(PendulumStep step)
    {
        CurrentStep = step;
        StepChanged?.Invoke(step);
    }
}
