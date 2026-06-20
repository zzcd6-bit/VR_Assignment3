using System.Collections;
using UnityEngine;

public class MazeKeyDoor : MonoBehaviour
{
    public enum KeyDoorOpenMode
    {
        SlideUp,
        SlideLocalRight,
        Swing
    }

    [Header("Key")]
    public string requiredKeyId = "GoldKey";

    [Header("Door")]
    public Transform doorPart;
    public KeyDoorOpenMode openMode = KeyDoorOpenMode.SlideUp;

    [Header("Open Setting")]
    public float openDistance = 2.8f;
    public float openAngle = 90f;
    public float openSpeed = 3f;

    [Header("Audio")]
    public AudioSource openSound;
    public AudioSource lockedSound;

    private Vector3 closedPosition;
    private Vector3 openPosition;
    private Quaternion closedRotation;
    private Quaternion openRotation;

    private bool initialized = false;
    private bool isOpen = false;
    private bool isMoving = false;

    private void Awake()
    {
        CacheInitialState();
    }

    private void CacheInitialState()
    {
        if (initialized)
        {
            return;
        }

        if (doorPart == null)
        {
            doorPart = transform;
        }

        closedPosition = doorPart.localPosition;
        closedRotation = doorPart.localRotation;

        openPosition = closedPosition + GetOpenDirection() * openDistance;
        openRotation = closedRotation * Quaternion.Euler(0f, openAngle, 0f);

        initialized = true;
    }

    public void TryOpen(Transform activator)
    {
        CacheInitialState();

        if (isOpen || isMoving)
        {
            return;
        }

        MazeInventory inventory = null;

        if (activator != null)
        {
            inventory = activator.GetComponentInParent<MazeInventory>();
        }

        if (inventory == null || !inventory.HasKey(requiredKeyId))
        {
            if (lockedSound != null)
            {
                lockedSound.Play();
            }

            Debug.Log("Locked door. Need key: " + requiredKeyId);
            return;
        }

        OpenDoor();
    }

    public void OpenDoor()
    {
        if (isOpen || isMoving)
        {
            return;
        }

        if (openSound != null)
        {
            openSound.Play();
        }

        StartCoroutine(OpenRoutine());
    }

    private IEnumerator OpenRoutine()
    {
        isMoving = true;

        if (openMode == KeyDoorOpenMode.Swing)
        {
            while (Quaternion.Angle(doorPart.localRotation, openRotation) > 0.5f)
            {
                doorPart.localRotation = Quaternion.RotateTowards(
                    doorPart.localRotation,
                    openRotation,
                    openSpeed * 80f * Time.deltaTime
                );

                yield return null;
            }

            doorPart.localRotation = openRotation;
        }
        else
        {
            while (Vector3.Distance(doorPart.localPosition, openPosition) > 0.01f)
            {
                doorPart.localPosition = Vector3.MoveTowards(
                    doorPart.localPosition,
                    openPosition,
                    openSpeed * Time.deltaTime
                );

                yield return null;
            }

            doorPart.localPosition = openPosition;
        }

        isOpen = true;
        isMoving = false;
    }

    private Vector3 GetOpenDirection()
    {
        switch (openMode)
        {
            case KeyDoorOpenMode.SlideLocalRight:
                return Vector3.right;

            case KeyDoorOpenMode.SlideUp:
                return Vector3.up;

            default:
                return Vector3.up;
        }
    }
}