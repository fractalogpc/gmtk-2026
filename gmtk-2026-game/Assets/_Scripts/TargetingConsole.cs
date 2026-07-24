using TMPro;
using UnityEngine;

public class TargetingConsole : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI targetText;
    [SerializeField] private TextMeshProUGUI gunText;
    [SerializeField] private Lever leverAzimuth;
    [SerializeField] private Lever leverElevation;
    [SerializeField] private NixieClock countdownClock;

    private void OnEnable()
    {
        
    }

}
