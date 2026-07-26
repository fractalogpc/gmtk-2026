using UnityEngine;
using UnityEngine.Events;

public class DecoderController : MonoBehaviour
{
    public DragDial leftDial;
    public DragDial rightDial;

    public GameObject verticalBar;
    public GameObject horizontalBar;

    [Tooltip("Local units the horizontal bar shifts vertically per degree of right-dial rotation.")]
    [SerializeField] private float horizontalBarSensitivity = 0.005f;
    [Tooltip("Local units the vertical bar shifts horizontally per degree of left-dial rotation.")]
    [SerializeField] private float verticalBarSensitivity = 0.005f;

    [Tooltip("Min/Max offset (local units) the vertical bar can shift along X from its start position.")]
    [SerializeField] private Vector2 verticalBarOffsetClamp = new Vector2(-0.5f, 0.5f);
    [Tooltip("Min/Max offset (local units) the horizontal bar can shift along Y from its start position.")]
    [SerializeField] private Vector2 horizontalBarOffsetClamp = new Vector2(-0.5f, 0.5f);

    [Header("Signal / Wave")]
    [SerializeField] private WaveVisual waveVisual;
    [SerializeField] private StaticCameraView staticView;
    [Tooltip("Lights that stay off until the decoder is first activated (first SetTarget/RandomizeTarget call).")]
    [SerializeField] private GameObject[] indicatorLights;
    [Tooltip("Other GameObjects that stay disabled until the decoder is first activated.")]
    [SerializeField] private GameObject[] additionalActivatedObjects;
    [Tooltip("Auto-populated at Awake by scanning within Fire Detection Radius. Any manual entries are overwritten.")]
    [SerializeField] private E_FireComponent[] nearbyFires;
    [Tooltip("Radius (world units) around this machine to scan for E_FireComponent instances at Awake. 0 disables the scan.")]
    [SerializeField] private float fireDetectionRadius = 5f;
    [SerializeField] private UnityEvent onSignalMatched;
    [SerializeField] private UnityEvent onSignalLost;
    [SerializeField] private UnityEvent onTargetRandomized;

    private bool gameActive;
    private bool powered = true;
    private bool IsInteractable => gameActive && powered;

    private Vector3 verticalBarInitialWorldPos;
    private Vector3 horizontalBarInitialWorldPos;

    public bool IsSignalMatched => waveVisual != null && waveVisual.IsMatched;
    public float CurrentAmplitude => waveVisual != null ? waveVisual.CurrentAmplitude : 0f;
    public float CurrentDistortion => waveVisual != null ? waveVisual.CurrentDistortion : 0f;
    public float CurrentPhase => waveVisual != null ? waveVisual.CurrentPhase : 0f;

    public float NormalizedAccuracy => waveVisual.NormalizedAccuracy;

    // C# event so other scripts (e.g. GameManager) can subscribe without needing an inspector hookup.
    public event System.Action SignalMatched;

    private void Awake()
    {
        if (verticalBar != null) verticalBarInitialWorldPos = verticalBar.transform.position;
        if (horizontalBar != null) horizontalBarInitialWorldPos = horizontalBar.transform.position;

        // Constrain each dial's rotation range to exactly the bar clamp so the dial
        // physically stops at the same limits and stays in sync with the bar.
        if (leftDial != null && verticalBarSensitivity != 0f)
        {
            // vertical bar offset = -leftDial.Angle * sens, clamped to [clamp.x, clamp.y]
            //   → leftDial.Angle in [-clamp.y / sens, -clamp.x / sens]
            float min = -verticalBarOffsetClamp.y / verticalBarSensitivity;
            float max = -verticalBarOffsetClamp.x / verticalBarSensitivity;
            leftDial.SetBounds(min, max);
            leftDial.SetAngle((min + max) * 0.5f);
        }
        if (rightDial != null && horizontalBarSensitivity != 0f)
        {
            float min = horizontalBarOffsetClamp.x / horizontalBarSensitivity;
            float max = horizontalBarOffsetClamp.y / horizontalBarSensitivity;
            rightDial.SetBounds(min, max);
            rightDial.SetAngle((min + max) * 0.5f);
        }

        AutoDetectNearbyFires();
    }

    private void AutoDetectNearbyFires()
    {
        if (fireDetectionRadius <= 0f) return;
        E_FireComponent[] all = FindObjectsByType<E_FireComponent>(FindObjectsSortMode.None);
        System.Collections.Generic.List<E_FireComponent> nearby = new();
        Vector3 pos = transform.position;
        foreach (E_FireComponent fire in all)
        {
            if (fire == null) continue;
            if (Vector3.Distance(pos, fire.transform.position) <= fireDetectionRadius)
                nearby.Add(fire);
        }
        nearbyFires = nearby.ToArray();
    }

    private void OnEnable()
    {
        if (waveVisual != null)
        {
            waveVisual.Matched += HandleMatched;
            waveVisual.Unmatched += HandleUnmatched;
            waveVisual.TargetRandomized += HandleTargetRandomized;
        }

        if (nearbyFires != null)
        {
            foreach (E_FireComponent fire in nearbyFires)
            {
                if (fire == null) continue;
                fire.OnFireStarted.AddListener(HandleFireStarted);
                fire.OnFireExtinguished.AddListener(HandleFireExtinguished);
            }
        }
    }

    private void OnDisable()
    {
        if (waveVisual != null)
        {
            waveVisual.Matched -= HandleMatched;
            waveVisual.Unmatched -= HandleUnmatched;
            waveVisual.TargetRandomized -= HandleTargetRandomized;
        }

        if (nearbyFires != null)
        {
            foreach (E_FireComponent fire in nearbyFires)
            {
                if (fire == null) continue;
                fire.OnFireStarted.RemoveListener(HandleFireStarted);
                fire.OnFireExtinguished.RemoveListener(HandleFireExtinguished);
            }
        }
    }

    private int activeFireCount;
    private void HandleFireStarted()
    {
        activeFireCount++;
        ApplyAdditionalObjectsState();
    }

    private void HandleFireExtinguished()
    {
        activeFireCount = Mathf.Max(0, activeFireCount - 1);
        ApplyAdditionalObjectsState();
    }

    private void ApplyAdditionalObjectsState()
    {
        bool shouldBeOn = hasActivated && powered && activeFireCount == 0;
        SetAll(additionalActivatedObjects, shouldBeOn);
    }

    private void ApplyIndicatorLightsState()
    {
        SetAll(indicatorLights, hasActivated && powered);
    }

    private void HandleMatched()
    {
        onSignalMatched.Invoke();
        SignalMatched?.Invoke();
    }
    private void HandleUnmatched() => onSignalLost.Invoke();
    private void HandleTargetRandomized() => onTargetRandomized.Invoke();

    public void RandomizeTarget()
    {
        if (waveVisual != null) waveVisual.RandomizeTarget();
        SetInteractable(true);
        TurnOnIndicatorLights();
    }

    public void SetTarget(float amplitude, float distortion, float phase)
    {
        if (waveVisual != null) waveVisual.SetTarget(amplitude, distortion, phase);
        SetInteractable(true);
        TurnOnIndicatorLights();
    }

    private bool hasActivated;
    private void TurnOnIndicatorLights()
    {
        if (hasActivated) return;
        hasActivated = true;
        ApplyIndicatorLightsState();
        ApplyAdditionalObjectsState();
    }

    private static void SetAll(GameObject[] items, bool value)
    {
        if (items == null) return;
        foreach (GameObject go in items)
        {
            if (go != null) go.SetActive(value);
        }
    }

    public void Reset()
    {
        if (waveVisual != null) waveVisual.ResetState();
        SetInteractable(false);
    }

    public void SetInteractable(bool interactable)
    {
        gameActive = interactable;
        ApplyInteractable();
    }

    public void SetPowered(bool value)
    {
        powered = value;
        ApplyInteractable();
        ApplyAdditionalObjectsState();
        ApplyIndicatorLightsState();
    }

    private void ApplyInteractable()
    {
        if (staticView == null) return;
        if (IsInteractable) staticView.ReactivateManagedObjects();
        else staticView.DeactivateManagedObjects();
    }

    private void Start()
    {
        SetInteractable(false);
        SetAll(indicatorLights, false);
        SetAll(additionalActivatedObjects, false);
    }

    private void Update()
    {
        UpdateRadioVisuals();
    }

    private void UpdateRadioVisuals()
    {
        if (verticalBar != null && leftDial != null)
        {
            float offset = -leftDial.Angle * verticalBarSensitivity;
            verticalBar.transform.position = verticalBarInitialWorldPos + verticalBar.transform.TransformVector(Vector3.right) * offset;
        }

        if (horizontalBar != null && rightDial != null)
        {
            float offset = rightDial.Angle * horizontalBarSensitivity;
            horizontalBar.transform.position = horizontalBarInitialWorldPos + horizontalBar.transform.TransformVector(Vector3.up) * offset;
        }

        if (horizontalBar != null && verticalBar != null)
        {
            Vector3 hLocal = horizontalBar.transform.localPosition;
            hLocal.x = verticalBar.transform.localPosition.x;
            horizontalBar.transform.localPosition = hLocal;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (verticalBar != null)
        {
            Vector3 baseWorld = Application.isPlaying ? verticalBarInitialWorldPos : verticalBar.transform.position;
            DrawBarRangeGizmo(verticalBar.transform, baseWorld, Vector3.right, verticalBarOffsetClamp, Color.cyan);
        }
        if (horizontalBar != null)
        {
            Vector3 baseWorld = Application.isPlaying ? horizontalBarInitialWorldPos : horizontalBar.transform.position;
            DrawBarRangeGizmo(horizontalBar.transform, baseWorld, Vector3.up, horizontalBarOffsetClamp, Color.magenta);
        }
    }

    private static void DrawBarRangeGizmo(Transform bar, Vector3 baseWorld, Vector3 localAxis, Vector2 clamp, Color color)
    {
        Vector3 axisWorld = bar.TransformVector(localAxis);
        Vector3 minWorld = baseWorld + axisWorld * clamp.x;
        Vector3 maxWorld = baseWorld + axisWorld * clamp.y;

        Gizmos.color = color;
        Gizmos.DrawLine(minWorld, maxWorld);
        Gizmos.DrawWireSphere(minWorld, 0.02f);
        Gizmos.DrawWireSphere(maxWorld, 0.02f);
        Gizmos.DrawWireSphere(baseWorld, 0.015f);
    }
#endif
}
