using UnityEngine;
using UnityEngine.UI;

public class EndCanvasController : MonoBehaviour
{
    [SerializeField] private Button quitButton;
    [SerializeField] private CanvasGroup endCanvasGroup;

    public void End()
    {
        StartCoroutine(EndCoroutine());
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
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            endCanvasGroup.alpha = Mathf.Clamp01(elapsedTime / fadeDuration);
            yield return null;
        }
    }

    public void QuitGame()
    {
        
    }
}
