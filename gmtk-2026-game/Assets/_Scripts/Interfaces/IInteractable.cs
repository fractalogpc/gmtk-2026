using UnityEngine;

public interface IInteractable
{
    /// <summary>
    /// Called on the first frame the player starts interacting with this object.
    /// </summary>
    /// <param name="data"></param>
    InteractionSettings OnInteractStart(InteractionData data);
    /// <summary>
    /// Called every frame while the player is interacting with this object.
    /// </summary>
    /// <param name="data"></param>
    InteractionSettings DuringInteract(InteractionData data);
    /// <summary>
    /// Called on the first frame the player stops interacting with this object.
    /// </summary>
    /// <param name="data"></param>
    InteractionSettings OnInteractEnd(InteractionData data);

    /// <summary>
    /// Enable or disable this interactable's script (not the GameObject).
    /// Default implementation toggles MonoBehaviour.enabled.
    /// </summary>
    void SetInteractionEnabled(bool enabled)
    {
        if (this is MonoBehaviour mb) mb.enabled = enabled;
    }
}

[System.Serializable]
public struct InteractionSettings
{
    public bool lockCameraAndMovement;
    public InteractionSettings(bool lockCameraAndMovement)
    {
        this.lockCameraAndMovement = lockCameraAndMovement;
    }
}

[System.Serializable]
public struct InteractionData
{
    public Transform interactor;
    public Vector2 mouseDelta;
    public Ray ray;
}
