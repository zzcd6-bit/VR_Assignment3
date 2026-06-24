using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class CollectibleCounter : MonoBehaviour
{
    public static CollectibleCounter Instance { get; private set; }

    [Header("Collectible Settings")]
    public int targetCount = 6;
    public int currentCount = 0;

    [Header("UI")]
    public TMP_Text counterText;

    [Header("Event")]
    public UnityEvent onAllCollected;

    private bool allCollectedTriggered = false;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        UpdateUI();
    }

    public void AddCollectible(int amount)
    {
        currentCount += amount;

        if (currentCount > targetCount)
        {
            currentCount = targetCount;
        }

        UpdateUI();

        if (currentCount >= targetCount && !allCollectedTriggered)
        {
            allCollectedTriggered = true;
            onAllCollected.Invoke();
        }
    }

    private void UpdateUI()
    {
        if (counterText != null)
        {
            counterText.text = "Cores: " + currentCount + " / " + targetCount;
        }
    }
}