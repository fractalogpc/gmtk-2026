using TMPro;
using UnityEngine;
using FMODUnity;

public class TargetingConsole : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI targetText;
    [SerializeField] private TextMeshProUGUI gunText;
    [SerializeField] private DragLever leverAzimuth;
    [SerializeField] private DragLever leverElevation;
    [SerializeField] private Button recieveCoordinatesButton;
    [SerializeField] private StaticCameraView staticView;
    [Tooltip("Lights that stay off until the console is first activated (first SetTargetValues call).")]
    [SerializeField] private GameObject[] indicatorLights;
    [Tooltip("Other GameObjects that stay disabled until the console is first activated.")]
    [SerializeField] private GameObject[] additionalActivatedObjects;
    [Tooltip("Auto-populated at Awake by scanning within Fire Detection Radius. Any manual entries are overwritten.")]
    [SerializeField] private E_FireComponent[] nearbyFires;
    [Tooltip("Radius (world units) around this machine to scan for E_FireComponent instances at Awake. 0 disables the scan.")]
    [SerializeField] private float fireDetectionRadius = 5f;
    [SerializeField] private float azimuthForceMin, azimuthForceMax;
    [SerializeField] private float elevationForceMin, elevationForceMax;
    [SerializeField] private float gunMass = 100f;
    [SerializeField][Range(0, 1)] private float gunDrag = 0.1f;
    [SerializeField] private float gunMaxAzimuthVelocity = 30f;
    [SerializeField] private float gunMaxElevationVelocity = 15f;
    [SerializeField] private StudioEventEmitter gunMovementEmitter;
    [SerializeField] private Transform gunBase;
    [SerializeField] private Transform gunBarrel;
    [SerializeField] private float stopEmitterAfterSecondsAtZero = 0.2f;
    public float GunAzimuth => gunAzimuth;
    public float GunElevation => gunElevation;
    private float gunAzimuth, gunElevation;
    private float gunVelocityAzimuth, gunVelocityElevation;

    public bool HasReceivedCoordinates;

    private float pendingAzimuth;
    private float pendingElevation;
    private float pendingTolerance;
    private bool coordinatesEncrypted;

    private bool locked = false;
    // Targeting is interactable by default — the player is always allowed to aim the gun.
    // Power gating is the only thing that can knock it out at runtime.
    private bool gameActive = true;
    private bool powered = true;
    private bool hasDoneTutorial = false;
    private bool IsInteractable => gameActive && powered;

    public void SetLocked(bool isLocked)
    {
        ((IInteractable)leverAzimuth).SetInteractionEnabled(!isLocked);
        ((IInteractable)leverElevation).SetInteractionEnabled(!isLocked);
        if (isLocked)
        {
            gunText.text = "CONTROLS LOCKED\nFIRE COMMAND SENT";
        }
        else
        {
            UpdateGunText();
        }
        locked = isLocked;
    }

    public void DisplayMessage(string message)
    {
        gunText.text = message;
    }

    /// <summary>
    /// Shows a persistent red failure message on the gun-orientation screen. Stays visible
    /// until the player presses the receive-coordinates button (i.e. HasReceivedCoordinates
    /// flips back true for the next round).
    /// </summary>
    public void ShowFailureMessage(string message)
    {
        hasFailureMessage = true;
        if (gunText != null)
            gunText.text = $"<color=#ff3232>{message}</color>";
    }

    private void ClearFailureMessage()
    {
        if (!hasFailureMessage) return;
        hasFailureMessage = false;
        UpdateGunText();
    }

    private bool hasFailureMessage;

    public void SetTargetValues(float azimuth, float elevation, float tolerance, bool encrypted = false)
    {
        pendingAzimuth = azimuth;
        pendingElevation = elevation;
        pendingTolerance = tolerance;
        coordinatesEncrypted = encrypted;
        HasReceivedCoordinates = false;
        UpdateTargetText();
        SetInteractable(true);
        TurnOnIndicatorLights();
    }

    private bool hasActivated;
    private int activeFireCount;

    private void Awake()
    {
        SetAll(indicatorLights, false);
        SetAll(additionalActivatedObjects, false);
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
        if (nearbyFires == null) return;
        foreach (E_FireComponent fire in nearbyFires)
        {
            if (fire == null) continue;
            fire.OnFireStarted.AddListener(HandleFireStarted);
            fire.OnFireExtinguished.AddListener(HandleFireExtinguished);
        }
    }

    private void OnDisable()
    {
        if (nearbyFires == null) return;
        foreach (E_FireComponent fire in nearbyFires)
        {
            if (fire == null) continue;
            fire.OnFireStarted.RemoveListener(HandleFireStarted);
            fire.OnFireExtinguished.RemoveListener(HandleFireExtinguished);
        }
    }

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

    private void TurnOnIndicatorLights()
    {
        if (hasActivated) return;
        hasActivated = true;
        ApplyIndicatorLightsState();
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

    private static void SetAll(GameObject[] items, bool value)
    {
        if (items == null) return;
        foreach (GameObject go in items)
        {
            if (go != null) go.SetActive(value);
        }
    }

    public void RevealCoordinates()
    {
        if (!coordinatesEncrypted) return;
        coordinatesEncrypted = false;
        UpdateTargetText();
    }

    private void UpdateTargetText()
    {
        if (targetText == null) return;
        if (!HasReceivedCoordinates)
        {
            targetText.text = "FIRING ORDERS\n———————————————\nAWAITING TRANSMISSION\nPRESS RECEIVE";
            return;
        }
        if (coordinatesEncrypted)
        {
            targetText.text = "FIRING ORDERS\n———————————————\n<< ENCRYPTED >>\nDECODE SIGNAL";
        }
        else
        {
            targetText.text = $"FIRING ORDERS\n———————————————\n{pendingAzimuth:F2} AZIM\n{pendingElevation:F2} ELEV\nMAX DEV ±{pendingTolerance:F2}";
        }
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

    public void Reset()
    {
        HasReceivedCoordinates = false;
        locked = false;
        coordinatesEncrypted = false;
        pendingAzimuth = 0f;
        pendingElevation = 0f;
        if (targetText != null) targetText.text = string.Empty;
        UpdateGunText();
        // Note: intentionally leave interactability alone. The gun should always be aim-able
        // between rounds — power cuts are the only thing that should lock the levers.
    }

    private float MapNormalizedToRange(float normalizedValue, float min, float max)
    {
        return Mathf.Lerp(min, max, normalizedValue);
    }

    private void UpdateGunText()
    {
        if (hasFailureMessage) return; // preserve the red failure message across normal updates
        gunText.text = $"GUN ORIENTATION\n———————————————\n{gunAzimuth:F2} AZIM\n{gunElevation:F2} ELEV";
    }

    private void UpdateAccelFromLevers(float deltaTime)
    {
        float azimuthForce = MapNormalizedToRange(leverAzimuth.NormalizedValue, azimuthForceMin, azimuthForceMax);
        float elevationForce = MapNormalizedToRange(leverElevation.NormalizedValue, elevationForceMin, elevationForceMax);
        gunVelocityAzimuth += (azimuthForce / gunMass) * deltaTime;
        gunVelocityElevation += (elevationForce / gunMass) * deltaTime;
        gunVelocityAzimuth = Mathf.Clamp(gunVelocityAzimuth, -gunMaxAzimuthVelocity, gunMaxAzimuthVelocity);
        gunVelocityElevation = Mathf.Clamp(gunVelocityElevation, -gunMaxElevationVelocity, gunMaxElevationVelocity);
        if (azimuthForce == 0f) gunVelocityAzimuth = 0f;
        if (elevationForce == 0f) gunVelocityElevation = 0f;
    }

    private void UpdateGunFromVelocity(float deltaTime)
    {
        gunAzimuth += gunVelocityAzimuth * deltaTime;
        if (gunAzimuth > 180f) {
            gunAzimuth = -180f;
            gunVelocityAzimuth = 0f;
        }
        if (gunAzimuth < -180f) {
            gunAzimuth = 180f;
            gunVelocityAzimuth = 0f;
        }

        gunElevation += gunVelocityElevation * deltaTime;
        if (gunElevation > 90f) {
            gunElevation = 90f;
            gunVelocityElevation = 0f;
        }
        if (gunElevation < 0f) {
            gunElevation = 0f;
            gunVelocityElevation = 0f;
        }

        gunVelocityAzimuth *= (1f - gunDrag * deltaTime);
        gunVelocityElevation *= (1f - gunDrag * deltaTime);
    }

    private float secondsAtZeroSpeed = 0f;
    private void UpdateTraverseSound(float deltaTime)
    {
        float azimuthSpeed = Mathf.Abs(gunVelocityAzimuth) / gunMaxAzimuthVelocity;
        float elevationSpeed = Mathf.Abs(gunVelocityElevation) / gunMaxElevationVelocity;
        float speed = Mathf.Max(azimuthSpeed, elevationSpeed);
        gunMovementEmitter.SetParameter("TraverseSpeed", speed);
        if (speed > 0f)
        {
            secondsAtZeroSpeed = 0f;
            if (!gunMovementEmitter.IsPlaying())
            {
                gunMovementEmitter.Play();
            }
        }
        else
        {
            secondsAtZeroSpeed += deltaTime;
            if (secondsAtZeroSpeed >= stopEmitterAfterSecondsAtZero && gunMovementEmitter.IsPlaying())
            {
                gunMovementEmitter.Stop();
            }
        }
    }

    private float GetError()
    {
        // Account for the fact that azimuth is circular
        float azimuthError = Mathf.Abs(gunAzimuth - pendingAzimuth);
        if (azimuthError > 180f)
        {
            azimuthError = 360f - azimuthError;
        }

        float elevationError = Mathf.Abs(gunElevation - pendingElevation);

        return Mathf.Max(azimuthError, elevationError);
    }

    private void Update()
    {
        if (IsInteractable && !HasReceivedCoordinates
            && recieveCoordinatesButton != null && recieveCoordinatesButton.IsPressed())
        {
            HasReceivedCoordinates = true;
            ClearFailureMessage();
            UpdateTargetText();
        }

        if (locked) return;
        float deltaTime = Time.deltaTime;
        UpdateAccelFromLevers(deltaTime);
        UpdateGunFromVelocity(deltaTime);
        UpdateTraverseSound(deltaTime);
        gunBase.localRotation = Quaternion.Euler(0f, -gunAzimuth, 0f);
        gunBarrel.localRotation = Quaternion.Euler(-gunElevation, 0f, 0f);
        UpdateGunText();

        // Tutorial
        if (TutorialManager.Instance != null && !hasDoneTutorial && HasReceivedCoordinates)
        {
            float maxError = GetError();
            if (maxError <= 5.0f)
            {
                TutorialManager.Instance.CompleteLesson("targeting");
                TutorialManager.Instance.TriggerLesson("fire");
                hasDoneTutorial = true;
            }
        }
    }

}
