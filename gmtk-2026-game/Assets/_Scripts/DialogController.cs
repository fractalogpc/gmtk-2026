using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.InputSystem;

public class DialogController : MonoBehaviour
{
    // Reference to textComponent for Dialog >
    public TextMeshProUGUI textComponent;
    // String var for Dialog text >
    public string[] lines;

    // Player input reference for progress key >
    [SerializeField] PlayerInput playerInput;
    // Speed of typewriter anim >
    [SerializeField] float textSpeed;
    // What the Cooldown timer is set too >
    [SerializeField] float progressCooldown;
    // Object Reference for enableing and disableing Dialog Box >
    [SerializeField] GameObject dialogBox;

    // Index var for typewriter anim >
    int index;
    // Cooldown timer for dialog progress key >
    float coolDown;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        textComponent.text = string.Empty;
        StartDialogue();
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

    void StartDialogue()
    {
        index = 0;
        StartCoroutine(TypewriterAnim());
    }

    IEnumerator TypewriterAnim()
    {
        foreach (char c in lines[index].ToCharArray())
        {
            textComponent.text += c;
            yield return new WaitForSeconds(textSpeed);
        }
    }

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
            dialogBox.SetActive(false);
        }
    }
}
