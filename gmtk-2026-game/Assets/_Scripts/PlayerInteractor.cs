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

    private void Awake()
    {
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
        currentInteractable.OnInteractDrag(BuildData(GetAimRay()));
    }

    private void TryStart()
    {
        Ray ray = GetAimRay();
        if (!Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactLayers)) return;
        if (!string.IsNullOrEmpty(interactableTag) && !hit.collider.CompareTag(interactableTag)) return;

        IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
        if (interactable == null) return;

        currentInteractable = interactable;
        currentInteractable.OnInteractStart(BuildData(ray, hit));
    }

    private void EndInteraction()
    {
        if (currentInteractable == null) return;
        currentInteractable.OnInteractEnd(BuildData(GetAimRay()));
        currentInteractable = null;
    }

    private Ray GetAimRay()
    {
        Transform cam = playerCamera != null ? playerCamera.transform : transform;
        return new Ray(cam.position, cam.forward);
    }

    private InteractionData BuildData(Ray ray)
    {
        return new InteractionData
        {
            interactor = transform,
            ray = ray,
            hitPoint = ray.origin + ray.direction * interactDistance,
            hitNormal = -ray.direction
        };
    }

    private InteractionData BuildData(Ray ray, RaycastHit hit)
    {
        return new InteractionData
        {
            interactor = transform,
            ray = ray,
            hitPoint = hit.point,
            hitNormal = hit.normal
        };
    }
}
