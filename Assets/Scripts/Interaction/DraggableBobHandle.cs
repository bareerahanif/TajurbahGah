using System;
using UnityEngine;

/*
 * Layer: Interaction
 * Purpose: Sits on the pendulum bob's collider. Listens to an IPracticalInput source and,
 * once this specific bob has been picked up, forwards the raw screen-space drag as two
 * decomposed axes (vertical = along-string, horizontal = sideways) via DragAxes. It does NOT
 * decide what those axes mean (length vs angle) - that interpretation belongs to the practical
 * logic layer (PendulumExperimentController), keeping this component reusable for any
 * drag-based handle in a future practical.
 *
 * Talks to: reads events from an IPracticalInput component (assigned in the Inspector),
 * raises DragAxes/HandleReleased for PendulumExperimentController to consume.
 */
public class DraggableBobHandle : MonoBehaviour
{
    [Tooltip("Any component implementing IPracticalInput, e.g. MouseTouchInput.")]
    public MonoBehaviour inputSource;

    // vertical = pixels moved up(+)/down(-) this frame; horizontal = pixels moved right(+)/left(-)
    public event Action<float, float> DragAxes;
    public event Action HandleReleased;

    private IPracticalInput input;
    private bool isSelected = false;

    void Awake()
    {
        input = inputSource as IPracticalInput;
        if (input == null)
            Debug.LogError("DraggableBobHandle: inputSource does not implement IPracticalInput.", this);
    }

    void OnEnable()
    {
        if (input == null) return;
        input.PointerDown += HandlePointerDown;
        input.PointerDrag += HandlePointerDrag;
        input.PointerUp += HandlePointerUp;
    }

    void OnDisable()
    {
        if (input == null) return;
        input.PointerDown -= HandlePointerDown;
        input.PointerDrag -= HandlePointerDrag;
        input.PointerUp -= HandlePointerUp;
    }

    private void HandlePointerDown(GameObject hitObject)
    {
        isSelected = hitObject == gameObject;
    }

    private void HandlePointerDrag(Vector2 screenDelta)
    {
        if (!isSelected) return;
        DragAxes?.Invoke(screenDelta.y, screenDelta.x);
    }

    private void HandlePointerUp()
    {
        if (!isSelected) return;
        isSelected = false;
        HandleReleased?.Invoke();
    }
}
