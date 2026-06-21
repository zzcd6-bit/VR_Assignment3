using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRSimpleInteractable))]
public class NPCDialogueVR : MonoBehaviour
{
    [Header("UI")]
    public GameObject promptObject;
    public GameObject dialogueObject;
    public TMP_Text dialogueText;

    [Header("Dialogue Lines")]
    [TextArea(2, 4)]
    public string[] dialogueLines;

    [Header("NPC Animation")]
    public Animator npcAnimator;
    public string greetTriggerName = "Greet";
    public float greetDuration = 1.5f;

    private XRSimpleInteractable interactable;

    private bool playerInRange;
    private bool isGreeting;
    private bool dialogueActive;
    private int currentLineIndex;

    private void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();

        if (npcAnimator == null)
        {
            npcAnimator = GetComponentInChildren<Animator>();
        }

        if (promptObject != null)
        {
            promptObject.SetActive(false);
        }

        if (dialogueObject != null)
        {
            dialogueObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        interactable.selectEntered.AddListener(OnNPCSelected);
    }

    private void OnDisable()
    {
        interactable.selectEntered.RemoveListener(OnNPCSelected);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerCollectorMarker>() == null)
        {
            return;
        }

        playerInRange = true;

        if (!dialogueActive && !isGreeting && promptObject != null)
        {
            promptObject.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponentInParent<PlayerCollectorMarker>() == null)
        {
            return;
        }

        playerInRange = false;

        if (promptObject != null)
        {
            promptObject.SetActive(false);
        }

        EndDialogue();
    }

    private void OnNPCSelected(SelectEnterEventArgs args)
    {
        if (!playerInRange)
        {
            return;
        }

        if (isGreeting)
        {
            return;
        }

        if (!dialogueActive)
        {
            StartCoroutine(StartDialogueAfterGreeting());
        }
        else
        {
            ShowNextLine();
        }
    }

    private IEnumerator StartDialogueAfterGreeting()
    {
        isGreeting = true;

        if (promptObject != null)
        {
            promptObject.SetActive(false);
        }

        if (npcAnimator != null)
        {
            npcAnimator.SetTrigger(greetTriggerName);
        }

        yield return new WaitForSeconds(greetDuration);

        isGreeting = false;

        StartDialogue();
    }

    private void StartDialogue()
    {
        if (dialogueLines == null || dialogueLines.Length == 0)
        {
            return;
        }

        dialogueActive = true;
        currentLineIndex = 0;

        if (dialogueObject != null)
        {
            dialogueObject.SetActive(true);
        }

        if (dialogueText != null)
        {
            dialogueText.text = dialogueLines[currentLineIndex];
        }
    }

    private void ShowNextLine()
    {
        currentLineIndex++;

        if (currentLineIndex >= dialogueLines.Length)
        {
            EndDialogue();

            if (playerInRange && promptObject != null)
            {
                promptObject.SetActive(true);
            }

            return;
        }

        if (dialogueText != null)
        {
            dialogueText.text = dialogueLines[currentLineIndex];
        }
    }

    private void EndDialogue()
    {
        dialogueActive = false;
        currentLineIndex = 0;

        if (dialogueObject != null)
        {
            dialogueObject.SetActive(false);
        }
    }
}