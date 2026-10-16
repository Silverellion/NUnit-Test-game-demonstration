using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Visual representation of a card in the player's hand.
/// Features rounded anti-aliased 9-slice frames, matched artwork from Assets/Assets/Cards,
/// Cinzel typography, and fluid tween animations for hover, dragging, and fanning.
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
    public Image cardBackground;       // Header banner
    public Image cardBorder;           // Outer rounded gold / iron frame
    public Image artworkPlaceholder;    // Center illustration
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI typeText;
    public TextMeshProUGUI descriptionText;
    public Image costCircle;           // Cost gem circle

    [HideInInspector]
    public CardData cardData;
    [HideInInspector]
    public int handIndex;

    private CombatManager combatManager;
    private RectTransform rectTransform;
    private Canvas canvas;
    private Vector2 originalPosition;
    private Quaternion originalRotation = Quaternion.identity;
    private bool isHovered = false;
    private bool isDragging = false;
    private bool isPlayable = true;
    private CanvasGroup canvasGroup;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
        originalRotation = rectTransform.localRotation;
    }

    public void Initialize(CardData data, CombatManager manager, int index)
    {
        cardData = data;
        combatManager = manager;
        handIndex = index;

        // 1. Text & Fonts (Cinzel)
        if (nameText != null)
        {
            nameText.text = data.cardName;
            UIFonts.ApplyTitle(nameText, autoSize: true, minSize: 11f, maxSize: 15f);
        }
        if (costText != null)
        {
            costText.text = data.energyCost.ToString();
            UIFonts.ApplyTitle(costText, autoSize: false);
        }
        if (descriptionText != null)
        {
            descriptionText.text = data.description;
            UIFonts.ApplyBody(descriptionText, semiBold: false, autoSize: true, minSize: 10f, maxSize: 15f);
        }

        string typeString = "CARD";
        if (data.damage > 0)
            typeString = "ATTACK";
        else if (data.block > 0)
            typeString = "SKILL";
        else if (data.drawCards > 0)
            typeString = "SKILL";

        if (typeText != null)
        {
            typeText.text = typeString;
            UIFonts.ApplyTitle(typeText, autoSize: true, minSize: 9f, maxSize: 12f);
        }

        // 2. Card Artwork (Matched from Assets/Assets/Cards)
        if (artworkPlaceholder != null)
        {
            Sprite art = UISprites.GetCardArtwork(data.cardName);
            if (art != null)
            {
                artworkPlaceholder.sprite = art;
                artworkPlaceholder.color = Color.white;
                artworkPlaceholder.preserveAspect = true;
            }
        }

        // 3. Header Banner Sprite
        if (cardBackground != null)
        {
            cardBackground.sprite = UISprites.GetCardHeader(typeString);
            cardBackground.type = Image.Type.Sliced;
            cardBackground.color = Color.white;
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
                UITween.Cancel(rectTransform);
                UITween.Move(rectTransform, pos, 0.22f, EaseType.EaseOutCubic);
                UITween.Rotate(rectTransform, rot, 0.22f, EaseType.EaseOutCubic);
                UITween.Scale(rectTransform, Vector3.one, 0.22f, EaseType.EaseOutCubic);
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
            cardBorder.sprite = isPlayable ? UISprites.CardBorderPlayable : UISprites.CardBorderUnplayable;
            cardBorder.type = Image.Type.Sliced;
            cardBorder.color = Color.white;
        }

        if (costCircle != null)
        {
            costCircle.sprite = isPlayable ? UISprites.CostBadgePlayable : UISprites.CostBadgeUnplayable;
            costCircle.color = Color.white;
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

    // ─── SMOOTH HOVER / UNHOVER WITH UITWEEN ───

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isDragging) return;
        isHovered = true;

        Vector2 targetPos = originalPosition + new Vector2(0, 54f);
        Vector3 targetScale = Vector3.one * 1.10f;
        Quaternion targetRot = Quaternion.identity;

        transform.SetAsLastSibling();
        UITween.Cancel(rectTransform);
        UITween.Move(rectTransform, targetPos, 0.16f, EaseType.EaseOutCubic);
        UITween.Rotate(rectTransform, targetRot, 0.16f, EaseType.EaseOutCubic);
        UITween.Scale(rectTransform, targetScale, 0.16f, EaseType.EaseOutBack);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isDragging) return;
        isHovered = false;

        UITween.Cancel(rectTransform);
        UITween.Move(rectTransform, originalPosition, 0.16f, EaseType.EaseOutCubic);
        UITween.Rotate(rectTransform, originalRotation, 0.16f, EaseType.EaseOutCubic);
        UITween.Scale(rectTransform, Vector3.one, 0.16f, EaseType.EaseOutCubic, () =>
        {
            if (!isHovered && !isDragging)
                transform.SetSiblingIndex(handIndex);
        });
    }

    // ─── DRAG TO TARGET (Attack to Enemy, Defend/Buff to Player) ───

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!combatManager.IsPlayerTurn()) return;

        isDragging = true;
        isHovered = false;

        UITween.Cancel(rectTransform);

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
        }

        rectTransform.localScale = Vector3.one * 1.10f;
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
            UITween.Cancel(rectTransform);
            UITween.Move(rectTransform, originalPosition, 0.20f, EaseType.EaseOutCubic);
            UITween.Rotate(rectTransform, originalRotation, 0.20f, EaseType.EaseOutCubic);
            UITween.Scale(rectTransform, Vector3.one, 0.20f, EaseType.EaseOutCubic, () =>
            {
                transform.SetSiblingIndex(handIndex);
            });
        }
    }

    public void ResetHover()
    {
        if (isHovered || isDragging)
        {
            isHovered = false;
            isDragging = false;
            if (canvasGroup != null) canvasGroup.blocksRaycasts = true;
            UITween.Cancel(rectTransform);
            rectTransform.localScale = Vector3.one;
            rectTransform.anchoredPosition = originalPosition;
            rectTransform.localRotation = originalRotation;
        }
    }
}
