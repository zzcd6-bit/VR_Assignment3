using UnityEngine;

public class PlayController : MonoBehaviour
{
    [Header("Look Settings")]
    public float mouseSensitivity = 2f;
    public float minLookAngle = -80f;
    public float maxLookAngle = 80f;
    public float smoothTime = 0.03f;

    [Header("Mouse Lock")]
    public bool lockCursorOnStart = true;

    private float targetYaw;
    private float targetPitch;

    private float currentYaw;
    private float currentPitch;

    private float yawVelocity;
    private float pitchVelocity;

    void Start()
    {
        Vector3 startEuler = transform.localEulerAngles;

        targetYaw = startEuler.y;
        currentYaw = targetYaw;

        targetPitch = startEuler.x;
        if (targetPitch > 180f)
        {
            targetPitch -= 360f;
        }

        currentPitch = targetPitch;

        if (lockCursorOnStart)
        {
            LockCursor();
        }
    }

    void Update()
    {
        LookAround();
        HandleCursorLock();
    }

    void LookAround()
    {
        float mouseX = Input.GetAxisRaw("Mouse X");
        float mouseY = Input.GetAxisRaw("Mouse Y");

        targetYaw += mouseX * mouseSensitivity;
        targetPitch -= mouseY * mouseSensitivity;

        targetPitch = Mathf.Clamp(targetPitch, minLookAngle, maxLookAngle);

        currentYaw = Mathf.SmoothDampAngle(
            currentYaw,
            targetYaw,
            ref yawVelocity,
            smoothTime
        );

        currentPitch = Mathf.SmoothDampAngle(
            currentPitch,
            targetPitch,
            ref pitchVelocity,
            smoothTime
        );

        transform.localRotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
    }

    void HandleCursorLock()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            UnlockCursor();
        }

        if (Input.GetMouseButtonDown(0))
        {
            LockCursor();
        }
    }

    void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}