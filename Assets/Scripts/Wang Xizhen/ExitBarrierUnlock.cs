using UnityEngine;

public class ExitBarrierUnlock : MonoBehaviour
{
    [Header("Barrier")]
    public GameObject barrierObject;

    [Header("Optional")]
    public AudioSource unlockSound;
    public GameObject unlockEffect;

    public void UnlockBarrier()
    {
        if (unlockSound != null)
        {
            unlockSound.Play();
        }

        if (unlockEffect != null)
        {
            Instantiate(unlockEffect, transform.position, Quaternion.identity);
        }

        if (barrierObject != null)
        {
            barrierObject.SetActive(false);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}