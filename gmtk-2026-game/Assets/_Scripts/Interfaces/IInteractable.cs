using UnityEngine;

public interface IInteractable
{
    void OnInteractStart(InteractionData data);
    void OnInteractDrag(InteractionData data);
    void OnInteractEnd(InteractionData data);
}

[System.Serializable]
public struct InteractionData
{
    public Transform interactor;
    public Vector3 hitPoint;
    public Vector3 hitNormal;
    public Ray ray;
}
