using TMPro;
using UnityEngine;

public class TargetValuesUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI targetText;
    [SerializeField] private float azimuthCurrent;
    [SerializeField] private float elevationCurrent;

    public void UpdateTargetValues(float azimuth, float elevation)
    {
        azimuthCurrent = azimuth;
        elevationCurrent = elevation;
        targetText.text = $"Azimuth: {azimuthCurrent:F2}°\nElevation: {elevationCurrent:F2}°";
    }
}
