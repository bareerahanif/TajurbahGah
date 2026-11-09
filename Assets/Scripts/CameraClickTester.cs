
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraClickTester : MonoBehaviour
{
    [SerializeField] private Camera sceneCamera;

    private void Awake()
    {
        if (sceneCamera == null)
            sceneCamera = GetComponent<Camera>();

        if (sceneCamera == null)
            sceneCamera = Camera.main;
    }

    private void Update()
    {
        if (Mouse.current == null)
            return;

        if (!Mouse.current.leftButton.wasPressedThisFrame)
            return;

        if (sceneCamera == null)
        {
            Debug.LogWarning("CameraClickTester: No camera assigned.");
            return;
        }

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Ray ray = sceneCamera.ScreenPointToRay(mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
        {
            Debug.Log("Ray hit: " + hit.collider.gameObject.name);

            CuSO4Clickable clickable =
                hit.collider.GetComponentInParent<CuSO4Clickable>();

            if (clickable != null)
            {
                clickable.HandleClick();
            }
            else
            {
                Debug.Log("This object has no CuSO4Clickable component.");
            }
        }
        else
        {
            Debug.Log("No collider hit.");
        }
    }
}
