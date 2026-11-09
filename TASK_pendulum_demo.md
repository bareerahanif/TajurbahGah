# Task: Simple Pendulum Practical — Working Demo (Non-VR)

## Goal for this task
Build ONE complete, working practical — the simple pendulum experiment (measuring 'g') — as a
Unity 3D scene using mouse/touch input (no VR yet). This is the vertical slice that proves out
the layered architecture and the trial-loop pattern described in CLAUDE.md. VR input will be
swapped in later; do not build this in a way that hardcodes mouse/touch into the logic layer.

Read CLAUDE.md first for the mandatory architecture (interaction / logic / UI layers), the
physics rules, and the documentation requirements — they apply fully to this task.

## The experiment being simulated
Real lab procedure: measure the period of a simple pendulum for several different string
lengths, plot T² against L, and use the slope of that line to calculate g, comparing it to
9.8 m/s².

## Step-by-step student flow to implement

1. **Set length (L)** — student sets the pendulum's string length (e.g. by dragging the bob up
   or down along the string). This value should be readable/loggable.
2. **Set displacement angle** — student drags the bob sideways to set the starting angle.
   - Enforce a small-angle limit (~15°). If the student tries to exceed it, do not silently
     clamp — show explicit feedback explaining why (small-angle approximation requirement),
     per CLAUDE.md's feedback rule.
3. **Time N oscillations** — student clicks/taps "Start" to release the pendulum, watches it
   swing, and clicks/taps "Stop" after counting an agreed number of oscillations (default: 20 —
   confirm this is a reasonable number, adjust if needed). Do not auto-time this for the
   student; the manual start/stop is intentional (mirrors the real skill being practiced).
4. **Log the trial** — on stop, compute the period T = elapsed_time / N and log a row
   (L, T, T²) into a data table.
5. **Repeat** — student repeats steps 1–4 for approximately 5 different lengths. The trial loop
   controller (see CLAUDE.md) should make "repeat this loop with new parameters" a natural,
   reusable operation, not a copy-pasted sequence.
6. **Analyze** — once trials are complete, plot T² (y-axis) vs L (x-axis) using Unity UI (e.g.
   LineRenderer or UI Image-based plotting — charting library not in use for now). Compute the
   best-fit slope, derive g from the pendulum relation (T² = (4π²/g)·L, so slope = 4π²/g),
   display the computed g and its % error versus 9.8 m/s².

## Physics implementation
- Do not use Unity Rigidbody/PhysX for the swing — script the motion directly.
- For this demo, either:
  (a) small-angle SHM: θ(t) = θ₀·cos(√(g/L)·t), which is exact enough given we're enforcing the
      small-angle limit anyway, or
  (b) numerically integrate θ'' = -(g/L)·sinθ if you want it to hold up even near the angle
      limit.
  Pick (a) unless there's a clear reason to prefer (b) — simpler and consistent with the
  enforced small-angle constraint. Note the choice and reasoning in the script's summary
  comment.
- Use a fixed, known "true" g (9.8 m/s²) to drive the simulated motion — the student's job is
  to recover this value experimentally through their measurements, not for the simulation to
  magically know their intended answer.

## Scope boundaries for this task (do not expand beyond this)
- No VR/headset integration yet — mouse/touch/click only, but built behind the abstracted
  interaction interface from CLAUDE.md so VR can be swapped in later without touching logic.
- No other practicals, subjects, or content beyond this one pendulum experiment.
- No backend, no save/sync — session-local is fine.
- Build the scene in proper 3D (stand, string, bob, ground/table) with a simple orbit/fixed
  camera — not a 2D mockup — since the 3D assets and layout carry forward into the VR version.

## Definition of done for this task
A student can, in one continuous session:
set a length → set a valid displacement angle (rejected with feedback if too large) → start/stop
a timer across a counted number of oscillations → see the trial (L, T, T²) appended to a visible
data table → repeat for ~5 lengths → see a T² vs L plot with a computed g and % error against
9.8 m/s², rendered via Unity UI.

## Documentation reminder for this task specifically
Given this is the first practical and sets the pattern for 14 more, err on the side of *more*
explanatory comments here than you would for later, similar practicals — especially in the
trial-loop controller and the interaction-interface abstraction, since those are the pieces
being reused going forward.
