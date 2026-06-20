using System.Collections;
using UnityEngine;

public class MazePushDoor : MonoBehaviour
{
    public enum DoorOpenMode
    {
        Swing,
        SlideLocalRight,
        SlideUp
    }

    [Header("Door")]
    public Transform doorPart;
    public DoorOpenMode openMode = DoorOpenMode.Swing;

    [Header("Swing Setting")]
    public float openAngle = 90f;

    [Header("Slide Setting")]
    public float slideDistance = 1.5f;

    [Header("Movement")]
    public float openSpeed = 4f;
    public bool closeAgain = false;
    public float closeDelay = 2f;

    [Header("Audio")]
    public AudioSource openSound;

    private Quaternion closedRotation;
    private Quaternion openRotation;
    private Vector3 closedPosition;
    private Vector3 openPosition;

    private bool isOpen = false;
    private bool isMoving = false;
    private bool initialized = false;

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

        closedRotation = doorPart.localRotation;
        closedPosition = doorPart.localPosition;

        openRotation = closedRotation * Quaternion.Euler(0f, openAngle, 0f);
        openPosition = closedPosition + GetSlideDirection() * slideDistance;

        initialized = true;
    }

    public void PushOpen(Transform activator = null)
    {
        CacheInitialState();

        if (isOpen || isMoving)
        {
            return;
        }

        if (openMode == DoorOpenMode.Swing && activator != null)
        {
            float side = Vector3.Dot(transform.right, activator.position - transform.position);
            float direction = side >= 0f ? -1f : 1f;
            openRotation = closedRotation * Quaternion.Euler(0f, openAngle * direction, 0f);
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

        if (openMode == DoorOpenMode.Swing)
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

        if (closeAgain)
        {
            yield return new WaitForSeconds(closeDelay);
            StartCoroutine(CloseRoutine());
        }
    }

    private IEnumerator CloseRoutine()
    {
        isMoving = true;

        if (openMode == DoorOpenMode.Swing)
        {
            while (Quaternion.Angle(doorPart.localRotation, closedRotation) > 0.5f)
            {
                doorPart.localRotation = Quaternion.RotateTowards(
                    doorPart.localRotation,
                    closedRotation,
                    openSpeed * 80f * Time.deltaTime
                );

                yield return null;
            }

            doorPart.localRotation = closedRotation;
        }
        else
        {
            while (Vector3.Distance(doorPart.localPosition, closedPosition) > 0.01f)
            {
                doorPart.localPosition = Vector3.MoveTowards(
                    doorPart.localPosition,
                    closedPosition,
                    openSpeed * Time.deltaTime
                );

                yield return null;
            }

            doorPart.localPosition = closedPosition;
        }

        isOpen = false;
        isMoving = false;
    }

    private Vector3 GetSlideDirection()
    {
        switch (openMode)
        {
            case DoorOpenMode.SlideLocalRight:
                return Vector3.right;

            case DoorOpenMode.SlideUp:
                return Vector3.up;

            default:
                return Vector3.zero;
        }
    }
}