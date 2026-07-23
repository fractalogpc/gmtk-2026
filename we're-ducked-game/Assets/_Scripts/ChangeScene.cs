using UnityEngine;

public class ChangeScene : MonoBehaviour
{
    public void LoadScene(string sceneName)
    {
        SceneSwitcher.Instance.LoadMinigame(sceneName);
    }

    public void ReturnToMain()
    {
        SceneSwitcher.Instance.ReturnToMain();
    }
}
