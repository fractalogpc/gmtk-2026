using UnityEngine;

public class CrosshairManager : MonoBehaviour
{
    [SerializeField] private GameObject regularCrosshair;
    [SerializeField] private GameObject staticCameraCrosshair;
    [SerializeField] private GameObject interactableCrosshair;
    [SerializeField] private GameObject errorCrosshair;

    public bool Hidden { get; private set; } = false;
    public enum CrosshairType
    {
        Regular,
        StaticCamera,
        Interactable,
        Error
    }
    public CrosshairType CurrentCrosshairType { get; private set; } = CrosshairType.Regular;

    public void ShowRegularCrosshair()
    {
        if (Hidden) return;
        regularCrosshair.SetActive(true);
        interactableCrosshair.SetActive(false);
        errorCrosshair.SetActive(false);
        staticCameraCrosshair.SetActive(false);
        CurrentCrosshairType = CrosshairType.Regular;
    }

    public void ShowStaticCameraCrosshair()
    {
        if (Hidden) return;
        regularCrosshair.SetActive(false);
        interactableCrosshair.SetActive(false);
        errorCrosshair.SetActive(false);
        staticCameraCrosshair.SetActive(true);
        CurrentCrosshairType = CrosshairType.StaticCamera;
    }

    public void ShowInteractableCrosshair()
    {
        if (Hidden) return;
        regularCrosshair.SetActive(false);
        interactableCrosshair.SetActive(true);
        errorCrosshair.SetActive(false);
        staticCameraCrosshair.SetActive(false);
        CurrentCrosshairType = CrosshairType.Interactable;
    }

    public void ShowErrorCrosshair()
    {
        if (Hidden) return;
        regularCrosshair.SetActive(false);
        interactableCrosshair.SetActive(false);
        errorCrosshair.SetActive(true);
        staticCameraCrosshair.SetActive(false);
        CurrentCrosshairType = CrosshairType.Error;
    }

    public void SetCrosshairHidden(bool hidden)
    {
        Hidden = hidden;
        regularCrosshair.SetActive(!hidden && CurrentCrosshairType == CrosshairType.Regular);
        interactableCrosshair.SetActive(!hidden && CurrentCrosshairType == CrosshairType.Interactable);
        errorCrosshair.SetActive(!hidden && CurrentCrosshairType == CrosshairType.Error);
        staticCameraCrosshair.SetActive(!hidden && CurrentCrosshairType == CrosshairType.StaticCamera);
    }
}
