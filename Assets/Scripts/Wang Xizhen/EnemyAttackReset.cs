using System.Collections;
using UnityEngine;

public class EnemyAttackReset : MonoBehaviour
{
    [Header("Enemy Animation")]
    public Animator enemyAnimator;
    public string attackTriggerName = "Attack";

    [Header("Enemy Audio")]
    public AudioSource attackAudioSource;
    public AudioClip attackSound;
    public float attackSoundDelay = 0f;
    public float attackSoundVolume = 1f;

    [Header("Timing")]
    public float attackHitDelay = 0.7f;
    public float fadeOutDuration = 0.5f;
    public float blackScreenHoldTime = 0.3f;
    public float fadeInDuration = 1f;
    public float retriggerCooldown = 1.0f;

    [Header("Player Reset")]
    public PlayerRespawn playerRespawn;
    public ScreenFader screenFader;

    private bool isProcessing;
    private float lastTriggerTime = -999f;

    private void Awake()
    {
        if (enemyAnimator == null)
        {
            enemyAnimator = GetComponentInParent<Animator>();
        }

        if (attackAudioSource == null)
        {
            attackAudioSource = GetComponentInParent<AudioSource>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TryTriggerAttack(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryTriggerAttack(other);
    }

    private void TryTriggerAttack(Collider other)
    {
        if (isProcessing)
        {
            return;
        }

        if (Time.time - lastTriggerTime < retriggerCooldown)
        {
            return;
        }

        if (other.GetComponentInParent<PlayerCollectorMarker>() == null)
        {
            return;
        }

        StartCoroutine(AttackAndReset());
    }

    private IEnumerator AttackAndReset()
    {
        isProcessing = true;
        lastTriggerTime = Time.time;

        if (enemyAnimator != null)
        {
            enemyAnimator.SetTrigger(attackTriggerName);
        }

        if (attackSoundDelay > 0f)
        {
            yield return new WaitForSeconds(attackSoundDelay);
        }

        PlayAttackSound();

        yield return new WaitForSeconds(attackHitDelay);

        if (screenFader != null)
        {
            yield return screenFader.FadeOut(fadeOutDuration);
        }

        if (playerRespawn != null)
        {
            playerRespawn.Respawn();
        }

        yield return new WaitForSeconds(blackScreenHoldTime);

        if (screenFader != null)
        {
            yield return screenFader.FadeIn(fadeInDuration);
        }

        yield return new WaitForSeconds(retriggerCooldown);

        isProcessing = false;
    }

    private void PlayAttackSound()
    {
        if (attackAudioSource == null)
        {
            return;
        }

        if (attackSound != null)
        {
            attackAudioSource.PlayOneShot(attackSound, attackSoundVolume);
        }
        else
        {
            attackAudioSource.Play();
        }
    }
}