using System;
using UnityEngine;

/*
 * Layer: Interaction
 * Purpose: Device-agnostic contract for how a student expresses input in a practical.
 * Concrete implementations (mouse/touch today, gaze+tap or a Bluetooth controller later)
 * raise these events; nothing above this layer should know which device produced them.
 *
 * Talks to: implemented by MouseTouchInput (and future VR input scripts). Consumed by
 * interaction-layer "handle" components such as DraggableBobHandle, never directly by the
 * practical logic layer.
 */
public interface IPracticalInput
{
    // Fired when the student presses/taps down on something in the 3D scene. The GameObject
    // is whatever collider was hit, so a handle component on that object can decide whether
    // it cares about being selected.
    event Action<GameObject> PointerDown;

    // Fired every frame the pointer/finger moves while held down, carrying the screen-space
    // delta in pixels since the last frame. Handles translate this into whatever domain value
    // they represent (length, angle, etc.) - that translation is an interaction-layer concern
    // because it depends on camera/screen geometry, not on the experiment's meaning of it.
    event Action<Vector2> PointerDrag;

    // Fired when the student releases the pointer/finger.
    event Action PointerUp;
}
