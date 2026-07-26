using UnityEngine;

public class CrosshairManager : MonoBehaviour
{
    [SerializeField] private GameObject regularCrosshair;
    [SerializeField] private GameObject interactableCrosshair;
    [SerializeField] private GameObject errorCrosshair;

    public bool Hidden { get; private set; } = false;
    public enum CrosshairType
    {
        Regular,
        Interactable,
        Error
    }
    public CrosshairType CurrentCrosshairType { get; private set; } = CrosshairType.Regular;

    public void ShowRegularCrosshair()
    {
        regularCrosshair.SetActive(true);
        interactableCrosshair.SetActive(false);
        errorCrosshair.SetActive(false);
        CurrentCrosshairType = CrosshairType.Regular;
    }

    public void ShowInteractableCrosshair()
    {
        regularCrosshair.SetActive(false);
        interactableCrosshair.SetActive(true);
        errorCrosshair.SetActive(false);
        CurrentCrosshairType = CrosshairType.Interactable;
    }

    public void ShowErrorCrosshair()
    {
        regularCrosshair.SetActive(false);
        interactableCrosshair.SetActive(false);
        errorCrosshair.SetActive(true);
        CurrentCrosshairType = CrosshairType.Error;
    }

    public void SetCrosshairHidden(bool hidden)
    {
        Hidden = hidden;
        regularCrosshair.SetActive(!hidden && CurrentCrosshairType == CrosshairType.Regular);
        interactableCrosshair.SetActive(!hidden && CurrentCrosshairType == CrosshairType.Interactable);
        errorCrosshair.SetActive(!hidden && CurrentCrosshairType == CrosshairType.Error);
    }
}
