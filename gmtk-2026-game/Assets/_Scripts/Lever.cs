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

    public float CurrentAngle { get; private set; }
    public float NormalizedValue => Mathf.InverseLerp(minAngle, maxAngle, CurrentAngle);

    public InteractionSettings OnInteractStart(InteractionData data)
    {
        return new InteractionSettings(lockCameraAndMovement: true);
    }

    public InteractionSettings DuringInteract(InteractionData data)
    {
        return new InteractionSettings(lockCameraAndMovement: true);
    }

    public InteractionSettings OnInteractEnd(InteractionData data)
    {
        return new InteractionSettings(lockCameraAndMovement: false);
    }

}
