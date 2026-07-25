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
        currentPowderLoaded = 0f;
    }

    public void LoadShell()
    {
        if (locked) return;
        loadedShell = currentSelectedShell;
        locked = true;
    }

    public void LoadPowder(float amount)
    {
        if (locked) return;
        currentPowderLoaded += amount;
    }

    public void Fire()
    {
        loadedShell = GameManager.ShellType.None;
        locked = false;
        currentPowderLoaded = 0f;
    }
}
