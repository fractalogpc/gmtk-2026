using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.InputSystem;
using GLTFast.Schema;

public class DialogController : MonoBehaviour
{
    // String var for Dialog text >
    public string[] lines;
    // Temporary image var for testing dialogue functionality >
    public Sprite tempSprite;

    // Player input reference for progress key >
    [SerializeField] PlayerInput playerInput;
    // Speed of typewriter anim >
    [SerializeField] float textSpeed;
    // What the Cooldown timer is set too >
    [SerializeField] float progressCooldown;
    // Object Reference for enableing and disableing Dialog Box >
    [SerializeField] GameObject dialogueBox;
    // Reference to text component for Dialog >
    [SerializeField] TextMeshProUGUI textComponent;
    // Reference to image gameobject for the person speaking during dialogue >
    [SerializeField] GameObject dialogueImage;

    // Index var for typewriter anim >
    int index;
    // Cooldown timer for dialog progress key >
    float coolDown;
    // Image component reference for dialogue >
    UnityEngine.UI.Image image;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        textComponent.text = string.Empty;
        image = dialogueImage.GetComponent<UnityEngine.UI.Image>();
        dialogueBox.SetActive(false);
        //StartDialogue();
    }

    // Update is called once per frame
    void Update()
    {
        if (playerInput.actions["Progress Dialogue"].IsPressed() && coolDown <= 0)
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
    public void StartDialogue()
    {
        dialogueBox.SetActive(true);
        image.sprite = tempSprite;
        index = 0;
        StartCoroutine(TypewriterAnim());
    }

    // This is called by StartDialogue(); to display text with a basic typewriter anim
    IEnumerator TypewriterAnim()
    {
        foreach (char c in lines[index].ToCharArray())
        {
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
            dialogueBox.SetActive(false);
        }
    }
}
