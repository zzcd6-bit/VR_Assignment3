using UnityEngine;

public class VRRideLookButton : MonoBehaviour
{
    public VRCoasterRide rideController;

    [Header("Button Setting")]
    public bool isReplayButton = false;

    [Header("Gaze Setting")]
    public float gazeTime = 2f;

    private bool isLooking = false;
    private float timer = 0f;
    private bool hasTriggered = false;

    void Update()
    {
        if (!isLooking || hasTriggered)
        {
            return;
        }

        timer += Time.deltaTime;

        if (timer >= gazeTime)
        {
            TriggerButton();
        }
    }

    public void BeginLook()
    {
        isLooking = true;
        timer = 0f;
        hasTriggered = false;

        Debug.Log("Looking at button: " + gameObject.name);
    }

    public void EndLook()
    {
        isLooking = false;
        timer = 0f;

        Debug.Log("Exit button: " + gameObject.name);
    }

    void TriggerButton()
    {
        hasTriggered = true;
        isLooking = false;

        if (rideController == null)
        {
            Debug.LogWarning("√ª”–Õœ»Î VRCoasterRide°£");
            return;
        }

        if (isReplayButton)
        {
            rideController.RestartRide();
        }
        else
        {
            rideController.StartRide();
        }
    }
}