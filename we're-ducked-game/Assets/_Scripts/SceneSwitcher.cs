using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSwitcher : MonoBehaviour
{
    public static SceneSwitcher Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;
        GameObject go = new GameObject(nameof(SceneSwitcher));
        go.AddComponent<SceneSwitcher>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void LoadMinigame(string sceneName)
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("SceneSwitcher: no GameManager in scene.");
            return;
        }

        if (BasicPlayerController.Instance != null)
        {
            GameManager.Instance.SavePlayerState(BasicPlayerController.Instance.transform);
        }

        GameManager.Instance.mainSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(sceneName);
    }

    public void ReturnToMain()
    {
        if (GameManager.Instance == null || string.IsNullOrEmpty(GameManager.Instance.mainSceneName))
        {
            Debug.LogError("SceneSwitcher: no main scene recorded.");
            return;
        }

        SceneManager.LoadScene(GameManager.Instance.mainSceneName);
    }
}
