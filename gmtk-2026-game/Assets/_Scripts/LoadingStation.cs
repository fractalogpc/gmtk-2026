using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class LoadingStation : MonoBehaviour
{

    [SerializeField] private Button loadButton;

    [SerializeField] private TextMeshProUGUI powderText;
    [SerializeField] private TextMeshProUGUI shellText;

    [Tooltip("Powder units per second when hold first starts.")]
    [SerializeField] private float startFillSpeed = 5f;
    [Tooltip("Additional units per second per second while holding — makes the meter accelerate.")]
    [SerializeField] private float fillAcceleration = 25f;
    [Tooltip("Units per second the powder drains when released outside the target zone.")]
    [SerializeField] private float decaySpeed = 15f;
    [Tooltip("Units per second the powder drains during the OVERLOADED lockout. Should be faster than decaySpeed.")]
    [SerializeField] private float overloadDecaySpeed = 30f;
    [Tooltip("Lower bound of the acceptable powder amount.")]
    [SerializeField] private float targetMin = 7*10;
    [Tooltip("Upper bound of the acceptable powder amount.")]
    [SerializeField] private float targetMax = 7*12;
    [Tooltip("Powder amount at which the load overloads and resets. Must be > targetMax.")]
    [SerializeField] private float overloadThreshold = 91f;

    [Header("Events")]
    [SerializeField] private UnityEvent onOverload;
    [SerializeField] private UnityEvent onShellLoaded;
    [SerializeField] private UnityEvent<float> onPowderChanged;

    private GameManager.ShellType currentSelectedShell = GameManager.ShellType.None;
    private GameManager.ShellType loadedShell = GameManager.ShellType.None;
    private float currentPowderLoaded = 0f;
    private bool locked = false;

    private float currentFillSpeed;
    private bool wasPressed;
    private bool overloaded;

    public float CurrentPowder => currentPowderLoaded;
    public bool IsLocked => locked;

    public void SelectShell(int shell)
    {
        currentSelectedShell = (GameManager.ShellType)shell;

        UpdateShellText();
    }

    public void Start()
    {
        UpdateProgressText();
        UpdateShellText();
    }

    private void Update()
    {
        if (locked || loadButton == null) return;

        bool pressed = loadButton.IsPressed();
        float previous = currentPowderLoaded;

        if (overloaded)
        {
            currentPowderLoaded = Mathf.Max(0f, currentPowderLoaded - overloadDecaySpeed * Time.deltaTime);
            if (currentPowderLoaded <= 0f)
            {
                overloaded = false;
                currentFillSpeed = 0f;
            }
        }
        else if (pressed)
        {
            if (!wasPressed) currentFillSpeed = startFillSpeed;
            currentFillSpeed += fillAcceleration * Time.deltaTime;
            currentPowderLoaded += currentFillSpeed * Time.deltaTime;

            if (currentPowderLoaded >= overloadThreshold)
            {
                Overload();
                wasPressed = pressed;
                return;
            }
        }
        else if (wasPressed && currentPowderLoaded > targetMax)
        {
            Overload();
            wasPressed = pressed;
            return;
        }
        else if (wasPressed &&
                 currentPowderLoaded >= targetMin &&
                 currentPowderLoaded <= targetMax)
        {
            wasPressed = pressed;
            UpdateProgressText();
            return;
        }
        else if (currentPowderLoaded > 0f)
        {
            currentPowderLoaded = Mathf.Max(0f, currentPowderLoaded - decaySpeed * Time.deltaTime);
        }

        if (!Mathf.Approximately(previous, currentPowderLoaded))
        {
            onPowderChanged.Invoke(currentPowderLoaded);
            UpdateProgressText();
        }

        wasPressed = pressed;
    }

    private void UpdateProgressText()
    {
        if (locked)
        {
            powderText.text = $"POWDER LOADED\n\n[{GenerateBar(currentPowderLoaded)}]\n\nREADY";
            return;
        }
        if (overloaded)
        {
            powderText.text = $"OVERLOADED\n\n[{GenerateBar(currentPowderLoaded)}]\n\nWAIT FOR RESET";
            return;
        }
        powderText.text = $"POWDER LOADING\n\n[{GenerateBar(currentPowderLoaded)}]\n\n{(currentPowderLoaded == 0 ? "HOLD TO LOAD" : "DO NOT OVERFILL\n\nPULL TO LOCK")}";
    }

    private GameManager.ShellType requiredShell = GameManager.ShellType.None;
    private void UpdateShellText()
    {
        shellText.text = $"REQUIRED SHELL \n\n{ParseShellType(requiredShell)}\n\nSELECTED SHELL \n\n{ParseShellType(currentSelectedShell)}";
    }

    private string ParseShellType(GameManager.ShellType shell)
    {
        switch (shell)
        {
            case GameManager.ShellType.AP:
                return "Armor Piercing";
            case GameManager.ShellType.INC:
                return "Incendiary";
            case GameManager.ShellType.HE:
                return "High Explosive";
            default:
                return "NONE";
        }
    }

    private string GenerateBar(float value)
    {
        int totalSections = 13;
        int filledSections = Mathf.Clamp(Mathf.RoundToInt(value / overloadThreshold * totalSections), 0, totalSections);

        if (overloaded)
        {
            string filled = new string('#', filledSections);
            string empty = new string('-', totalSections - filledSections);
            return $"<color=red>{filled}</color>{empty}";
        }

        string bar = new string('#', filledSections) + new string('-', totalSections - filledSections);

        string first = bar.Substring(0, 9);
        string second = bar.Substring(9, 2);
        string third = bar.Substring(11, 2);

        return $"<color=white>{first}</color><color=green>{second}</color><color=red>{third}</color>";
    }

    private void Overload()
    {
        overloaded = true;
        currentFillSpeed = 0f;
        onOverload.Invoke();
        onPowderChanged.Invoke(currentPowderLoaded);
        UpdateProgressText();
    }

    public void LoadShell()
    {
        loadedShell = currentSelectedShell;
        locked = true;
        onShellLoaded.Invoke();
        onPowderChanged.Invoke(currentPowderLoaded);
        UpdateProgressText();
    }

    private void Fire()
    {
        loadedShell = GameManager.ShellType.None;
        locked = false;
        currentPowderLoaded = 0f;
        currentFillSpeed = 0f;
        onPowderChanged.Invoke(currentPowderLoaded);
        UpdateProgressText();
    }
}
