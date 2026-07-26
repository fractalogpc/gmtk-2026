using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using FMODUnity;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private StudioEventEmitter footstepEmitter;

    [Header("Look")]
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private float lookSensitivity = 0.1f;
    [SerializeField] private float minPitch = -85f;
    [SerializeField] private float maxPitch = 85f;
    [SerializeField] private bool invertY = false;
    [SerializeField] private float gravityStrengthMult = 1f;
    [SerializeField] private float gravityVelocity = 0;

    [Header("Zoom")]
    [SerializeField] private CinemachineCamera playerCamera;
    [SerializeField] private float defaultFov = 60f;
    [SerializeField] private float zoomedFov = 30f;
    [SerializeField] private float zoomSpeed = 10f;
    [Tooltip("Scales look sensitivity while zoomed (multiplied with lookSensitivity).")]
    [SerializeField] private float zoomedSensitivityMultiplier = 0.5f;

    private CharacterController controller;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private float pitch;
    private bool isZoomed;
    private bool canLook = true;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    public bool InvertY
    {
        get => invertY;
        set => invertY = value;
    }

    public float LookSensitivity
    {
        get => lookSensitivity;
        set => lookSensitivity = value;
    }

    public void SetLookEnabled(bool enabled)
    {
        canLook = enabled;
        if (!enabled)
        {
            lookInput = Vector2.zero;
        }
    }

    private void OnEnable()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Update()
    {
        HandleLook();
        HandleMove();
        HandleZoom();
        if (!controller.isGrounded)
        {
            gravityVelocity += Physics.gravity.y * Time.deltaTime * gravityStrengthMult;
            controller.Move(new Vector3(0, gravityVelocity, 0) * Time.deltaTime);
        }
        else
        {
            gravityVelocity = 0;
        }

        timeSinceLastFootstep += Time.deltaTime;
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }

    public void OnZoom(InputAction.CallbackContext context)
    {
        if (context.started) isZoomed = true;
        else if (context.canceled) isZoomed = false;
    }

    private void HandleLook()
    {
        if (!canLook) return;

        float sensitivity = lookSensitivity * (isZoomed ? zoomedSensitivityMultiplier : 1f);
        Vector2 look = lookInput * sensitivity;
        transform.Rotate(0f, look.x, 0f);

        float pitchDelta = invertY ? look.y : -look.y;
        pitch = Mathf.Clamp(pitch + pitchDelta, minPitch, maxPitch);

        if (cameraPivot != null)
            cameraPivot.localEulerAngles = new Vector3(pitch, 0f, 0f);
    }

    private void HandleZoom()
    {
        if (playerCamera == null) return;
        float targetFov = isZoomed ? zoomedFov : defaultFov;
        LensSettings lens = playerCamera.Lens;
        lens.FieldOfView = Mathf.Lerp(lens.FieldOfView, targetFov, Time.deltaTime * zoomSpeed);
        playerCamera.Lens = lens;
    }

    private void HandleMove()
    {
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        controller.Move(move * (moveSpeed * Time.deltaTime));

        if (new Vector2(controller.velocity.x, controller.velocity.z).magnitude > 0.1f)
        {
            HandleFootstep();
        }
    }

    const float FOOTSTEP_FREQUENCY = 0.5f;
    float timeSinceLastFootstep = 0f;

    private void HandleFootstep()
    {
        if (timeSinceLastFootstep > FOOTSTEP_FREQUENCY)
        {
            timeSinceLastFootstep = 0f;
            footstepEmitter.Play();
        }
    }
}
