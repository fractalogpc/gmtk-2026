using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;

public class StaticCameraView : MonoBehaviour, IStaticCamera
{
    [SerializeField] private CinemachineCamera staticCamera;
    [Tooltip("Interactable scripts to enable while this view is active, and disable when it closes. Drop the script component (must implement IInteractable).")]
    [SerializeField] private MonoBehaviour[] managedObjects;

    [SerializeField] private UnityEvent onEnter;
    [SerializeField] private UnityEvent onExit;

    public CinemachineCamera StaticCamera => staticCamera;

    private Collider[] ownColliders;
    private bool[] deactivated;

    private void Awake()
    {
        ownColliders = GetComponents<Collider>();
        deactivated = new bool[managedObjects != null ? managedObjects.Length : 0];
        SetManagedEnabled(false);
    }

    public void OnEnterView()
    {
        SetOwnCollidersEnabled(false);
        SetManagedEnabled(true);
        onEnter.Invoke();
    }

    public void OnExitView()
    {
        SetManagedEnabled(false);
        SetOwnCollidersEnabled(true);
        onExit.Invoke();
    }

    private void SetOwnCollidersEnabled(bool value)
    {
        foreach (Collider col in ownColliders) col.enabled = value;
    }

    private void SetManagedEnabled(bool value)
    {
        for (int i = 0; i < managedObjects.Length; i++)
        {
            if (managedObjects[i] is IInteractable interactable)
            {
                bool target = value && !deactivated[i];
                interactable.SetInteractionEnabled(target);
            }
        }
    }

    public void DeactivateManagedObjects()
    {
        for (int i = 0; i < managedObjects.Length; i++)
        {
            deactivated[i] = true;
            if (managedObjects[i] is IInteractable interactable)
                interactable.SetInteractionEnabled(false);
        }
    }

    public void ReactivateManagedObjects()
    {
        for (int i = 0; i < managedObjects.Length; i++)
        {
            deactivated[i] = false;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (managedObjects == null) return;
        for (int i = 0; i < managedObjects.Length; i++)
        {
            if (managedObjects[i] != null && managedObjects[i] is not IInteractable)
            {
                Debug.LogWarning($"{managedObjects[i].name} on {name}'s managed list does not implement IInteractable.", this);
            }
        }
    }
#endif
}
