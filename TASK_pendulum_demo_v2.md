# Task: Pendulum Practical — v2 (Learning & UX Improvements)

## Context
The v1 pendulum demo from TASK_pendulum_demo.md is working and has been built to an APK
(drag length → confirm → drag angle → confirm → start/stop timer → trial logged to table →
repeat → graph shows computed g). This task adds a set of UX and learning improvements on top
of that working version. Read CLAUDE.md first — the layer separation (interaction / logic / UI)
still applies: everything below should be additive to the UI/presentation layer and the
interaction layer, and should NOT require rewriting the logic layer's state machine or physics.
If something below seems to require touching the logic layer, flag it before proceeding rather
than assuming.

Treat this as a UI/UX-quality pass as much as a features pass — this version may be shown to
supervisors/stakeholders, so polish and clarity matter, not just functionality.

## 1. Live readouts while adjusting parameters
- While the student is dragging the bob to set length, show a live-updating numeric readout
  (e.g. "L = 24 cm") near the bob or in a small HUD, updating continuously as they drag — not
  only after they hit Confirm.
- Same for angle: while dragging to set displacement, show a live-updating angle readout
  (e.g. "θ = 12°").
- These readouts should feel like part of the lab equipment (e.g. styled like a small digital
  gauge/ruler marking), not a debug overlay — keep visual style consistent with the existing UI.

## 2. Visual angle-limit indicator
- Add a visual arc/protractor-style overlay behind or around the bob's swing path showing the
  valid displacement range.
- Shade it clearly: green/neutral up to the small-angle limit (~15°), red/warning beyond it.
- This should appear during the angle-setting step and update live as the student drags,
  replacing or supplementing the after-the-fact rejection message — the goal is the student
  sees the boundary before they cross it, not only after.
- When the student does exceed the limit, keep some form of explicit feedback message, but you
  can shorten it now that the visual makes the boundary obvious.

## 3. Trial progress indicator
- Show current trial progress near the data table, e.g. "Trial 3 of 5".
- Once the target trial count is reached, change the "Next Trial" button's label/behavior to
  something like "Finish & Analyze" that moves the student to the graph/results step, instead
  of silently allowing indefinite extra trials (or, if unlimited trials should remain allowed,
  keep "Next Trial" available but add a clearly separate "Finish & Analyze" button once the
  minimum count is reached — pick whichever is cleaner given the current UI layout).

## 4. Tap-based oscillation counting
- Replace (or augment) the current Start/Stop free-timing approach with a tap-based counter:
  the student taps once each time the bob completes a swing back through center, incrementing
  an on-screen tally, for the target oscillation count (default 20, consistent with v1).
- The system's internal timer should be tied to these taps (i.e., N in T = elapsed_time / N
  should reflect the student's actual tap count for that trial), not a separately-running
  hidden count — the point is what the student experienced should be what gets logged.
- Show the running tally clearly during the swing (e.g. "14 / 20") so the student always knows
  where they are.

## 5. Data table improvements
- Add a trial number column (Trial #, L, T, T², plus a slot for the computed g/error later).
- Add a lightweight way to flag/discard a bad trial (e.g. a small "×" or "redo" icon per row)
  rather than only ever appending — real labs involve discarding mistimed runs, and this is a
  legitimate part of the skill being taught. A discarded trial should not count toward the
  target trial count or feed into the graph/g calculation.
- Keep this simple — no need for editing values in place, just discard-and-redo.

## 6. Live-updating g estimate
- Once at least 2 trials exist, show a running computed g and % error (vs 9.8 m/s²) prominently
  near the table/graph, updating as each new trial is added — not only revealed at the very end.
- The goal is the student watches the estimate stabilize/improve as more data points are added.
  Make this visually prominent (bigger/bolder than table rows), not buried in the graph.

## 7. Predict-observe-explain step
- Before each trial's timing step (after length is confirmed, before angle/start), add a quick
  prediction prompt: "As L increases, do you think T will go up, go down, or stay the same?"
  with three tappable options (Up / Down / Same) — no free text input.
- After that trial's T is logged, briefly reveal whether the prediction was correct against the
  actual trend seen so far (comparing to the previous trial if one exists; for the very first
  trial, skip the reveal since there's nothing to compare against yet, or just acknowledge the
  prediction was recorded).
- Keep this fast and lightweight — it should not meaningfully slow down the trial loop, just add
  a brief think-before-you-act beat.

## 8. "Why" explanation on angle-limit violation
- The first time (per session) a student exceeds the ~15° limit, show a short explanatory note
  distinct from the routine rejection message — something like: "Above about 15°, the simple
  formula for a pendulum's period starts to become inaccurate, because it relies on an
  approximation that only holds for small swings." Keep it short, plain-language, no heavy
  notation required.
- This should show once (or rarely) rather than every single time the student nudges past the
  limit repeatedly, to avoid being naggy — use reasonable judgement on frequency (e.g. show it
  the first time in a session, then fall back to the short routine message afterward).

## 9. Closing reflection screen
- After the graph/results step (computed g, % error shown), add a short reflection screen with:
  - The final computed g and % error restated clearly.
  - A prompt like: "Why might your measured value differ from 9.8 m/s²?"
  - 2–3 tappable common-cause options (e.g. "Reaction time when tapping/timing", "Angle was too
    large in some trials", "Friction/air resistance") — no free text required, this should stay
    lightweight.
  - No right/wrong grading needed on this — it's a reflection prompt, not a quiz question; all
    options are legitimate real causes.

## UI/UX polish expectations
This version may be shown to supervisors — treat visual presentation as part of the task, not
an afterthought:
- Keep a consistent visual language across all new UI elements (readouts, progress indicator,
  angle overlay, reflection screen) and the existing buttons/table/instructions — same color
  palette, font, and button style as what's already built.
- Make sure new UI elements don't crowd or overlap the existing 3D scene view or the data table;
  test at the same resolution/aspect ratio as the current APK build.
- Instruction text at the top of the screen should update per step (as it already does) —
  extend this pattern to the new steps (prediction prompt, reflection screen) rather than
  introducing a different instructional style.
- Where reasonable, add subtle transitions/animations (e.g. table row appearing, angle overlay
  fading in) rather than instant pop-in — keep these lightweight, not a performance risk on
  target mobile hardware.

## Documentation requirement (per CLAUDE.md)
Any new or modified script must have its summary comment at the top updated to reflect the new
behavior. Add comments explaining the reasoning behind the prediction/reflection logic and the
angle-overlay thresholds specifically, since these encode pedagogical intent that isn't obvious
from the code alone.

## Explicitly out of scope for this task
- No new practicals, subjects, or VR/headset integration.
- No backend/save-sync.
- No grading/scoring system for the reflection or prediction prompts — these are for engagement
  and reasoning, not assessment.

## Definition of done
A student can complete the full v1 flow with all of the above layered in: seeing live readouts
while dragging, seeing the angle-limit visually before crossing it, tapping to count
oscillations, making a prediction before each trial and getting a brief reveal, watching g/%
error update live as trials are added, discarding a bad trial if needed, seeing "Trial X of 5"
progress, and ending on a reflection screen after the graph — all styled consistently with the
existing UI, on the same target hardware/resolution as the current APK build.
