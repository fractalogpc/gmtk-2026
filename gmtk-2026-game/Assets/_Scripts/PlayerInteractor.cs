using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractor : MonoBehaviour
{
    [Header("Raycast")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float interactDistance = 3f;
    [SerializeField] private float staticViewInteractDistance = 100f;
    [SerializeField] private LayerMask interactLayers = ~0;
    [Tooltip("Only colliders with this tag will be considered interactable. Leave empty to accept any IInteractable.")]
    [SerializeField] private string interactableTag = "Interactable";
    [SerializeField] private CrosshairManager crosshairManager;
    [Tooltip("Optional. If set, Escape/Cancel opens the pause menu when no interaction or static view needs to be exited first.")]
    [SerializeField] private PauseController pauseController;

    private IInteractable currentInteractable;
    private IStaticCamera activeStaticView;
    private PlayerInput playerInput;

    public bool IsInStaticView => activeStaticView != null;
    public bool IsInteracting => currentInteractable != null;

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

    public void OnCancel(InputAction.CallbackContext context)
    {
        if (!context.started) return;

        // Priority: end an active interaction first, then close a static view, then pause.
        // Only one action per press so the player never accidentally exits AND opens the pause menu.
        if (currentInteractable != null)
        {
            EndInteraction();
            return;
        }
        if (activeStaticView != null)
        {
            ExitStaticView();
            return;
        }
        if (pauseController != null)
        {
            pauseController.TogglePause();
        }
    }

    private void Update()
    {
        if (currentInteractable == null)
        {
            if (InteractableInView(out bool interactableEnabled, out bool staticCamera))
            {
                if (staticCamera)
                {
                    crosshairManager.ShowStaticCameraCrosshair();
                }
                else if (interactableEnabled)
                {
                    crosshairManager.ShowInteractableCrosshair();
                }
                else
                {
                    crosshairManager.ShowErrorCrosshair();
                }
            }
            else
            {
                crosshairManager.ShowRegularCrosshair();
            }
            return;
        }
        ApplyInteractionSettings(currentInteractable.DuringInteract(BuildData(GetInteractRay(), GetMouseDelta())));
    }

    private bool InteractableInView(out bool canInteract, out bool staticCamera)
    {
        canInteract = false;
        staticCamera = false;
        Ray ray = GetInteractRay();
        float distance = activeStaticView != null ? staticViewInteractDistance : interactDistance;
        if (!Physics.Raycast(ray, out RaycastHit hit, distance, interactLayers)) 
        {
            return false;
        }
        if (!string.IsNullOrEmpty(interactableTag) && !hit.collider.CompareTag(interactableTag)) {
            return false;
        }
        IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
        IStaticCamera staticView = hit.collider.GetComponentInParent<IStaticCamera>();
        if (staticView != null)
        {
            staticCamera = true;
            canInteract = true;
            return true;
        }
        if (interactable is DragLever lever && !lever.CanInteract)
        {
            return true;
        }
        if (interactable is MonoBehaviour mb && !mb.enabled) {
            return true;
        }
        canInteract = true;
        return true;
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
        Ray ray = GetInteractRay();
        float distance = activeStaticView != null ? staticViewInteractDistance : interactDistance;
        if (!Physics.Raycast(ray, out RaycastHit hit, distance, interactLayers)) return;
        if (!string.IsNullOrEmpty(interactableTag) && !hit.collider.CompareTag(interactableTag)) return;

        if (activeStaticView == null)
        {
            IStaticCamera staticView = hit.collider.GetComponentInParent<IStaticCamera>();
            if (staticView != null && staticView.StaticCamera != null)
            {
                EnterStaticView(staticView);
                return;
            }
        }

        IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
        if (interactable == null) return;
        if (interactable is MonoBehaviour mb && !mb.enabled) return;

        currentInteractable = interactable;
        ApplyInteractionSettings(currentInteractable.OnInteractStart(BuildData(ray, hit)));
    }

    private void EnterStaticView(IStaticCamera view)
    {
        activeStaticView = view;
        view.StaticCamera.gameObject.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        crosshairManager.SetCrosshairHidden(true);
        Debug.Log("Set crosshair hidden");
        SetPlayerControlEnabled(false);
        view.OnEnterView();
    }

    private void ExitStaticView()
    {
        activeStaticView.OnExitView();
        activeStaticView.StaticCamera.gameObject.SetActive(false);
        activeStaticView = null;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        crosshairManager.SetCrosshairHidden(false);
        SetPlayerControlEnabled(true);
    }

    private void SetPlayerControlEnabled(bool enabled)
    {
        InputAction look = playerInput.actions.FindAction("Look");
        InputAction move = playerInput.actions.FindAction("Move");
        if (enabled)
        {
            look?.Enable();
            move?.Enable();
        }
        else
        {
            look?.Disable();
            move?.Disable();
        }
    }

    private void ApplyInteractionSettings(InteractionSettings settings)
    {
        if (activeStaticView != null) return;
        SetPlayerControlEnabled(!settings.lockCameraAndMovement);
    }

    private void EndInteraction()
    {
        if (currentInteractable == null) return;
        ApplyInteractionSettings(currentInteractable.OnInteractEnd(BuildData(GetInteractRay(), GetMouseDelta())));
        currentInteractable = null;
    }

    private Ray GetInteractRay()
    {
        if (activeStaticView != null && Mouse.current != null && Camera.main != null)
        {
            return Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        }
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
