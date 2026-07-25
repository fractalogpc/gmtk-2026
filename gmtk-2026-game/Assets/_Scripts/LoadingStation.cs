using UnityEngine;

public class LoadingStation : MonoBehaviour
{

    [SerializeField] private Button loadButton;

    private GameManager.ShellType currentSelectedShell = GameManager.ShellType.Normal;
    private GameManager.ShellType loadedShell = GameManager.ShellType.Normal;
    private float currentPowderLoaded = 100f;
    private bool locked = true;

    public void SelectShell(int shell)
    {
        currentSelectedShell = (GameManager.ShellType)shell;
    }

    private void LoadShell()
    {
        loadedShell = currentSelectedShell;
        locked = true;
    }

    private void LoadPowder(float amount)
    {
        currentPowderLoaded += amount;
    }

    private void Fire()
    {
        loadedShell = GameManager.ShellType.None;
        locked = false;
        currentPowderLoaded = 0f;
    }
}
