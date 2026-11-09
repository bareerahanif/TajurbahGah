using UnityEngine;

/*
 * Layer: UI / presentation
 * Purpose: Applies the experiment controller's current length/angle values to the actual scene
 * Transforms (pivot rotation, thread scale, bob position). Kept separate from
 * PendulumExperimentController so the logic layer never has to touch a Transform directly -
 * useful once this content is reused inside a VR rig with a different visual hierarchy.
 *
 * Talks to: reads CurrentLengthMeters/CurrentAngleDegrees/CurrentSwingAngleDegrees from
 * PendulumExperimentController every frame; writes to pivot/threadVisual/bobVisual Transforms.
 */
public class PendulumVisualPresenter : MonoBehaviour
{
    public PendulumExperimentController controller;
    public Transform pivot;
    public Transform threadVisual;
    public Transform bobVisual;

    [Tooltip("Radius of the bob mesh in meters, so the bob sits at pivot distance = length + radius.")]
    public float bobRadiusMeters = 0.05f;

    void LateUpdate()
    {
        if (controller == null) return;

        float angle = controller.CurrentStep == PendulumStep.Swinging
            ? controller.CurrentSwingAngleDegrees
            : controller.CurrentAngleDegrees;

        // Rotating about the pivot's local Z axis swings the bob left/right in the camera's
        // view, since the rig is laid out in the X (screen-right) / Y (screen-up) plane.
        pivot.localRotation = Quaternion.Euler(0f, 0f, angle);

        float length = controller.CurrentLengthMeters;
        float totalLength = length + bobRadiusMeters;

        // Default Unity cylinder mesh is 2 units tall, hence the /2f to convert length to scale.
        threadVisual.localScale = new Vector3(threadVisual.localScale.x, length / 2f, threadVisual.localScale.z);
        threadVisual.localPosition = new Vector3(0f, -length / 2f, 0f);
        bobVisual.localPosition = new Vector3(0f, -totalLength, 0f);
    }
}
