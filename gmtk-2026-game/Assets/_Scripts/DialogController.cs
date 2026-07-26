using UnityEngine;
using TMPro;
using System.Collections;
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
        dialogueVisible = true;
        image.sprite = dialogue.sprite;
        lines = dialogue.dialogue;
        index = 0;
        dialogueBox.transform.DOMove(tweenDestination, dialogueAppearAnimSpeed);
        StartCoroutine(TypewriterAnim());
    }

    // This is called by StartDialogue(); to display text with a basic typewriter anim
    IEnumerator TypewriterAnim()
    {
        dialogueSound.Play();
        foreach (char c in lines[index].ToCharArray())
        {
            if (c == ' ') 
            {
                dialogueSound.Play();
            }
            textComponent.text += c;
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
            dialogueBox.transform.DOMove(new Vector3(tweenDestination.x, -1000, 0), 0.5f);
            dialogueVisible = false;
        }
    }
}
