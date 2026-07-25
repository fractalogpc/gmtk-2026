using UnityEngine;
using UnityEngine.Events;

public enum LeverDirectionMode
{
    Bidirectional,
    IncreaseOnly,
    DecreaseOnly,
}

public class DragLever : MonoBehaviour, IInteractable
{
    [Header("Rotation")]
    [SerializeField] private Transform leverPivot;
    [SerializeField] private Vector3 rotationAxis = Vector3.right;
    [Tooltip("Local direction the handle points from the pivot. Must be perpendicular to Rotation Axis.")]
    [SerializeField] private Vector3 handleAxis = Vector3.up;
    [SerializeField] private float minAngle = -45f;
    [SerializeField] private float maxAngle = 45f;
    [SerializeField] private float startAngle = 0f;

    [Header("Input")]
    [Tooltip("Degrees per pixel of mouse motion along the lever's screen-space tangent.")]
    [SerializeField] private float sensitivity = 0.5f;

    [Header("Segmentation")]
    [Tooltip("Number of discrete stops between min and max (inclusive). 0 or 1 = analog.")]
    [SerializeField] private int segments = 0;

    [Header("Motion")]
    [Tooltip("Speed of interpolation toward the target angle. 0 = snap instantly.")]
    [SerializeField] private float leverSpeed = 0f;

    [Header("Behavior")]
    [Tooltip("Restrict which way the lever can be moved. Once at a position, motion the other way is ignored.")]
    [SerializeField] private LeverDirectionMode directionMode = LeverDirectionMode.Bidirectional;
    [Tooltip("If true, the lever returns to Default Angle when released.")]
    [SerializeField] private bool returnOnRelease = false;
    [Tooltip("Angle the lever returns to on release (only used when Return On Release is enabled). Bypasses direction restriction.")]
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
    private bool wasAtMax;
    private bool wasAtMin;
    private bool isInteracting;

    public float Angle => currentAngle;
    public float NormalizedValue => Mathf.InverseLerp(minAngle, maxAngle, currentAngle);

    private void Awake()
    {
        if (leverPivot == null) leverPivot = transform;
        initialLocalRotation = leverPivot.localRotation;
        rawAngle = Mathf.Clamp(startAngle, minAngle, maxAngle);
        currentAngle = SnapAngle(rawAngle);
        leverPivot.localRotation = TargetRotation();
    }

    private void Update()
    {
        Quaternion target = TargetRotation();
        float speed = overrideLerpSpeed ?? leverSpeed;
        if (speed <= 0f)
        {
            leverPivot.localRotation = target;
            overrideLerpSpeed = null;
            return;
        }
        leverPivot.localRotation = Quaternion.Slerp(leverPivot.localRotation, target, Time.deltaTime * speed);
        if (Quaternion.Angle(leverPivot.localRotation, target) < 0.1f)
        {
            leverPivot.localRotation = target;
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
        Vector3 pivotScreen = dragCamera.WorldToScreenPoint(leverPivot.position);
        Vector3 tangentEndScreen = dragCamera.WorldToScreenPoint(leverPivot.position + tangentWorld);
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
            overrideLerpSpeed = null;
            isInteracting = false;
        }
        enabled = false;
    }

    private Vector3 GetHandleDirection(Vector3 axis)
    {
        // Use the pivot's REST rotation (parent + initialLocalRotation) so the drag direction
        // stays fixed for the whole interaction — the tangent doesn't rotate with the handle.
        Quaternion baseRot = leverPivot.parent != null
            ? leverPivot.parent.rotation * initialLocalRotation
            : initialLocalRotation;
        Vector3 worldHandle = baseRot * handleAxis;
        Vector3 projected = Vector3.ProjectOnPlane(worldHandle, axis);
        return projected.sqrMagnitude < 1e-6f ? Vector3.zero : projected.normalized;
    }

    private Vector3 GetWorldAxis()
    {
        Quaternion baseRot = leverPivot.parent != null
            ? leverPivot.parent.rotation * initialLocalRotation
            : initialLocalRotation;
        return (baseRot * rotationAxis).normalized;
    }

    private Quaternion TargetRotation()
    {
        return initialLocalRotation * Quaternion.AngleAxis(currentAngle, rotationAxis);
    }

    private void ApplyAngle(float angle, bool ignoreDirection = false)
    {
        float target = Mathf.Clamp(angle, minAngle, maxAngle);
        if (!ignoreDirection)
        {
            if (directionMode == LeverDirectionMode.IncreaseOnly)
                target = Mathf.Max(target, rawAngle);
            else if (directionMode == LeverDirectionMode.DecreaseOnly)
                target = Mathf.Min(target, rawAngle);
        }

        rawAngle = target;
        float snapped = SnapAngle(rawAngle);
        if (Mathf.Approximately(snapped, currentAngle)) return;

        currentAngle = snapped;
        onAngleChanged.Invoke(currentAngle);
        onValueChanged.Invoke(NormalizedValue);

        bool atMax = Mathf.Approximately(currentAngle, maxAngle);
        bool atMin = Mathf.Approximately(currentAngle, minAngle);
        if (atMax && !wasAtMax) onReachedMax.Invoke();
        if (atMin && !wasAtMin) onReachedMin.Invoke();
        wasAtMax = atMax;
        wasAtMin = atMin;
    }

    private float SnapAngle(float angle)
    {
        if (segments < 2) return angle;
        float step = (maxAngle - minAngle) / (segments - 1);
        float snapped = minAngle + Mathf.Round((angle - minAngle) / step) * step;
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

    public void ResetLever()
    {
        overrideLerpSpeed = null;
        ApplyAngle(defaultAngle, ignoreDirection: true);
    }

    public void ResetLever(float speed)
    {
        if (speed != -1) overrideLerpSpeed = speed;
        ApplyAngle(defaultAngle, ignoreDirection: true);
    }
}
