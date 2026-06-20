using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class HMDGazeClickInteractor : MonoBehaviour
{
    [Header("Ray")]
    public Camera targetCamera;
    public float interactDistance = 5f;
    public LayerMask interactLayer = ~0;

    [Header("Debug")]
    public bool drawDebugRay = true;

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = GetComponent<Camera>();
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    private void Update()
    {
        if (targetCamera == null)
        {
            return;
        }

        if (drawDebugRay)
        {
            Debug.DrawRay(
                targetCamera.transform.position,
                targetCamera.transform.forward * interactDistance,
                Color.red
            );
        }

        if (!ClickPressed())
        {
            return;
        }

        Ray ray = new Ray(targetCamera.transform.position, targetCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactLayer, QueryTriggerInteraction.Collide))
        {
            MazeDoorButton doorButton = hit.collider.GetComponentInParent<MazeDoorButton>();

            if (doorButton != null)
            {
                doorButton.Interact();
                return;
            }

            MazeKeyPickup keyPickup = hit.collider.GetComponentInParent<MazeKeyPickup>();

            if (keyPickup != null)
            {
                keyPickup.Interact();
                return;
            }
        }
    }

    private bool ClickPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            return true;
        }

        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            return true;
        }

        if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)
        {
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetMouseButtonDown(0))
        {
            return true;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            return true;
        }

        if (Input.GetButtonDown("Fire1"))
        {
            return true;
        }
#endif

        return false;
    }
}