using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;

public class MainMenuManager : MonoBehaviour
{

    private CinemachineCamera[] menuCameras;

    private void Start()
    {
        menuCameras = GetComponentsInChildren<CinemachineCamera>();
    }

    public void OnRightClick()
    {
        BackToMenu();
    }

    public void StartGame()
    {
        SceneManager.LoadScene("Bunker");
    }

    public void BackToMenu()
    {
        for (int i = 1; i < menuCameras.Length; i++)
        {
            menuCameras[i].enabled = false;
        }
    }
}
