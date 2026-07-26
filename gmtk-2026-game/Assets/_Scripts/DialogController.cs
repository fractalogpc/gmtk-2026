using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using GLTFast.Schema;
using DG.Tweening;
using FMODUnity;

public class DialogController : MonoBehaviour
{
    // Player input reference for progress key >
    [SerializeField] PlayerInput playerInput;
    // Speed of typewriter anim >
    [SerializeField] float textSpeed;
    // What the Cooldown timer is set too >
    [SerializeField] float progressCooldown;
    // Reference to text component for Dialog >
    [SerializeField] TextMeshProUGUI textComponent;
    // Object Reference for enableing and disableing Dialog Box >
    [SerializeField] GameObject dialogueBox;
    // Destination for tweening animation >
    [SerializeField] Vector3 tweenDestination;
    // Reference to image gameobject for the person speaking during dialogue >
    [SerializeField] GameObject dialogueImage;
    // Reference to Dialog Object for testing >
    [SerializeField] DialogObject testDialog;
    // Speed of the Tween for Dialogue box >
    [SerializeField] float dialogueAppearAnimSpeed;
    [SerializeField] GameObject progressableIcon;
    [SerializeField] GameObject notProgressableIcon;

    [SerializeField] StudioEventEmitter dialogueSound;

    // String var for Dialog text >
    string[] lines;
    // Index var for typewriter anim >
    int index;
    // Cooldown timer for dialog progress key >
    float coolDown;
    // Image component reference for dialogue >
    UnityEngine.UI.Image image;
    // Bool for preventing code from running when it shouldn't >
    bool dialogueVisible;
    // If true, the player can advance lines but the final dismissal is blocked until ForceDismiss is called.
    bool preventDismiss;
    // Queue of dialogues waiting for the current one to finish.
    readonly Queue<QueuedDialog> dialogueQueue = new();

    public bool IsShowing => dialogueVisible;
    public bool IsPreventingDismiss => preventDismiss;

    private struct QueuedDialog
    {
        public DialogObject dialog;
        public bool preventDismiss;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        dialogueVisible = false;
        textComponent.text = string.Empty;
        image = dialogueImage.GetComponent<UnityEngine.UI.Image>();
        dialogueBox.transform.position = new Vector3(tweenDestination.x, -1000, 0);
        //StartDialogue(testDialog);
    }

    // Update is called once per frame
    void Update()
    {
        if (playerInput.actions["Progress Dialogue"].IsPressed() && coolDown <= 0 && dialogueVisible)
        {
            if (textComponent.text == lines[index])
            {
                NextLine();
            }
            else
            {
                StopAllCoroutines();
                textComponent.text = lines[index];
            }
            coolDown = progressCooldown;
        }
        coolDown += -1*Time.deltaTime;
    }

    // This is called by another script to start a dialogue
    public void StartDialogue(DialogObject dialogue)
    {
        StartDialogue(dialogue, false);
    }

    /// <summary>
    /// Overload that lets callers request the dialog stay on its final line until ForceDismiss()
    /// is called (useful for tutorials that need the mechanic completed before advancing).
    /// If a dialog is already visible, this queues behind it.
    /// </summary>
    public void StartDialogue(DialogObject dialogue, bool preventEarlyDismiss)
    {
        if (dialogue == null) return;
        if (dialogueVisible)
        {
            dialogueQueue.Enqueue(new QueuedDialog { dialog = dialogue, preventDismiss = preventEarlyDismiss });
            return;
        }
        ShowDialogue(dialogue, preventEarlyDismiss);
    }

    private void ShowDialogue(DialogObject dialogue, bool preventEarlyDismiss)
    {
        dialogueVisible = true;
        preventDismiss = preventEarlyDismiss;
        image.sprite = dialogue.sprite;
        lines = dialogue.dialogue;
        index = 0;
        textComponent.text = string.Empty;
        dialogueBox.transform.DOMove(tweenDestination, dialogueAppearAnimSpeed);
        SetProgressableIcon(!preventEarlyDismiss);
        StopAllCoroutines();
        StartCoroutine(TypewriterAnim());
    }

    /// <summary>
    /// Dismiss the current dialog regardless of preventDismiss and pop the next queued one.
    /// </summary>
    public void ForceDismiss()
    {
        if (!dialogueVisible) return;
        preventDismiss = false;
        DismissAndAdvance();
    }

    private void SetProgressableIcon(bool progressable)
    {
        progressableIcon.SetActive(progressable);
        notProgressableIcon.SetActive(!progressable);
    }

    private void DismissAndAdvance()
    {
        dialogueVisible = false;
        preventDismiss = false;

        if (dialogueQueue.Count > 0)
        {
            var next = dialogueQueue.Dequeue();
            ShowDialogue(next.dialog, next.preventDismiss);
        }
        else
        {
            dialogueBox.transform.DOMove(new Vector3(tweenDestination.x, -1000, 0), 0.5f);
        }
    }

    // This is called by StartDialogue(); to display text with a basic typewriter anim
    IEnumerator TypewriterAnim()
    {
        dialogueSound.Play();
        string line = lines[index];
        int position = 0;

        while (position < line.Length)
        {
            if (line[position] == '<')
            {
                int tagEnd = line.IndexOf('>', position);
                if (tagEnd >= 0)
                {
                    textComponent.text += line.Substring(position, tagEnd - position + 1);
                    position = tagEnd + 1;
                    continue;
                }
            }

            char c = line[position];
            if (c == ' ')
            {
                dialogueSound.Play();
            }
            textComponent.text += c;
            position++;
            yield return new WaitForSeconds(textSpeed);
        }
    }

    // This is called in update to proceed to the next line of dialogue/end dialogue
    void NextLine()
    {
        if (index < lines.Length - 1)
        {
            index++;
            textComponent.text = string.Empty;
            StartCoroutine (TypewriterAnim());
        }
        else
        {
            if (!dialogueVisible) return;
            // If a tutorial-style dialog is holding on its final line, don't let the player
            // dismiss it — ForceDismiss() will be called once the mechanic is complete.
            if (preventDismiss) return;
            DismissAndAdvance();
        }
    }
}
