using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class EndCanvasController : MonoBehaviour
{
    [SerializeField] private CanvasGroup endCanvasGroup;

    public void End()
    {
        StartCoroutine(EndCoroutine());
    }

    private void Start()
    {
        endCanvasGroup.alpha = 0f; // Start with the canvas invisible
        endCanvasGroup.blocksRaycasts = false; // Disable interaction with the canvas
        endCanvasGroup.interactable = false; // Disable interaction with the canvas
    }

    private System.Collections.IEnumerator EndCoroutine()
    {
        // Your end logic here
        yield return new WaitForSeconds(5f); // Wait for 2 seconds before quitting

        // Fade in the end canvas over time
        float fadeDuration = 5f; // Duration of the fade
        float elapsedTime = 0f;
        endCanvasGroup.blocksRaycasts = true; // Enable interaction with the canvas
        endCanvasGroup.interactable = true; // Enable interaction with the canvas

        Cursor.lockState = CursorLockMode.None; // Unlock the cursor
        Cursor.visible = true; // Make the cursor visible
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            endCanvasGroup.alpha = Mathf.Clamp01(elapsedTime / fadeDuration);
            yield return null;
        }
    }

    public void QuitGame()
    {
        SceneManager.LoadScene("MainMenu"); // Load the main menu scene
    }
}
