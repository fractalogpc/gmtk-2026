using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class ButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{

    private bool isHovering = false;

    [SerializeField] private Color hoverColor;
    [SerializeField] private Color normalColor;
    [SerializeField] private float animationSpeed;

    private TextMeshProUGUI textMeshPro;

    private void Start()
    {
        textMeshPro = GetComponentsInChildren<TextMeshProUGUI>()[0];
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
    }

    private void Update()
    {
        if (isHovering)
        {
            textMeshPro.color = Color.Lerp(textMeshPro.color, hoverColor, Time.deltaTime * animationSpeed);
        }
        else
        {
            textMeshPro.color = Color.Lerp(textMeshPro.color, normalColor, Time.deltaTime * animationSpeed);
        }
    }
}
