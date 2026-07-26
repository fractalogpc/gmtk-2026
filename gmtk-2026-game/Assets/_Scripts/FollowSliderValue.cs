using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class FollowSliderValue : MonoBehaviour
{
    private TextMeshProUGUI textComponent;
    [SerializeField] private Slider slider;
    [SerializeField] private Toggle toggle;

    private void Awake()
    {
        textComponent = GetComponent<TextMeshProUGUI>();
    }

    void Update()
    {
        if (textComponent != null && slider != null)
        {
            textComponent.text = slider.value.ToString("0.00");
        }

        if (textComponent != null && toggle != null)
        {
            textComponent.text = toggle.isOn ? "On" : "Off";
        }
    }
}
