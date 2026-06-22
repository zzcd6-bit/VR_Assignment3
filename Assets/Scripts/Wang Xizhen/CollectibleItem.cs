using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CollectibleItem : MonoBehaviour
{
    [Header("Collectible")]
    public int amount = 1;

    [Header("Visual")]
    public float rotateSpeed = 80f;
    public float floatSpeed = 1.5f;
    public float floatHeight = 0.1f;
    public bool enableFloating = true;

    [Header("Audio")]
    public AudioClip collectSound;
    public float soundVolume = 1f;

    [Header("Effect")]
    public GameObject collectEffect;

    private bool collected;
    private Vector3 startPosition;

    private void Awake()
    {
        startPosition = transform.position;

        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void Update()
    {
        RotateItem();

        if (enableFloating)
        {
            FloatItem();
        }
    }

    private void RotateItem()
    {
        transform.Rotate(Vector3.up * rotateSpeed * Time.deltaTime, Space.World);
    }

    private void FloatItem()
    {
        float newY = startPosition.y + Mathf.Sin(Time.time * floatSpeed) * floatHeight;
        transform.position = new Vector3(startPosition.x, newY, startPosition.z);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected)
        {
            return;
        }

        if (other.GetComponentInParent<PlayerCollectorMarker>() == null)
        {
            return;
        }

        Collect();
    }

    private void Collect()
    {
        collected = true;

        if (CollectibleCounter.Instance != null)
        {
            CollectibleCounter.Instance.AddCollectible(amount);
        }

        if (collectSound != null)
        {
            AudioSource.PlayClipAtPoint(collectSound, transform.position, soundVolume);
        }

        if (collectEffect != null)
        {
            Instantiate(collectEffect, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }
}