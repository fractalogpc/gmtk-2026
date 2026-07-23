using UnityEngine;
using UnityEngine.InputSystem;

public class BasicPlayerController : MonoBehaviour
{
    public static BasicPlayerController Instance { get; private set; }

    public Vector3 movementInput;

    public float speed = 5f;
    public Camera targetCamera;
    public LayerMask clickMask = ~0;
    public float arrivalThreshold = 0.05f;

    private Vector3? clickTarget;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ApplyPlayerState(transform);
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        Vector2 inputVector = context.ReadValue<Vector2>();
        Vector3 movement = new Vector3(inputVector.x, 0f, inputVector.y);
        movementInput = movement;

        if (movement.sqrMagnitude > 0f)
        {
            clickTarget = null;
        }
    }

    public void OnClick(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        Camera cam = targetCamera != null ? targetCamera : Camera.main;
        if (cam == null) return;

        Vector2 pointerPos = Pointer.current != null ? Pointer.current.position.ReadValue() : (Vector2)Input.mousePosition;
        Ray ray = cam.ScreenPointToRay(pointerPos);

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, clickMask))
        {
            Interactable interactable = hit.collider.GetComponentInParent<Interactable>();
            if (interactable != null && interactable.TryInteract(transform.position))
            {
                return;
            }

            Vector3 target = hit.point;
            target.y = transform.position.y;
            clickTarget = target;
        }
    }

    public void Update()
    {
        if (movementInput.sqrMagnitude > 0f)
        {
            transform.Translate(movementInput * speed * Time.deltaTime);
            return;
        }

        if (clickTarget.HasValue)
        {
            Vector3 target = clickTarget.Value;
            Vector3 delta = target - transform.position;
            float distance = delta.magnitude;

            if (distance <= arrivalThreshold)
            {
                transform.position = target;
                clickTarget = null;
                return;
            }

            Vector3 step = delta / distance * speed * Time.deltaTime;
            if (step.magnitude >= distance)
            {
                transform.position = target;
                clickTarget = null;
            }
            else
            {
                transform.position += step;
            }
        }
    }
}
