using UnityEngine;
using UnityEngine.Events;

public class Button : MonoBehaviour, IInteractable
{

    [SerializeField] private Transform buttonTransform;
    [SerializeField] private Vector3 pressedPosition;
    [SerializeField] private float animationSpeed = 10f;

    private Vector3 originalPosition;
    private bool isPressed = false;
    public UnityEvent OnButtonPressed;
    public UnityEvent OnButtonReleased;

    private void Awake()
    {
        originalPosition = buttonTransform.position;
    }

    private void Update()
    {
        if (IsPressed())
        {
            buttonTransform.position = Vector3.Slerp(buttonTransform.position, originalPosition + pressedPosition, Time.deltaTime * animationSpeed);
        }
        else
        {
            buttonTransform.position = Vector3.Slerp(buttonTransform.position, originalPosition, Time.deltaTime * animationSpeed);
        }
    }

    public InteractionSettings OnInteractStart(InteractionData data)
    {
        OnButtonPressed?.Invoke();
        isPressed = true;
        return new InteractionSettings(false);
    }

    public InteractionSettings DuringInteract(InteractionData data)
    {
        return new InteractionSettings(false);
    }

    public InteractionSettings OnInteractEnd(InteractionData data)
    {
        isPressed = false;
        OnButtonReleased?.Invoke();
        return new InteractionSettings(false);
    }

    public bool IsPressed()
    {
        return isPressed;
    }
}
