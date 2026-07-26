using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public enum SliderDirectionMode
{
    Bidirectional,
    IncreaseOnly,
    DecreaseOnly,
}

public class DragSlider : MonoBehaviour, IInteractable
{
    [Header("Slide")]
    [SerializeField] private Transform sliderHandle;
    [Tooltip("Local direction (in the handle's parent space) the slider moves along.")]
    [SerializeField] private Vector3 slideAxis = Vector3.right;
    [SerializeField] private float minOffset = -0.5f;
    [SerializeField] private float maxOffset = 0.5f;
    [SerializeField] private float startOffset = 0f;

    [Header("Input")]
    [Tooltip("Local units per pixel of mouse motion along the slider's screen-space direction.")]
    [SerializeField] private float sensitivity = 0.005f;

    [Header("Segmentation")]
    [Tooltip("Number of discrete stops between min and max (inclusive). 0 or 1 = analog.")]
    [SerializeField] private int segments = 0;

    [Header("Motion")]
    [Tooltip("Speed of interpolation toward the target offset. 0 = snap instantly.")]
    [SerializeField] private float sliderSpeed = 0f;

    [Header("Behavior")]
    [Tooltip("Restrict which way the slider can be moved. Once at a position, motion the other way is ignored.")]
    [SerializeField] private SliderDirectionMode directionMode = SliderDirectionMode.Bidirectional;
    [Tooltip("If true, the slider returns to Default Offset when released.")]
    [SerializeField] private bool returnOnRelease = false;
    [Tooltip("Offset the slider returns to on release (only used when Return On Release is enabled). Bypasses direction restriction.")]
    [SerializeField] private float defaultOffset = 0f;

    [Header("Events")]
    [SerializeField] private UnityEvent<float> onValueChanged;
    [SerializeField] private UnityEvent<float> onOffsetChanged;
    [SerializeField] private UnityEvent onReachedMax;
    [SerializeField] private UnityEvent onReachedMin;

    private Vector3 initialLocalPosition;
    private float currentOffset;
    private float rawOffset;
    private float? overrideLerpSpeed;
    private Camera dragCamera;
    private CursorLockMode savedCursorLockMode;
    private bool savedCursorVisible;
    private Vector2 savedCursorPosition;
    private bool wasAtMax;
    private bool wasAtMin;
    private bool isInteracting;

    public float Offset => currentOffset;
    public float NormalizedValue => Mathf.InverseLerp(minOffset, maxOffset, currentOffset);

    private void Awake()
    {
        if (sliderHandle == null) sliderHandle = transform;
        initialLocalPosition = sliderHandle.localPosition;
        rawOffset = Mathf.Clamp(startOffset, minOffset, maxOffset);
        currentOffset = SnapOffset(rawOffset);
        sliderHandle.localPosition = TargetLocalPosition();
    }

    private void Update()
    {
        Vector3 target = TargetLocalPosition();
        float speed = overrideLerpSpeed ?? sliderSpeed;
        if (speed <= 0f)
        {
            sliderHandle.localPosition = target;
            overrideLerpSpeed = null;
            return;
        }
        sliderHandle.localPosition = Vector3.Lerp(sliderHandle.localPosition, target, Time.deltaTime * speed);
        if ((sliderHandle.localPosition - target).sqrMagnitude < 1e-6f)
        {
            sliderHandle.localPosition = target;
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
        rawOffset = currentOffset;
        overrideLerpSpeed = null;
        return new InteractionSettings(lockCameraAndMovement: true);
    }

    public InteractionSettings DuringInteract(InteractionData data)
    {
        if (!isInteracting) return new InteractionSettings(lockCameraAndMovement: false);
        if (dragCamera == null) return new InteractionSettings(lockCameraAndMovement: true);

        Vector3 axisWorld = GetWorldSlideAxis();
        if (axisWorld.sqrMagnitude < 1e-8f) return new InteractionSettings(lockCameraAndMovement: true);

        Vector3 handleScreen = dragCamera.WorldToScreenPoint(sliderHandle.position);
        Vector3 axisEndScreen = dragCamera.WorldToScreenPoint(sliderHandle.position + axisWorld);
        Vector2 axisScreen = (Vector2)(axisEndScreen - handleScreen);
        if (axisScreen.sqrMagnitude < 1e-4f) return new InteractionSettings(lockCameraAndMovement: true);

        Vector2 axisDir = axisScreen.normalized;
        float alignedMotion = Vector2.Dot(data.mouseDelta, axisDir);
        ApplyOffset(rawOffset + alignedMotion * sensitivity);

        return new InteractionSettings(lockCameraAndMovement: true);
    }

    public InteractionSettings OnInteractEnd(InteractionData data)
    {
        if (!isInteracting) return new InteractionSettings(lockCameraAndMovement: false);
        isInteracting = false;
        Cursor.lockState = savedCursorLockMode;
        Cursor.visible = savedCursorVisible;
        RestoreCursorPosition();
        if (returnOnRelease) ApplyOffset(defaultOffset, ignoreDirection: true);
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

    private Vector3 GetWorldSlideAxis()
    {
        Transform parent = sliderHandle.parent;
        return parent != null ? parent.TransformVector(slideAxis) : slideAxis;
    }

    private Vector3 TargetLocalPosition()
    {
        return initialLocalPosition + slideAxis.normalized * currentOffset;
    }

    private void ApplyOffset(float offset, bool ignoreDirection = false)
    {
        float target = Mathf.Clamp(offset, minOffset, maxOffset);
        if (!ignoreDirection)
        {
            if (directionMode == SliderDirectionMode.IncreaseOnly)
                target = Mathf.Max(target, rawOffset);
            else if (directionMode == SliderDirectionMode.DecreaseOnly)
                target = Mathf.Min(target, rawOffset);
        }

        rawOffset = target;
        float snapped = SnapOffset(rawOffset);
        if (Mathf.Approximately(snapped, currentOffset)) return;

        currentOffset = snapped;
        onOffsetChanged.Invoke(currentOffset);
        onValueChanged.Invoke(NormalizedValue);

        bool atMax = Mathf.Approximately(currentOffset, maxOffset);
        bool atMin = Mathf.Approximately(currentOffset, minOffset);
        if (atMax && !wasAtMax) onReachedMax.Invoke();
        if (atMin && !wasAtMin) onReachedMin.Invoke();
        wasAtMax = atMax;
        wasAtMin = atMin;

        float effectiveSpeed = overrideLerpSpeed ?? sliderSpeed;
        if (effectiveSpeed <= 0f || !enabled || !gameObject.activeInHierarchy)
        {
            sliderHandle.localPosition = TargetLocalPosition();
        }
    }

    private float SnapOffset(float offset)
    {
        if (segments < 2) return offset;
        float step = (maxOffset - minOffset) / (segments - 1);
        float snapped = minOffset + Mathf.Round((offset - minOffset) / step) * step;
        return Mathf.Clamp(snapped, minOffset, maxOffset);
    }

    public void SetOffset(float offset)
    {
        overrideLerpSpeed = null;
        ApplyOffset(offset, ignoreDirection: true);
    }

    public void SetOffset(float offset, float speed)
    {
        overrideLerpSpeed = speed;
        ApplyOffset(offset, ignoreDirection: true);
    }

    public void ResetSlider()
    {
        overrideLerpSpeed = null;
        ApplyOffset(defaultOffset, ignoreDirection: true);
    }

    public void ResetSlider(float speed)
    {
        if (speed != -1) overrideLerpSpeed = speed;
        ApplyOffset(defaultOffset, ignoreDirection: true);
    }

    public void SetBounds(float min, float max)
    {
        minOffset = min;
        maxOffset = max;
        ApplyOffset(Mathf.Clamp(rawOffset, min, max), ignoreDirection: true);
    }
}
