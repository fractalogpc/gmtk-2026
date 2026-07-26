using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public enum DialDirectionMode
{
    Bidirectional,
    IncreaseOnly,
    DecreaseOnly,
}

public class DragDial : MonoBehaviour, IInteractable
{
    [Header("Rotation")]
    [SerializeField] private Transform dialPivot;
    [SerializeField] private Vector3 rotationAxis = Vector3.forward;
    [Tooltip("Local direction the indicator points from the pivot. Must be perpendicular to Rotation Axis.")]
    [SerializeField] private Vector3 indicatorAxis = Vector3.up;
    [Tooltip("If true, the dial spins freely with no bounds. Min/Max Angle are ignored.")]
    [SerializeField] private bool continuous = true;
    [SerializeField] private float minAngle = -180f;
    [SerializeField] private float maxAngle = 180f;
    [SerializeField] private float startAngle = 0f;

    [Header("Input")]
    [Tooltip("Degrees per pixel of mouse motion along the dial's screen-space tangent.")]
    [SerializeField] private float sensitivity = 0.5f;

    [Header("Segmentation")]
    [Tooltip("Number of discrete stops. For bounded dials, spread between min and max (inclusive). For continuous dials, spread evenly around 360°. 0 or 1 = analog.")]
    [SerializeField] private int segments = 0;

    [Header("Motion")]
    [Tooltip("Speed of interpolation toward the target angle. 0 = snap instantly.")]
    [SerializeField] private float dialSpeed = 0f;

    [Header("Behavior")]
    [Tooltip("Restrict which way the dial can be turned. Once at a position, motion the other way is ignored.")]
    [SerializeField] private DialDirectionMode directionMode = DialDirectionMode.Bidirectional;
    [Tooltip("If true, the dial returns to Default Angle when released.")]
    [SerializeField] private bool returnOnRelease = false;
    [Tooltip("Angle the dial returns to on release (only used when Return On Release is enabled). Bypasses direction restriction.")]
    [SerializeField] private float defaultAngle = 0f;

    [Header("Events")]
    [SerializeField] private UnityEvent<float> onValueChanged;
    [SerializeField] private UnityEvent<float> onAngleChanged;
    [SerializeField] private UnityEvent onReachedMax;
    [SerializeField] private UnityEvent onReachedMin;

    private Quaternion initialLocalRotation;
    private float currentAngle;
    private float rawAngle;
    private float? overrideLerpSpeed;
    private Camera dragCamera;
    private CursorLockMode savedCursorLockMode;
    private bool savedCursorVisible;
    private Vector2 savedCursorPosition;
    private bool wasAtMax;
    private bool wasAtMin;
    private bool isInteracting;

    public float Angle => currentAngle;
    public float NormalizedValue => continuous
        ? Mathf.Repeat(currentAngle, 360f) / 360f
        : Mathf.InverseLerp(minAngle, maxAngle, currentAngle);

    private void Awake()
    {
        if (dialPivot == null) dialPivot = transform;
        initialLocalRotation = dialPivot.localRotation;
        rawAngle = continuous ? startAngle : Mathf.Clamp(startAngle, minAngle, maxAngle);
        currentAngle = SnapAngle(rawAngle);
        dialPivot.localRotation = TargetRotation();
    }

    private void Update()
    {
        Quaternion target = TargetRotation();
        float speed = overrideLerpSpeed ?? dialSpeed;
        if (speed <= 0f)
        {
            dialPivot.localRotation = target;
            overrideLerpSpeed = null;
            return;
        }
        dialPivot.localRotation = Quaternion.Slerp(dialPivot.localRotation, target, Time.deltaTime * speed);
        if (Quaternion.Angle(dialPivot.localRotation, target) < 0.1f)
        {
            dialPivot.localRotation = target;
            overrideLerpSpeed = null;
        }
    }

    public InteractionSettings OnInteractStart(InteractionData data)
    {
        if (!enabled) return new InteractionSettings(lockCameraAndMovement: false);
        isInteracting = true;
        dragCamera = Camera.main;
        savedCursorLockMode = Cursor.lockState;
        savedCursorVisible = Cursor.visible;
        savedCursorPosition = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        rawAngle = currentAngle;
        overrideLerpSpeed = null;
        return new InteractionSettings(lockCameraAndMovement: true);
    }

    public InteractionSettings DuringInteract(InteractionData data)
    {
        if (!isInteracting) return new InteractionSettings(lockCameraAndMovement: false);
        if (dragCamera == null) return new InteractionSettings(lockCameraAndMovement: true);

        Vector3 axis = GetWorldAxis();
        Vector3 handleDir = GetHandleDirection(axis);
        if (handleDir == Vector3.zero) return new InteractionSettings(lockCameraAndMovement: true);

        Vector3 tangentWorld = Vector3.Cross(axis, handleDir);
        Vector3 pivotScreen = dragCamera.WorldToScreenPoint(dialPivot.position);
        Vector3 tangentEndScreen = dragCamera.WorldToScreenPoint(dialPivot.position + tangentWorld);
        Vector2 tangentScreen = (Vector2)(tangentEndScreen - pivotScreen);
        if (tangentScreen.sqrMagnitude < 1e-4f) return new InteractionSettings(lockCameraAndMovement: true);

        Vector2 tangentDir = tangentScreen.normalized;
        float alignedMotion = Vector2.Dot(data.mouseDelta, tangentDir);
        ApplyAngle(rawAngle + alignedMotion * sensitivity);

        return new InteractionSettings(lockCameraAndMovement: true);
    }

    public InteractionSettings OnInteractEnd(InteractionData data)
    {
        if (!isInteracting) return new InteractionSettings(lockCameraAndMovement: false);
        isInteracting = false;
        Cursor.lockState = savedCursorLockMode;
        Cursor.visible = savedCursorVisible;
        RestoreCursorPosition();
        if (returnOnRelease) ApplyAngle(defaultAngle, ignoreDirection: true);
        return new InteractionSettings(lockCameraAndMovement: false);
    }

    public void SetInteractionEnabled(bool value)
    {
        if (!value) SafeDisable();
        else enabled = true;
    }

    public void SafeDisable()
    {
        if (isInteracting)
        {
            Cursor.lockState = savedCursorLockMode;
            Cursor.visible = savedCursorVisible;
            RestoreCursorPosition();
            overrideLerpSpeed = null;
            isInteracting = false;
        }
        enabled = false;
    }

    private void RestoreCursorPosition()
    {
        if (savedCursorLockMode == CursorLockMode.Locked || Mouse.current == null) return;
        Mouse.current.WarpCursorPosition(savedCursorPosition);
    }

    private Vector3 GetHandleDirection(Vector3 axis)
    {
        // Dials can spin freely, so the tangent tracks the CURRENT indicator direction
        // rather than the rest pose — this keeps the drag direction correct at all angles.
        Quaternion baseRot = dialPivot.parent != null
            ? dialPivot.parent.rotation * dialPivot.localRotation
            : dialPivot.localRotation;
        Vector3 worldIndicator = baseRot * indicatorAxis;
        Vector3 projected = Vector3.ProjectOnPlane(worldIndicator, axis);
        return projected.sqrMagnitude < 1e-6f ? Vector3.zero : projected.normalized;
    }

    private Vector3 GetWorldAxis()
    {
        Quaternion baseRot = dialPivot.parent != null
            ? dialPivot.parent.rotation * initialLocalRotation
            : initialLocalRotation;
        return (baseRot * rotationAxis).normalized;
    }

    private Quaternion TargetRotation()
    {
        return initialLocalRotation * Quaternion.AngleAxis(currentAngle, rotationAxis);
    }

    private void ApplyAngle(float angle, bool ignoreDirection = false)
    {
        float target = continuous ? angle : Mathf.Clamp(angle, minAngle, maxAngle);
        if (!ignoreDirection)
        {
            if (directionMode == DialDirectionMode.IncreaseOnly)
                target = Mathf.Max(target, rawAngle);
            else if (directionMode == DialDirectionMode.DecreaseOnly)
                target = Mathf.Min(target, rawAngle);
        }

        rawAngle = target;
        float snapped = SnapAngle(rawAngle);
        if (Mathf.Approximately(snapped, currentAngle)) return;

        currentAngle = snapped;
        onAngleChanged.Invoke(currentAngle);
        onValueChanged.Invoke(NormalizedValue);

        if (!continuous)
        {
            bool atMax = Mathf.Approximately(currentAngle, maxAngle);
            bool atMin = Mathf.Approximately(currentAngle, minAngle);
            if (atMax && !wasAtMax) onReachedMax.Invoke();
            if (atMin && !wasAtMin) onReachedMin.Invoke();
            wasAtMax = atMax;
            wasAtMin = atMin;
        }

        float effectiveSpeed = overrideLerpSpeed ?? dialSpeed;
        if (effectiveSpeed <= 0f || !enabled || !gameObject.activeInHierarchy)
        {
            dialPivot.localRotation = TargetRotation();
        }
    }

    private float SnapAngle(float angle)
    {
        if (segments < 2) return angle;
        if (continuous)
        {
            float step = 360f / segments;
            return Mathf.Round(angle / step) * step;
        }
        float boundedStep = (maxAngle - minAngle) / (segments - 1);
        float snapped = minAngle + Mathf.Round((angle - minAngle) / boundedStep) * boundedStep;
        return Mathf.Clamp(snapped, minAngle, maxAngle);
    }

    public void SetAngle(float angle)
    {
        overrideLerpSpeed = null;
        ApplyAngle(angle, ignoreDirection: true);
    }

    public void SetAngle(float angle, float speed)
    {
        overrideLerpSpeed = speed;
        ApplyAngle(angle, ignoreDirection: true);
    }

    public void ResetDial()
    {
        overrideLerpSpeed = null;
        ApplyAngle(defaultAngle, ignoreDirection: true);
    }

    public void ResetDial(float speed)
    {
        if (speed != -1) overrideLerpSpeed = speed;
        ApplyAngle(defaultAngle, ignoreDirection: true);
    }

    public void SetBounds(float min, float max)
    {
        continuous = false;
        minAngle = min;
        maxAngle = max;
        ApplyAngle(Mathf.Clamp(rawAngle, min, max), ignoreDirection: true);
    }
}
