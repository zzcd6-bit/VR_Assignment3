using UnityEngine;

public class PlayerRespawn : MonoBehaviour
{
    [Header("Spawn Point")]
    public Transform startPoint;

    [Header("XR Camera")]
    public Transform cameraTransform;

    private CharacterController characterController;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    public void Respawn()
    {
        if (startPoint == null)
        {
            Debug.LogWarning("Start Point is not assigned.");
            return;
        }

        if (cameraTransform == null)
        {
            Debug.LogWarning("Camera Transform is not assigned.");
            return;
        }

        if (characterController != null)
        {
            characterController.enabled = false;
        }

        
        Vector3 cameraForward = cameraTransform.forward;
        cameraForward.y = 0f;
        cameraForward.Normalize();

        Vector3 targetForward = startPoint.forward;
        targetForward.y = 0f;
        targetForward.Normalize();

        if (cameraForward.sqrMagnitude > 0.001f && targetForward.sqrMagnitude > 0.001f)
        {
            float angle = Vector3.SignedAngle(cameraForward, targetForward, Vector3.up);
            transform.RotateAround(cameraTransform.position, Vector3.up, angle);
        }

        
        Vector3 cameraOffsetFromOrigin = cameraTransform.position - transform.position;
        cameraOffsetFromOrigin.y = 0f;

        Vector3 newOriginPosition = startPoint.position - cameraOffsetFromOrigin;

        
        newOriginPosition.y = startPoint.position.y;

        transform.position = newOriginPosition;

        if (characterController != null)
        {
            characterController.enabled = true;
        }

        Physics.SyncTransforms();

        Debug.Log("Player respawned. Camera aligned to start point.");
    }
}