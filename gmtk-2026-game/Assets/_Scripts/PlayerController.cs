using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Look")]
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private float lookSensitivity = 0.1f;
    [SerializeField] private float minPitch = -85f;
    [SerializeField] private float maxPitch = 85f;
    [SerializeField] private bool invertY = false;

    private CharacterController controller;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private float pitch;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
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
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }

    private void HandleLook()
    {
        Vector2 look = lookInput * lookSensitivity;
        transform.Rotate(0f, look.x, 0f);

        float pitchDelta = invertY ? look.y : -look.y;
        pitch = Mathf.Clamp(pitch + pitchDelta, minPitch, maxPitch);

        if (cameraPivot != null)
            cameraPivot.localEulerAngles = new Vector3(pitch, 0f, 0f);
    }

    private void HandleMove()
    {
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        controller.Move(move * (moveSpeed * Time.deltaTime));
    }
}
