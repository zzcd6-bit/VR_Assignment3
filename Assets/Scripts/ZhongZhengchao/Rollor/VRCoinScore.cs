using UnityEngine;

public class VRCoinScore : MonoBehaviour
{
    public VRCoasterRide rideController;

    [Header("Score")]
    public int scoreValue = 10;

    [Header("Coin Animation")]
    public float rotateSpeed = 90f;

    [Header("Coin Sound")]
    public AudioClip collectSound;
    [Range(0f, 1f)]
    public float collectSoundVolume = 1f;

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

        transform.Rotate(Vector3.forward, rotateSpeed * Time.deltaTime);
    }

    public void CollectCoinFromInteractableEnter()
    {
        if (collected)
        {
            return;
        }

        // 测试阶段先注释掉，防止 IsRiding=false 导致你以为没点到
        /*
        if (rideController != null && !rideController.IsRiding)
        {
            Debug.Log("Ride is not running, coin cannot be collected.");
            return;
        }
        */

        CollectCoin();
    }

    void CollectCoin()
    {
        collected = true;

        if (rideController != null)
        {
            rideController.AddScore(scoreValue);
        }

        // 播放获取 Coin 的音效
        if (collectSound != null)
        {
            AudioSource.PlayClipAtPoint(
                collectSound,
                transform.position,
                collectSoundVolume
            );
        }

        Debug.Log("Coin Collected: " + gameObject.name);

        gameObject.SetActive(false);
    }

    public void ResetCoin()
    {
        collected = false;

        transform.position = startPosition;
        transform.rotation = startRotation;
        transform.localScale = startScale;

        gameObject.SetActive(true);
    }
}