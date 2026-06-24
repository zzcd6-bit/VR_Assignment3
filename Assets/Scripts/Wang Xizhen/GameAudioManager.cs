using UnityEngine;

public class GameAudioManager : MonoBehaviour
{
    public static GameAudioManager Instance { get; private set; }

    [Header("Audio Source")]
    public AudioSource audioSource;

    [Header("Pickup Sound")]
    public AudioClip pickupSound;
    public float pickupVolume = 1f;

    private void Awake()
    {
        Instance = this;

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;
        }
    }

    public void PlayPickupSound()
    {
        if (audioSource == null || pickupSound == null)
        {
            return;
        }

        audioSource.PlayOneShot(pickupSound, pickupVolume);
    }
}