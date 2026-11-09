using UnityEngine;

public class LabInteractionRaycaster : MonoBehaviour
{
    [Header("Core References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private PlatinumWireTool wireTool;
    [SerializeField] private BunsenBurnerFX burnerFX;
    [SerializeField] private LabFirstPersonMovement playerMovement;
    [SerializeField] private LayerMask interactableLayer;
    [SerializeField] private float tableSurfaceHeight = 0.85f;

    private bool isHoldingWire = false;
    private int activeFingerId = -1;
    private Plane dragPlane;

    private void Awake()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (playerMovement == null)
            playerMovement = FindFirstObjectByType<LabFirstPersonMovement>();

        if (wireTool != null && burnerFX != null)
            wireTool.SetBurnerReference(burnerFX);

        dragPlane = new Plane(Vector3.up, new Vector3(0f, tableSurfaceHeight + 0.08f, 0f));
    }

    private void Update()
    {
#if UNITY_EDITOR
        HandleEditorInput();
#else
        HandleMobileTouch();
#endif
    }

    private void HandleEditorInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            TryGrabWire(Input.mousePosition);
        }
        else if (Input.GetMouseButton(0) && isHoldingWire)
        {
            DragWire(Input.mousePosition);
        }
        else if (Input.GetMouseButtonUp(0) && isHoldingWire)
        {
            ReleaseWire();
        }
    }

    private void HandleMobileTouch()
    {
        // Loop over all active touches so multi-touch (joystick + grab) works cleanly
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);

            // 1. Try to grab if we aren't holding the wire yet
            if (!isHoldingWire)
            {
                if (touch.phase == TouchPhase.Began)
                {
                    // Ignore touches originating on UI elements (like the virtual joystick or HUD)
                    if (UnityEngine.EventSystems.EventSystem.current != null &&
                        UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                    {
                        continue;
                    }

                    if (TryGrabWire(touch.position))
                    {
                        activeFingerId = touch.fingerId;
                        break;
                    }
                }
            }
            // 2. If holding, track ONLY the finger that initiated the grab
            else if (touch.fingerId == activeFingerId)
            {
                if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                {
                    DragWire(touch.position);
                }
                else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    ReleaseWire();
                }
                break;
            }
        }
    }

    private bool TryGrabWire(Vector2 screenPosition)
    {
#if UNITY_EDITOR
        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            return false;
        }
#endif

        Ray ray = playerCamera.ScreenPointToRay(screenPosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 10.0f, interactableLayer))
        {
            PlatinumWireTool tool = hit.collider.GetComponentInParent<PlatinumWireTool>();
            if (tool != null)
            {
                isHoldingWire = true;
                wireTool.GrabTool(hit.point);

                // Lock camera freelook in the movement script
                if (playerMovement != null)
                {
                    playerMovement.SetWireHeld(true);
                }

                Debug.Log("Successfully grabbed wire tool!");
                return true;
            }
        }

        return false;
    }

    private void DragWire(Vector2 screenPosition)
    {
        Ray ray = playerCamera.ScreenPointToRay(screenPosition);
        if (dragPlane.Raycast(ray, out float enter))
        {
            Vector3 targetPosition = ray.GetPoint(enter);
            wireTool.UpdateDragPosition(targetPosition);
        }
    }

    private void ReleaseWire()
    {
        isHoldingWire = false;
        activeFingerId = -1;

        if (wireTool != null)
        {
            wireTool.ReleaseTool();
        }

        // Re-enable camera freelook
        if (playerMovement != null)
        {
            playerMovement.SetWireHeld(false);
        }
    }
}