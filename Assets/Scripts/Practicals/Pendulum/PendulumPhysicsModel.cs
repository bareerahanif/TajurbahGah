using UnityEngine;

/*
 * Layer: Practical logic (pure math - no MonoBehaviour, no input/UI references)
 * Purpose: Simple-harmonic-motion model for the pendulum's swing angle over time.
 *
 * Assumption to verify against the real lab manual later: we use the small-angle SHM
 * closed-form solution theta(t) = theta0 * cos(sqrt(g/L) * t) rather than numerically
 * integrating theta'' = -(g/L) sin(theta). This is accurate to within a fraction of a percent
 * at the enforced 15-degree limit, which is more than adequate for a teaching demo, and it is
 * exact under the same small-angle assumption the practical already requires the student to
 * respect. See TASK_pendulum_demo.md, option (a).
 */
public static class PendulumPhysicsModel
{
    // Returns the swing angle in degrees at time t (seconds) after release, given the release
    // amplitude and string length, driven by a fixed "true" gravity the student is trying to
    // recover experimentally.
    public static float AngleAtTime(float amplitudeDegrees, float lengthMeters, float gravity, float t)
    {
        float angularFrequency = Mathf.Sqrt(gravity / lengthMeters);
        return amplitudeDegrees * Mathf.Cos(angularFrequency * t);
    }

    // Theoretical period - only useful for sanity-checking in the Inspector/debugging. The
    // student's measured period always comes from their own manual stopwatch trial, never
    // from this, so the simulation never "knows" the answer it is testing them on.
    public static float TheoreticalPeriod(float lengthMeters, float gravity)
    {
        return 2f * Mathf.PI * Mathf.Sqrt(lengthMeters / gravity);
    }
}
