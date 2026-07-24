using UnityEngine;

public interface IInteractable
{
    /// <summary>
    /// Called on the first frame the player starts interacting with this object.
    /// </summary>
    /// <param name="data"></param>
    void OnInteractStart(InteractionData data);
    /// <summary>
    /// Called every frame while the player is interacting with this object.
    /// </summary>
    /// <param name="data"></param>
    void OnInteractDrag(InteractionData data);
    /// <summary>
    /// Called on the first frame the player stops interacting with this object.
    /// </summary>
    /// <param name="data"></param>
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
