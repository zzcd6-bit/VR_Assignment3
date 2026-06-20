using UnityEngine;

public class MazeDoorButton : MonoBehaviour
{
    [Header("Target Door")]
    public MazePushDoor pushDoor;
    public MazeKeyDoor keyDoor;

    [Header("Button Visual")]
    public Transform buttonVisual;
    public float pressDistance = 0.04f;
    public float pressSpeed = 8f;

    [Header("Audio")]
    public AudioSource buttonSound;
    public AudioSource lockedSound;

    private Vector3 originalLocalPosition;
    private bool pressed = false;

    private void Awake()
    {
        if (buttonVisual == null)
        {
            buttonVisual = transform;
        }

        originalLocalPosition = buttonVisual.localPosition;
    }

    public void Interact()
    {
        if (pressed)
        {
            return;
        }

        if (buttonSound != null)
        {
            buttonSound.Play();
        }

        if (keyDoor != null)
        {
            TryOpenKeyDoor();
        }
        else if (pushDoor != null)
        {
            pressed = true;
            pushDoor.PushOpen(transform);
            StartCoroutine(PressAnimation());
        }
    }

    private void TryOpenKeyDoor()
    {
        MazeInventory inventory = FindObjectOfType<MazeInventory>();

        if (inventory == null || !inventory.HasKey(keyDoor.requiredKeyId))
        {
            if (lockedSound != null)
            {
                lockedSound.Play();
            }
            else if (keyDoor.lockedSound != null)
            {
                keyDoor.lockedSound.Play();
            }

            Debug.Log("Need key: " + keyDoor.requiredKeyId);
            return;
        }

        pressed = true;
        keyDoor.OpenDoor();
        StartCoroutine(PressAnimation());
    }

    private System.Collections.IEnumerator PressAnimation()
    {
        Vector3 pressedPosition = originalLocalPosition - Vector3.forward * pressDistance;

        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * pressSpeed;
            buttonVisual.localPosition = Vector3.Lerp(originalLocalPosition, pressedPosition, t);
            yield return null;
        }
    }
}