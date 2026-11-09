using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class LabFirstPersonMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 2.5f;
    [SerializeField] private float gravity = -9.81f;

    [Header("Look Settings")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float touchLookSensitivity = 0.12f;
    [SerializeField] private float mouseLookSensitivity = 2.0f;
    [SerializeField] private float minPitch = -40f;
    [SerializeField] private float maxPitch = 60f;

    [Header("Interaction Lock")]
    [Tooltip("When true, camera freelook is disabled so touches drag the wire instead.")]
    public bool isWireHeld = false;

    [Header("Fixed Android UI Joystick")]
    [SerializeField] private RectTransform joystickBackground;
    [SerializeField] private RectTransform joystickHandle;
    [SerializeField] private float joystickRadius = 75f;

    private CharacterController controller;
    private Vector3 velocity;
    private float pitch = 15f;
    private float yaw = 0f;

    // Touch & Pointer tracking
    private int moveFingerId = -1;
    private int lookFingerId = -1;
    private Vector2 moveInput;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        Vector3 rot = cameraTransform.eulerAngles;
        pitch = rot.x;
        yaw = rot.y;

        if (joystickBackground != null)
        {
            joystickBackground.gameObject.SetActive(true);
            if (joystickHandle != null)
            {
                joystickHandle.anchoredPosition = Vector2.zero;
            }
        }
    }

    private void Update()
    {
        if (Input.touchCount > 0)
        {
            HandleTouchMovement();
        }
#if UNITY_EDITOR
        else
        {
            HandleEditorMovement();
        }
#endif
        ApplyGravityAndMove();
    }

    private void HandleTouchMovement()
    {
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);

            // --- FIXED JOYSTICK REGION (LEFT SIDE) ---
            if (touch.position.x < Screen.width * 0.5f)
            {
                if (touch.phase == TouchPhase.Began && moveFingerId == -1)
                {
                    if (joystickBackground != null)
                    {
                        Vector2 bgScreenPos = RectTransformUtility.WorldToScreenPoint(null, joystickBackground.position);
                        float distanceToCenter = Vector2.Distance(touch.position, bgScreenPos);

                        if (distanceToCenter <= joystickRadius * 1.5f)
                        {
                            moveFingerId = touch.fingerId;
                            UpdateJoystickInput(touch.position);
                        }
                    }
                }
                else if (touch.fingerId == moveFingerId)
                {
                    if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                    {
                        UpdateJoystickInput(touch.position);
                    }
                    else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                    {
                        ResetMoveTouch();
                    }
                }
            }
            // --- RIGHT HALF: SWIPE TO LOOK (DISABLED IF WIRE IS HELD) ---
            else
            {
                // If wire is picked up, cancel any ongoing look touch and exit
                if (isWireHeld)
                {
                    lookFingerId = -1;
                    continue;
                }

                if (touch.phase == TouchPhase.Began && lookFingerId == -1)
                {
                    lookFingerId = touch.fingerId;
                }
                else if (touch.fingerId == lookFingerId)
                {
                    if (touch.phase == TouchPhase.Moved)
                    {
                        yaw += touch.deltaPosition.x * touchLookSensitivity;
                        pitch -= touch.deltaPosition.y * touchLookSensitivity;
                        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

                        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                        cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
                    }
                    else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                    {
                        lookFingerId = -1;
                    }
                }
            }
        }
    }

    private void UpdateJoystickInput(Vector2 currentTouchPos)
    {
        if (joystickBackground == null) return;

        Vector2 bgScreenPos = RectTransformUtility.WorldToScreenPoint(null, joystickBackground.position);
        Vector2 delta = currentTouchPos - bgScreenPos;

        moveInput = Vector2.ClampMagnitude(delta, joystickRadius) / joystickRadius;

        if (joystickHandle != null)
        {
            joystickHandle.anchoredPosition = moveInput * joystickRadius;
        }
    }

    private void ResetMoveTouch()
    {
        moveFingerId = -1;
        moveInput = Vector2.zero;

        if (joystickHandle != null)
        {
            joystickHandle.anchoredPosition = Vector2.zero;
        }
    }

    private void HandleEditorMovement()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        moveInput = new Vector2(h, v).normalized;

        // Mouse look disabled if holding wire
        if (!isWireHeld && Input.GetMouseButton(1))
        {
            yaw += Input.GetAxis("Mouse X") * mouseLookSensitivity;
            pitch -= Input.GetAxis("Mouse Y") * mouseLookSensitivity;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }
    }

    private void ApplyGravityAndMove()
    {
        Vector3 move = (transform.forward * moveInput.y + transform.right * moveInput.x) * walkSpeed;

        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move((move + velocity) * Time.deltaTime);
    }

    public void SetWireHeld(bool held)
    {
        isWireHeld = held;
        if (held)
        {
            lookFingerId = -1;
        }
    }
}