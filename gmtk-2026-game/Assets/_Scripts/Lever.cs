using UnityEngine;
using UnityEngine.Events;

public class Lever : MonoBehaviour, IInteractable
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

    public void OnInteractStart(InteractionData data)
    {
        dragStartLeverAngle = currentAngle;
        dragStartRayAngle = GetRayAngle(data.ray);
    }

    public void OnInteractDrag(InteractionData data)
    {
        float rayAngle = GetRayAngle(data.ray);
        SetAngle(dragStartLeverAngle + (rayAngle - dragStartRayAngle));
    }

    public void OnInteractEnd(InteractionData data) { }

    private float GetRayAngle(Ray ray)
    {
        Vector3 axis = GetWorldAxis();
        Plane plane = new Plane(axis, leverPivot.position);
        if (!plane.Raycast(ray, out float enter))
            return dragStartRayAngle;

        Vector3 dir = ray.GetPoint(enter) - leverPivot.position;
        Vector3 reference = GetWorldReference(axis);
        return Vector3.SignedAngle(reference, dir, axis);
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
