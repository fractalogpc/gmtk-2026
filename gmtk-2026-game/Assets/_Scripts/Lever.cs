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
    [SerializeField] private bool useHorizontalInput = false;
    [SerializeField] private float leverSpeed = 20f;
    [Header("Settings")]
    [SerializeField] private bool snappingEnabled = false;
    [SerializeField] private float snapIncrement = 5f;
    [Header("Input")]
    [SerializeField] private float inputSensitivity = 1f;

    [Header("Events")]
    [SerializeField] private UnityEvent<float> onValueChanged;
    [SerializeField] private UnityEvent<float> onAngleChanged;
    [SerializeField] private UnityEvent onReachedMax;
    [SerializeField] private UnityEvent onReachedMin;
    private Quaternion initialRotation;
    public float CurrentAngle => snappingEnabled ? Mathf.Round(RawCurrentAngle / snapIncrement) * snapIncrement : RawCurrentAngle;
    public float RawCurrentAngle { get; private set; }
    public float NormalizedValue => Mathf.InverseLerp(minAngle, maxAngle, RawCurrentAngle);

    public InteractionSettings OnInteractStart(InteractionData data)
    {
        lastRawAngle = RawCurrentAngle;
        lastCurrentAngle = CurrentAngle;
        return new InteractionSettings(lockCameraAndMovement: true);
    }

    public InteractionSettings DuringInteract(InteractionData data)
    {
        float input = useHorizontalInput ? data.mouseDelta.x : data.mouseDelta.y;
        input *= inputSensitivity;
        RawCurrentAngle = Mathf.Clamp(RawCurrentAngle + input, minAngle, maxAngle);
        return new InteractionSettings(lockCameraAndMovement: true);
    }

    public InteractionSettings OnInteractEnd(InteractionData data)
    {
        return new InteractionSettings(lockCameraAndMovement: false);
    }

    private void SendEvents()
    {
        onValueChanged?.Invoke(NormalizedValue);
        onAngleChanged?.Invoke(RawCurrentAngle);
        if (Mathf.Approximately(RawCurrentAngle, maxAngle))
        {
            onReachedMax?.Invoke();
        }
        else if (Mathf.Approximately(RawCurrentAngle, minAngle))
        {
            onReachedMin?.Invoke();
        }
    }

    private void Start()
    {
        RawCurrentAngle = Mathf.Clamp(startAngle, minAngle, maxAngle);
        initialRotation = leverPivot.localRotation;
        leverPivot.localRotation = initialRotation * Quaternion.AngleAxis(RawCurrentAngle, rotationAxis);
    }

    private float lastRawAngle = 0f;
    private float lastCurrentAngle = 0f;
    private void Update()
    {
        if (snappingEnabled)
        {
            if (!Mathf.Approximately(lastCurrentAngle, CurrentAngle)) SendEvents(); 
            float snappedAngle = Mathf.Round(RawCurrentAngle / snapIncrement) * snapIncrement;
            Quaternion targetRotation = initialRotation * Quaternion.AngleAxis(snappedAngle, rotationAxis);
            leverPivot.localRotation = Quaternion.Slerp(leverPivot.localRotation, targetRotation, Time.deltaTime * leverSpeed);
            lastCurrentAngle = CurrentAngle;
        }
        else
        {
            if (!Mathf.Approximately(lastRawAngle, RawCurrentAngle)) SendEvents();
            Quaternion targetRotation = initialRotation * Quaternion.AngleAxis(RawCurrentAngle, rotationAxis);
            leverPivot.localRotation = Quaternion.Slerp(leverPivot.localRotation, targetRotation, Time.deltaTime * leverSpeed);
            lastRawAngle = RawCurrentAngle;
        }
    }
}
