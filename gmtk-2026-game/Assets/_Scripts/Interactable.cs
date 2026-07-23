using UnityEngine;
using UnityEngine.Events;

public class Interactable : MonoBehaviour, IInteractable
{
    [SerializeField] private UnityEvent<InteractionData> onInteractStart;
    [SerializeField] private UnityEvent<InteractionData> onInteractDrag;
    [SerializeField] private UnityEvent<InteractionData> onInteractEnd;

    public bool IsBeingInteracted { get; private set; }
    public InteractionData CurrentInteraction { get; private set; }

    public void OnInteractStart(InteractionData data)
    {
        IsBeingInteracted = true;
        CurrentInteraction = data;
        onInteractStart.Invoke(data);
    }

    public void OnInteractDrag(InteractionData data)
    {
        CurrentInteraction = data;
        onInteractDrag.Invoke(data);
    }

    public void OnInteractEnd(InteractionData data)
    {
        IsBeingInteracted = false;
        CurrentInteraction = data;
        onInteractEnd.Invoke(data);
    }
}
