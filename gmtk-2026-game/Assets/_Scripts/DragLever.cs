using UnityEngine;
using UnityEngine.Events;

public class DragLever : MonoBehaviour, IInteractable
{
    [Header("Rotation")]
    [SerializeField] private Transform leverPivot;
    [SerializeField] private Vector3 rotationAxis = Vector3.right;
    [SerializeField] private float minAngle = -45f;
    [SerializeField] private float maxAngle = 45f;
    [SerializeField] private float startAngle = 0f;

    [Header("Input")]
    [Tooltip("Degrees per pixel of mouse motion along the lever's screen-space tangent.")]
    [SerializeField] private float sensitivity = 0.5f;

    [Header("Events")]
    [SerializeField] private UnityEvent<float> onValueChanged;
    [SerializeField] private UnityEvent<float> onAngleChanged;
    [SerializeField] private UnityEvent onReachedMax;
    [SerializeField] private UnityEvent onReachedMin;

    private Quaternion initialLocalRotation;
    private float currentAngle;
    private Camera dragCamera;
    private CursorLockMode savedCursorLockMode;
    private bool savedCursorVisible;
    private bool wasAtMax;
    private bool wasAtMin;

    public float Angle => currentAngle;
    public float NormalizedValue => Mathf.InverseLerp(minAngle, maxAngle, currentAngle);

    private void Awake()
    {
        if (leverPivot == null) leverPivot = transform;
        initialLocalRotation = leverPivot.localRotation;
        currentAngle = Mathf.Clamp(startAngle, minAngle, maxAngle);
        ApplyRotation();
    }

    public InteractionSettings OnInteractStart(InteractionData data)
    {
        dragCamera = Camera.main;
        savedCursorLockMode = Cursor.lockState;
        savedCursorVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        return new InteractionSettings(lockCameraAndMovement: true);
    }

    public InteractionSettings DuringInteract(InteractionData data)
    {
        if (dragCamera == null) return new InteractionSettings(lockCameraAndMovement: true);

        Vector3 axis = GetWorldAxis();
        Vector3 handleDir = GetHandleDirection(axis);
        Vector3 tangentWorld = Vector3.Cross(axis, handleDir);

        Vector3 pivotScreen = dragCamera.WorldToScreenPoint(leverPivot.position);
        Vector3 tangentEndScreen = dragCamera.WorldToScreenPoint(leverPivot.position + tangentWorld);
        Vector2 tangentScreen = (Vector2)(tangentEndScreen - pivotScreen);
        if (tangentScreen.sqrMagnitude < 1e-4f) return new InteractionSettings(lockCameraAndMovement: true);

        Vector2 tangentDir = tangentScreen.normalized;
        float alignedMotion = Vector2.Dot(data.mouseDelta, tangentDir);
        SetAngle(currentAngle + alignedMotion * sensitivity);

        return new InteractionSettings(lockCameraAndMovement: true);
    }

    public InteractionSettings OnInteractEnd(InteractionData data)
    {
        Cursor.lockState = savedCursorLockMode;
        Cursor.visible = savedCursorVisible;
        return new InteractionSettings(lockCameraAndMovement: false);
    }

    private Vector3 GetHandleDirection(Vector3 axis)
    {
        // A unit direction perpendicular to the axis that rotates with the lever,
        // used to compute the tangent (direction the tip is moving right now).
        Vector3 candidate = leverPivot.up;
        Vector3 projected = Vector3.ProjectOnPlane(candidate, axis);
        if (projected.sqrMagnitude < 1e-4f)
        {
            candidate = leverPivot.forward;
            projected = Vector3.ProjectOnPlane(candidate, axis);
        }
        return projected.normalized;
    }

    private Vector3 GetWorldAxis()
    {
        Quaternion baseRot = leverPivot.parent != null
            ? leverPivot.parent.rotation * initialLocalRotation
            : initialLocalRotation;
        return (baseRot * rotationAxis).normalized;
    }

    private void SetAngle(float angle)
    {
        float clamped = Mathf.Clamp(angle, minAngle, maxAngle);
        if (Mathf.Approximately(clamped, currentAngle)) return;

        currentAngle = clamped;
        ApplyRotation();
        onAngleChanged.Invoke(currentAngle);
        onValueChanged.Invoke(NormalizedValue);

        bool atMax = Mathf.Approximately(currentAngle, maxAngle);
        bool atMin = Mathf.Approximately(currentAngle, minAngle);
        if (atMax && !wasAtMax) onReachedMax.Invoke();
        if (atMin && !wasAtMin) onReachedMin.Invoke();
        wasAtMax = atMax;
        wasAtMin = atMin;
    }

    private void ApplyRotation()
    {
        leverPivot.localRotation = initialLocalRotation * Quaternion.AngleAxis(currentAngle, rotationAxis);
    }
}
