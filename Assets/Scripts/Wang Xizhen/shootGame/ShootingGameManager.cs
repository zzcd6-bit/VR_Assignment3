using System.Collections;
using TMPro;
using UnityEngine;

public class ShootingGameManager : MonoBehaviour
{
    public static ShootingGameManager Instance { get; private set; }

    [Header("Game Rules")]
    public int maxAmmo = 10;
    public int maxScore = 7;

    [Header("Runtime")]
    public int currentAmmo;
    public int currentScore;
    public bool gameEnded;

    [Header("UI")]
    public TMP_Text scoreText;
    public TMP_Text ammoText;
    public TMP_Text resultText;

    [Header("Score Colors")]
    public Color lowScoreColor = Color.red;
    public Color midScoreColor = Color.green;
    public Color perfectScoreColor = new Color(1f, 0.72f, 0.05f);

    [Header("Result NPC")]
    public GameObject resultNpcObject;
    public Animator npcAnimator;
    public string danceTriggerName = "Dance";
    public string sighTriggerName = "Sigh";

    [Header("Result Audio")]
    public AudioSource resultAudioSource;

    public AudioClip celebrationSound;
    public AudioClip sighSound;
    public AudioClip confettiSound;

    public float celebrationVolume = 1f;
    public float sighVolume = 1f;
    public float confettiVolume = 1f;

    [Header("Perfect Celebration")]
    public ParticleSystem[] confettiEffects;
    public float confettiInterval = 0.5f;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        StartGame();
    }

    public void StartGame()
    {
        currentAmmo = maxAmmo;
        currentScore = 0;
        gameEnded = false;

        HideResultNPC();
        UpdateUI();

        if (resultText != null)
        {
            resultText.text = "";
        }
    }

    public bool CanShoot()
    {
        return !gameEnded && currentAmmo > 0;
    }

    public bool UseAmmo()
    {
        if (!CanShoot())
        {
            return false;
        }

        currentAmmo--;
        UpdateUI();
        return true;
    }

    public void AddScore()
    {
        if (gameEnded)
        {
            return;
        }

        currentScore++;

        if (currentScore > maxScore)
        {
            currentScore = maxScore;
        }

        UpdateUI();

        if (currentScore >= maxScore)
        {
            FinishPerfect();
        }
    }

    public void CheckEndAfterShot()
    {
        if (gameEnded)
        {
            return;
        }

        if (currentAmmo <= 0)
        {
            if (currentScore >= 5)
            {
                FinishNormalWin();
            }
            else
            {
                FinishLose();
            }
        }
    }

    private void FinishPerfect()
    {
        gameEnded = true;

        if (resultText != null)
        {
            resultText.text = "Perfect!";
        }

        ShowResultNPC();
        PlayDance();
        PlayCelebrationSound();
        StartCoroutine(PlayConfettiThreeTimes());
    }

    private void FinishNormalWin()
    {
        gameEnded = true;

        if (resultText != null)
        {
            resultText.text = "Good!";
        }

        ShowResultNPC();
        PlayDance();
        PlayCelebrationSound();
    }

    private void FinishLose()
    {
        gameEnded = true;

        if (resultText != null)
        {
            resultText.text = "Try Again";
        }

        ShowResultNPC();
        PlaySigh();
        PlaySighSound();
    }

    private void ShowResultNPC()
    {
        if (resultNpcObject != null)
        {
            resultNpcObject.SetActive(true);
        }
        else if (npcAnimator != null)
        {
            npcAnimator.gameObject.SetActive(true);
        }
    }

    private void HideResultNPC()
    {
        if (resultNpcObject != null)
        {
            resultNpcObject.SetActive(false);
        }
        else if (npcAnimator != null)
        {
            npcAnimator.gameObject.SetActive(false);
        }
    }

    private void PlayDance()
    {
        if (npcAnimator != null)
        {
            npcAnimator.ResetTrigger(sighTriggerName);
            npcAnimator.SetTrigger(danceTriggerName);
        }
    }

    private void PlaySigh()
    {
        if (npcAnimator != null)
        {
            npcAnimator.ResetTrigger(danceTriggerName);
            npcAnimator.SetTrigger(sighTriggerName);
        }
    }

    private void PlayCelebrationSound()
    {
        if (resultAudioSource != null && celebrationSound != null)
        {
            resultAudioSource.PlayOneShot(celebrationSound, celebrationVolume);
        }
    }

    private void PlaySighSound()
    {
        if (resultAudioSource != null && sighSound != null)
        {
            resultAudioSource.PlayOneShot(sighSound, sighVolume);
        }
    }

    private void PlayConfettiSound()
    {
        if (resultAudioSource != null && confettiSound != null)
        {
            resultAudioSource.PlayOneShot(confettiSound, confettiVolume);
        }
    }

    private IEnumerator PlayConfettiThreeTimes()
    {
        for (int i = 0; i < 3; i++)
        {
            if (confettiEffects != null)
            {
                foreach (ParticleSystem effect in confettiEffects)
                {
                    if (effect != null)
                    {
                        effect.Play();
                    }
                }
            }

            PlayConfettiSound();

            yield return new WaitForSeconds(confettiInterval);
        }
    }

    private void UpdateUI()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + currentScore + " / " + maxScore;

            if (currentScore < 5)
            {
                scoreText.color = lowScoreColor;
            }
            else if (currentScore < maxScore)
            {
                scoreText.color = midScoreColor;
            }
            else
            {
                scoreText.color = perfectScoreColor;
            }
        }

        if (ammoText != null)
        {
            ammoText.text = "Ammo: " + currentAmmo + " / " + maxAmmo;
        }
    }
}