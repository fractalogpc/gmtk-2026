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

    [Header("Events")]
    [SerializeField] private UnityEvent<float> onValueChanged;
    [SerializeField] private UnityEvent<float> onAngleChanged;
    [SerializeField] private UnityEvent onReachedMax;
    [SerializeField] private UnityEvent onReachedMin;

    private Quaternion initialLocalRotation;
    private float currentAngle;
    private float dragStartLeverAngle;
    private float dragStartRayAngle;
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
        dragStartLeverAngle = currentAngle;
        dragStartRayAngle = GetRayAngle(data.ray);
        return new InteractionSettings(lockCameraAndMovement: true);
    }

    public InteractionSettings DuringInteract(InteractionData data)
    {
        float rayAngle = GetRayAngle(data.ray);
        float delta = Mathf.DeltaAngle(dragStartRayAngle, rayAngle);
        SetAngle(dragStartLeverAngle + delta);
        return new InteractionSettings(lockCameraAndMovement: true);
    }

    public InteractionSettings OnInteractEnd(InteractionData data)
    {
        return new InteractionSettings(lockCameraAndMovement: false);
    }

    private float GetRayAngle(Ray ray)
    {
        Vector3 axis = GetWorldAxis();

        // Intersect the cursor ray with a plane through the pivot facing the camera.
        // This always yields a stable world point regardless of how the camera is oriented
        // relative to the rotation axis.
        Plane cursorPlane = new(-ray.direction, leverPivot.position);
        if (!cursorPlane.Raycast(ray, out float enter)) return dragStartRayAngle;

        // Drop the axis-aligned component so the point lives on the rotation plane.
        Vector3 offset = ray.GetPoint(enter) - leverPivot.position;
        Vector3 inRotationPlane = Vector3.ProjectOnPlane(offset, axis);
        if (inRotationPlane.sqrMagnitude < 1e-6f) return dragStartRayAngle;

        Vector3 reference = GetWorldReference(axis);
        return Vector3.SignedAngle(reference, inRotationPlane, axis);
    }

    private Vector3 GetWorldAxis()
    {
        Quaternion baseRot = leverPivot.parent != null
            ? leverPivot.parent.rotation * initialLocalRotation
            : initialLocalRotation;
        return (baseRot * rotationAxis).normalized;
    }

    private static Vector3 GetWorldReference(Vector3 axis)
    {
        Vector3 candidate = Vector3.forward;
        if (Mathf.Abs(Vector3.Dot(axis, candidate)) > 0.99f) candidate = Vector3.up;
        return Vector3.ProjectOnPlane(candidate, axis).normalized;
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
