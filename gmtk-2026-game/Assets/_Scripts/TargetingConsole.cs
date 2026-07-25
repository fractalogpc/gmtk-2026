using TMPro;
using UnityEngine;

public class TargetingConsole : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI targetText;
    [SerializeField] private TextMeshProUGUI gunText;
    [SerializeField] private DragLever leverAzimuth;
    [SerializeField] private TextMeshProUGUI azimuthChangeText;
    [SerializeField] private DragLever leverElevation;
    [SerializeField] private TextMeshProUGUI elevationChangeText;
    [SerializeField] private float azimuthDPSMin, azimuthDPSMax;
    [SerializeField] private float elevationDPSMin, elevationDPSMax;
    public float GunAzimuth => gunAzimuth;
    public float GunElevation => gunElevation;
    private float gunAzimuth;
    private float gunElevation;

    public void SetLocked(bool isLocked)
    {
        ((IInteractable)leverAzimuth).SetInteractionEnabled(!isLocked);
        ((IInteractable)leverElevation).SetInteractionEnabled(!isLocked);
        if (isLocked)
        {
            gunText.text = "CONTROLS LOCKED\nFIRE COMMAND SENT";
        }
        else
        {
            UpdateGunText();
        }
    }
    public void SetTargetValues(float azimuth, float elevation)
    {
        targetText.text = $"FIRING ORDERS\n________________\n{azimuth:F2} AZIM\n{elevation:F2} ELEV";
    }

    private float MapNormalizedToRange(float normalizedValue, float min, float max)
    {
        return Mathf.Lerp(min, max, normalizedValue);
    }

    private void UpdateGunText()
    {
        gunText.text = $"GUN ORIENTATION\n_______________\n{gunAzimuth:F2} AZIM\n{gunElevation:F2} ELEV";
    }

    private void UpdateGunFromLevers(float deltaTime)
    {
        gunAzimuth += deltaTime * MapNormalizedToRange(leverAzimuth.NormalizedValue, azimuthDPSMin, azimuthDPSMax) ; 
        if (gunAzimuth > 180f) gunAzimuth = -180f;
        if (gunAzimuth < -180f) gunAzimuth = 180f;
        gunElevation += deltaTime * MapNormalizedToRange(leverElevation.NormalizedValue, elevationDPSMin, elevationDPSMax);
        if (gunElevation > 90f) gunElevation = 90f;
        if (gunElevation < 0f) gunElevation = 0f;
    }

    private void Update()
    {
        UpdateGunFromLevers(Time.deltaTime);
        azimuthChangeText.text = $"{MapNormalizedToRange(leverAzimuth.NormalizedValue, azimuthDPSMin, azimuthDPSMax):F2}";
        elevationChangeText.text = $"{MapNormalizedToRange(leverElevation.NormalizedValue, elevationDPSMin, elevationDPSMax):F2}";
        UpdateGunText();
    }

}
