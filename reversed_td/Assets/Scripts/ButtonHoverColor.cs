using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class ButtonHoverColor : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hoverColor = new Color(1f, 0.5f, 0f); // pomarańczowy

    private TextMeshProUGUI tmpText;
    private Text uiText;

    void Awake()
    {
        tmpText = GetComponentInChildren<TextMeshProUGUI>();
        if (tmpText == null)
            uiText = GetComponentInChildren<Text>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SetColor(hoverColor);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetColor(normalColor);
    }

    private void SetColor(Color color)
    {
        if (tmpText != null)
            tmpText.color = color;
        else if (uiText != null)
            uiText.color = color;
    }
}
