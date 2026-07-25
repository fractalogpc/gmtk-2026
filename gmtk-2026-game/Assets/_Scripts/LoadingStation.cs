using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class LoadingStation : MonoBehaviour
{

    [SerializeField] private Button loadButton;

    [SerializeField] private TextMeshProUGUI powderText;

    [Tooltip("Powder units per second when hold first starts.")]
    [SerializeField] private float startFillSpeed = 5f;
    [Tooltip("Additional units per second per second while holding — makes the meter accelerate.")]
    [SerializeField] private float fillAcceleration = 25f;
    [Tooltip("Units per second the powder drains when released outside the target zone.")]
    [SerializeField] private float decaySpeed = 15f;
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

    private GameManager.ShellType currentSelectedShell = GameManager.ShellType.Normal;
    private GameManager.ShellType loadedShell = GameManager.ShellType.Normal;
    private float currentPowderLoaded = 0f;
    private bool locked = false;

    private float currentFillSpeed;
    private bool wasPressed;

    public float CurrentPowder => currentPowderLoaded;
    public bool IsLocked => locked;

    public void SelectShell(int shell)
    {
        currentSelectedShell = (GameManager.ShellType)shell;
    }

    public void Start()
    {
        UpdateText();
    }

    private void Update()
    {
        if (locked || loadButton == null) return;

        bool pressed = loadButton.IsPressed();
        float previous = currentPowderLoaded;

        if (pressed)
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
        else if (wasPressed &&
                 currentPowderLoaded >= targetMin &&
                 currentPowderLoaded <= targetMax)
        {
            LoadShell();
            wasPressed = pressed;
            return;
        }
        else if (currentPowderLoaded > 0f)
        {
            currentPowderLoaded = Mathf.Max(0f, currentPowderLoaded - decaySpeed * Time.deltaTime);
        }

        if (!Mathf.Approximately(previous, currentPowderLoaded))
        {
            onPowderChanged.Invoke(currentPowderLoaded);
            UpdateText();
        }

        wasPressed = pressed;
    }

    private void UpdateText()
    {
        powderText.text = $"POWDER LOADING\n\n[{GenerateBar(currentPowderLoaded)}]\n\n{(currentPowderLoaded == 0 ? "HOLD TO LOAD" : "DO NOT OVERFILL")}";
    }

    private string GenerateBar(float value)
    {
        int totalSections = 13;
        int filledSections = Mathf.Clamp(Mathf.RoundToInt(value / overloadThreshold * totalSections), 0, totalSections);
        string bar = new string('#', filledSections) + new string('-', totalSections - filledSections);

        string first = bar.Substring(0, 9);
        string second = bar.Substring(9, 2);
        string third = bar.Substring(11, 2);

        bar = $"<color=white>{first}</color><color=green>{second}</color><color=red>{third}</color>";
        
        return bar;
    }

    private void Overload()
    {
        currentPowderLoaded = 0f;
        currentFillSpeed = 0f;
        onOverload.Invoke();
        onPowderChanged.Invoke(currentPowderLoaded);
    }

    private void LoadShell()
    {
        loadedShell = currentSelectedShell;
        locked = true;
        onShellLoaded.Invoke();
        onPowderChanged.Invoke(currentPowderLoaded);
    }

    private void Fire()
    {
        loadedShell = GameManager.ShellType.None;
        locked = false;
        currentPowderLoaded = 0f;
        currentFillSpeed = 0f;
        onPowderChanged.Invoke(currentPowderLoaded);
    }
}
