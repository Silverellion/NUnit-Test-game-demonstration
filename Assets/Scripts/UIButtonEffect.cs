using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Adds polished hover lift (1.05x) and press squish (0.95x) scale animations
/// to UI buttons using UITween.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class UIButtonEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    private RectTransform rectTransform;
    private Button button;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        button = GetComponent<Button>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (button != null && !button.interactable) return;
        UITween.Scale(rectTransform, Vector3.one * 1.05f, 0.15f, EaseType.EaseOutCubic);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        UITween.Scale(rectTransform, Vector3.one, 0.15f, EaseType.EaseOutCubic);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (button != null && !button.interactable) return;
        UITween.Scale(rectTransform, Vector3.one * 0.95f, 0.08f, EaseType.EaseOutQuad);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (button != null && !button.interactable) return;
        UITween.Scale(rectTransform, Vector3.one * 1.05f, 0.12f, EaseType.EaseOutCubic);
    }

    private void OnDisable()
    {
        if (rectTransform != null) rectTransform.localScale = Vector3.one;
    }
}
