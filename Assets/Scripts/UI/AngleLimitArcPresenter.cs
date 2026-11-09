using UnityEngine;

/*
 * Layer: UI / presentation
 * Purpose: Draws a curved boundary (green within the small-angle limit, red beyond) around the
 * pivot showing the pendulum's valid displacement range, live during angle-setting
 * (TASK_pendulum_demo_v2.md item 2). Deliberately simple LineRenderer arcs rather than a filled
 * protractor mesh/shader - sufficient for a teaching demo without adding rendering complexity.
 * The arcs are positioned at the pivot but do NOT rotate with it (they represent the static
 * allowed range, not the bob's current position).
 * Talks to: reads PendulumExperimentController.CurrentStep/CurrentLengthMeters/maxAngleDegrees;
 * regenerates arc geometry every frame while active (cheap at this vertex count).
 */
public class AngleLimitArcPresenter : MonoBehaviour
{
    public PendulumExperimentController controller;
    public LineRenderer greenArc;        // -limit .. +limit
    public LineRenderer redArcPositive;  // +limit .. +limit+margin
    public LineRenderer redArcNegative;  // -limit-margin .. -limit
    public float radiusMargin = 0.05f;
    [Tooltip("How far past the limit (degrees) the red warning arcs extend.")]
    public float warningMarginDegrees = 15f;
    [Range(4, 64)] public int segmentsPerArc = 16;

    void Update()
    {
        bool active = controller.CurrentStep == PendulumStep.SettingAngle;
        if (greenArc.gameObject.activeSelf != active)
        {
            greenArc.gameObject.SetActive(active);
            redArcPositive.gameObject.SetActive(active);
            redArcNegative.gameObject.SetActive(active);
        }

        if (!active) return;

        // Length is locked by the time angle-setting starts, but rebuilding every frame anyway
        // keeps this robust without relying on that assumption never changing.
        float radius = controller.CurrentLengthMeters + radiusMargin;
        float limit = controller.maxAngleDegrees;

        BuildArc(greenArc, -limit, limit, radius);
        BuildArc(redArcPositive, limit, limit + warningMarginDegrees, radius);
        BuildArc(redArcNegative, -limit - warningMarginDegrees, -limit, radius);
    }

    // Builds a flat arc in the local XY plane, matching PendulumVisualPresenter's swing plane
    // and its Quaternion.Euler(0,0,angle) convention: 0 degrees points straight down (-Y),
    // positive angle rotates toward +X.
    private void BuildArc(LineRenderer line, float fromDegrees, float toDegrees, float radius)
    {
        line.positionCount = segmentsPerArc;
        line.useWorldSpace = false;
        for (int i = 0; i < segmentsPerArc; i++)
        {
            float t = (float)i / (segmentsPerArc - 1);
            float angleDeg = Mathf.Lerp(fromDegrees, toDegrees, t);
            float rad = angleDeg * Mathf.Deg2Rad;
            Vector3 point = new Vector3(Mathf.Sin(rad) * radius, -Mathf.Cos(rad) * radius, 0f);
            line.SetPosition(i, point);
        }
    }
}
