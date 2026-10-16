using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Visual representation of a card in the hand.
/// Handles drag-to-play, click-to-play, and smooth hover/unhover animations.
/// </summary>
public class CardUI : MonoBehaviour,
    IPointerClickHandler,
    IPointerEnterHandler,
    IPointerExitHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    [Header("Card Layout References")]
    public Image cardBackground;
    public Image cardBorder;
    public Image artworkPlaceholder;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI typeText;
    public TextMeshProUGUI descriptionText;
    public Image costCircle;

    [HideInInspector]
    public CardData cardData;
    [HideInInspector]
    public int handIndex;

    private CombatManager combatManager;
    private RectTransform rectTransform;
    private Canvas canvas;
    private Vector2 originalPosition;
    private Quaternion originalRotation = Quaternion.identity;
    private Vector3 originalScale = Vector3.one;
    private bool isHovered = false;
    private bool isDragging = false;
    private bool isPlayable = true;
    private CanvasGroup canvasGroup;
    private Coroutine animCoroutine;

    // Palette tokens for Slay the Spire card framing
    private static readonly Color PLAYABLE_BORDER_COLOR = new Color(0.92f, 0.78f, 0.35f, 1f);   // Bright gold
    private static readonly Color UNPLAYABLE_BORDER_COLOR = new Color(0.35f, 0.33f, 0.38f, 0.8f); // Weathered dark iron
    private static readonly Color COST_AFFORDABLE_COLOR = new Color(0.12f, 0.55f, 0.95f, 1f);   // Electric blue gem
    private static readonly Color COST_UNAFFORDABLE_COLOR = new Color(0.70f, 0.20f, 0.20f, 1f);  // Dull red

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
        originalScale = Vector3.one;
        originalRotation = rectTransform.localRotation;
    }

    public void Initialize(CardData data, CombatManager manager, int index)
    {
        cardData = data;
        combatManager = manager;
        handIndex = index;

        if (nameText != null) nameText.text = data.cardName;
        if (costText != null) costText.text = data.energyCost.ToString();
        if (descriptionText != null) descriptionText.text = data.description;

        if (typeText != null)
        {
            if (data.damage > 0)
                typeText.text = "ATTACK";
            else if (data.block > 0)
                typeText.text = "SKILL";
            else if (data.drawCards > 0)
                typeText.text = "SKILL";
            else
                typeText.text = "CARD";
        }

        if (cardBackground != null)
        {
            cardBackground.color = data.cardColor;
        }

        rectTransform.localScale = Vector3.one;
        UpdatePlayableState();
    }

    public void SetOriginalTransform(Vector2 pos, Quaternion rot, bool animate = false)
    {
        originalPosition = pos;
        originalRotation = rot;

        if (!isDragging && !isHovered)
        {
            if (animate && gameObject.activeInHierarchy)
            {
                if (animCoroutine != null) StopCoroutine(animCoroutine);
                animCoroutine = StartCoroutine(SmoothAnimate(pos, rot, Vector3.one, 0.20f));
            }
            else
            {
                rectTransform.anchoredPosition = pos;
                rectTransform.localRotation = rot;
                rectTransform.localScale = Vector3.one;
            }
        }
    }

    public void SetOriginalPosition(Vector2 pos)
    {
        SetOriginalTransform(pos, rectTransform.localRotation);
    }

    public void UpdatePlayableState()
    {
        if (combatManager == null || cardData == null) return;

        isPlayable = combatManager.CanPlayCard(cardData);

        if (cardBorder != null)
        {
            cardBorder.color = isPlayable ? PLAYABLE_BORDER_COLOR : UNPLAYABLE_BORDER_COLOR;
        }

        if (costCircle != null)
        {
            costCircle.color = isPlayable ? COST_AFFORDABLE_COLOR : COST_UNAFFORDABLE_COLOR;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = isPlayable ? 1f : 0.65f;
        }
    }

    // ─── POINTER CLICK (Guides player to drag onto target) ───

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isDragging) return;
        if (!isPlayable || combatManager == null || !combatManager.IsPlayerTurn()) return;

        combatManager.OnCardClicked(cardData);
    }

    // ─── SMOOTH HOVER / UNHOVER TRANSITIONS ───

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isDragging) return;
        isHovered = true;

        if (animCoroutine != null)
        {
            StopCoroutine(animCoroutine);
            animCoroutine = null;
        }

        // Smoothly lift up (+45px), rotate upright, scale slightly to 1.08x
        Vector2 targetPos = originalPosition + new Vector2(0, 48f);
        Vector3 targetScale = Vector3.one * 1.08f;
        Quaternion targetRot = Quaternion.identity;

        transform.SetAsLastSibling();
        animCoroutine = StartCoroutine(SmoothAnimate(targetPos, targetRot, targetScale, 0.12f));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isDragging) return;
        isHovered = false;

        if (animCoroutine != null)
        {
            StopCoroutine(animCoroutine);
            animCoroutine = null;
        }

        // Smoothly return to exact fanned hand position, fanned rotation, and 1.0x scale
        animCoroutine = StartCoroutine(SmoothAnimate(originalPosition, originalRotation, Vector3.one, 0.12f, () =>
        {
            if (!isHovered && !isDragging)
                transform.SetSiblingIndex(handIndex);
        }));
    }

    // ─── DRAG TO TARGET (Attack to Enemy, Defend/Buff to Player) ───

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!combatManager.IsPlayerTurn()) return;

        isDragging = true;
        isHovered = false;

        if (animCoroutine != null)
        {
            StopCoroutine(animCoroutine);
            animCoroutine = null;
        }

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
        }

        rectTransform.localScale = Vector3.one * 1.08f;
        rectTransform.localRotation = Quaternion.identity;
        transform.SetAsLastSibling();

        combatManager.OnCardDragStart(cardData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            rectTransform.position = eventData.position;
        }
        else
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)rectTransform.parent,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPoint);
            rectTransform.localPosition = localPoint;
        }

        combatManager.OnCardDragUpdate(cardData, eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        isDragging = false;

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
        }

        bool played = combatManager.OnCardDragEnd(cardData, handIndex, eventData.position);

        if (!played)
        {
            // Cancelled: smoothly glide back into exact fanned hand slot
            animCoroutine = StartCoroutine(SmoothAnimate(originalPosition, originalRotation, Vector3.one, 0.15f, () =>
            {
                transform.SetSiblingIndex(handIndex);
            }));
        }
    }

    private IEnumerator SmoothAnimate(Vector2 targetPos, Quaternion targetRot, Vector3 targetScale, float duration, System.Action onComplete = null)
    {
        Vector2 startPos = rectTransform.anchoredPosition;
        Quaternion startRot = rectTransform.localRotation;
        Vector3 startScale = rectTransform.localScale;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            rectTransform.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            rectTransform.localRotation = Quaternion.Slerp(startRot, targetRot, t);
            rectTransform.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }

        rectTransform.anchoredPosition = targetPos;
        rectTransform.localRotation = targetRot;
        rectTransform.localScale = targetScale;
        animCoroutine = null;
        onComplete?.Invoke();
    }

    public void ResetHover()
    {
        if (isHovered || isDragging)
        {
            isHovered = false;
            isDragging = false;
            if (canvasGroup != null) canvasGroup.blocksRaycasts = true;
            rectTransform.localScale = Vector3.one;
            rectTransform.anchoredPosition = originalPosition;
            rectTransform.localRotation = originalRotation;
        }
    }
}
