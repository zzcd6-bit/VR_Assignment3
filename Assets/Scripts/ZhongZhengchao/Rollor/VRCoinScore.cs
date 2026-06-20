using UnityEngine;

public class VRCoinScore : MonoBehaviour
{
    public VRCoasterRide rideController;

    [Header("Score")]
    public int scoreValue = 10;

    [Header("Gaze Setting")]
    public float gazeTime = 0.5f;

    private bool isLooking = false;
    private float timer = 0f;
    private bool collected = false;

    private Vector3 startPosition;
    private Quaternion startRotation;
    private Vector3 startScale;

    void Start()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;
        startScale = transform.localScale;
    }

    void Update()
    {
        if (collected)
        {
            return;
        }

        transform.Rotate(Vector3.forward, 90f * Time.deltaTime);

        if (!isLooking)
        {
            return;
        }

        if (rideController != null && !rideController.IsRiding)
        {
            return;
        }

        timer += Time.deltaTime;

        if (timer >= gazeTime)
        {
            CollectCoin();
        }
    }

    public void BeginLook()
    {
        isLooking = true;
        timer = 0f;

        Debug.Log("Looking at coin: " + gameObject.name);
    }

    public void EndLook()
    {
        isLooking = false;
        timer = 0f;
    }

    void CollectCoin()
    {
        collected = true;
        isLooking = false;
        timer = 0f;

        if (rideController != null)
        {
            rideController.AddScore(scoreValue);
        }

        gameObject.SetActive(false);
    }

    public void ResetCoin()
    {
        collected = false;
        isLooking = false;
        timer = 0f;

        transform.position = startPosition;
        transform.rotation = startRotation;
        transform.localScale = startScale;

        gameObject.SetActive(true);
    }
}