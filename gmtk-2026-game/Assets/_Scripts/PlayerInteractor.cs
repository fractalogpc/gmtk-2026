using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractor : MonoBehaviour
{
    [Header("Raycast")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float interactDistance = 3f;
    [SerializeField] private LayerMask interactLayers = ~0;
    [Tooltip("Only colliders with this tag will be considered interactable. Leave empty to accept any IInteractable.")]
    [SerializeField] private string interactableTag = "Interactable";

    private IInteractable currentInteractable;
    private PlayerInput playerInput;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        if (playerCamera == null) playerCamera = Camera.main;
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.started) TryStart();
        else if (context.canceled) EndInteraction();
    }

    private void Update()
    {
        if (currentInteractable == null) return;
        ApplyInteractionSettings(currentInteractable.DuringInteract(BuildData(GetAimRay(), GetMouseDelta())));
    }

    private Vector2 GetMouseDelta()
    {
        if (playerInput == null || playerInput.currentControlScheme != "Keyboard&Mouse")
        {
            return Vector2.zero;
        }
        return playerInput.actions.FindAction("Mouse").ReadValue<Vector2>();
    }

    private void TryStart()
    {
        Ray ray = GetAimRay();
        if (!Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactLayers)) return;
        if (!string.IsNullOrEmpty(interactableTag) && !hit.collider.CompareTag(interactableTag)) return;

        IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
        if (interactable == null) return;

        currentInteractable = interactable;
        ApplyInteractionSettings(currentInteractable.OnInteractStart(BuildData(ray, hit)));
    }

    private void ApplyInteractionSettings(InteractionSettings settings)
    {
        if (settings.lockCameraAndMovement)
        {
            playerInput.actions.FindAction("Look").Disable();
            playerInput.actions.FindAction("Move").Disable();
        }
        else
        {
            playerInput.actions.FindAction("Look").Enable();
            playerInput.actions.FindAction("Move").Enable();
        }
    }

    private void EndInteraction()
    {
        if (currentInteractable == null) return;
        ApplyInteractionSettings(currentInteractable.OnInteractEnd(BuildData(GetAimRay(), GetMouseDelta())));
        currentInteractable = null;
    }

    private Ray GetAimRay()
    {
        Transform cam = playerCamera != null ? playerCamera.transform : transform;
        return new Ray(cam.position, cam.forward);
    }

    private InteractionData BuildData(Ray ray, Vector2 mouseDelta)
    {
        return new InteractionData
        {
            interactor = transform,
            mouseDelta = mouseDelta,
            ray = ray,
        };
    }

    private InteractionData BuildData(Ray ray, RaycastHit hit)
    {
        return new InteractionData
        {
            interactor = transform,
            ray = ray,
        };
    }
}
