using UnityEngine;
using UnityEngine.Events;

public class Interactable : MonoBehaviour
{
    public float interactRange = 5f;
    public UnityEvent onInteract = new UnityEvent();

    public bool TryInteract(Vector3 fromPosition)
    {
        if (Vector3.Distance(transform.position, fromPosition) > interactRange) return false;
        onInteract.Invoke();
        return true;
    }
}
