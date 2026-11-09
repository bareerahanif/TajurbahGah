using System;
using UnityEngine;
using UnityEngine.InputSystem;

/*
 * Layer: Interaction
 * Purpose: Concrete IPracticalInput implementation for mouse (editor) and touch (mobile)
 * input via Unity's Input System package. This is the ONLY script in the project allowed to
 * read Mouse/Touchscreen state directly - everything else reacts to IPracticalInput events.
 * When VR input arrives later (gaze+tap or a Bluetooth controller), a new class implementing
 * IPracticalInput is added alongside this one; no other script needs to change.
 *
 * Talks to: raises PointerDown/PointerDrag/PointerUp; consumed by DraggableBobHandle.
 */
public class MouseTouchInput : MonoBehaviour, IPracticalInput
{
    public event Action<GameObject> PointerDown;
    public event Action<Vector2> PointerDrag;
    public event Action PointerUp;

    [Tooltip("Camera used to raycast from the pointer position into the 3D scene.")]
    public Camera raycastCamera;

    private bool isPointerHeld = false;
    private Vector2 lastScreenPosition;

    void Awake()
    {
        if (raycastCamera == null) raycastCamera = Camera.main;
    }

    void Update()
    {
        if (!ReadPointerState(out Vector2 currentPosition, out bool pressedThisFrame, out bool heldThisFrame, out bool releasedThisFrame))
            return; // no mouse or touch device active this frame

        if (pressedThisFrame)
        {
            lastScreenPosition = currentPosition;
            GameObject hit = RaycastFromScreenPoint(currentPosition);
            if (hit != null)
            {
                isPointerHeld = true;
                PointerDown?.Invoke(hit);
            }
        }
        else if (isPointerHeld && heldThisFrame)
        {
            Vector2 delta = currentPosition - lastScreenPosition;
            lastScreenPosition = currentPosition;
            if (delta.sqrMagnitude > 0f)
                PointerDrag?.Invoke(delta);
        }
        else if (isPointerHeld && releasedThisFrame)
        {
            isPointerHeld = false;
            PointerUp?.Invoke();
        }
    }

    // Reads either the mouse or the first active touch, whichever is present, so the same
    // code path serves both editor testing today and mobile touch builds later.
    private bool ReadPointerState(out Vector2 position, out bool pressedThisFrame, out bool heldThisFrame, out bool releasedThisFrame)
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            var touch = Touchscreen.current.primaryTouch;
            position = touch.position.ReadValue();
            pressedThisFrame = touch.press.wasPressedThisFrame;
            heldThisFrame = true;
            releasedThisFrame = false;
            return true;
        }
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame)
        {
            position = Touchscreen.current.primaryTouch.position.ReadValue();
            pressedThisFrame = false;
            heldThisFrame = false;
            releasedThisFrame = true;
            return true;
        }

        if (Mouse.current != null)
        {
            position = Mouse.current.position.ReadValue();
            pressedThisFrame = Mouse.current.leftButton.wasPressedThisFrame;
            heldThisFrame = Mouse.current.leftButton.isPressed;
            releasedThisFrame = Mouse.current.leftButton.wasReleasedThisFrame;
            return true;
        }

        position = default;
        pressedThisFrame = false;
        heldThisFrame = false;
        releasedThisFrame = false;
        return false;
    }

    private GameObject RaycastFromScreenPoint(Vector2 screenPoint)
    {
        if (raycastCamera == null) return null;
        Ray ray = raycastCamera.ScreenPointToRay(screenPoint);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            return hit.collider.gameObject;
        return null;
    }
}
