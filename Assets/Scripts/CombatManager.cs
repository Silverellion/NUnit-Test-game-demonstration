using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Core combat manager: handles turns, deck management, card playing, enemy AI, win/loss,
/// smooth card dealing animations, animated "YOUR TURN" pop-out banner, and combat arena animations.
/// </summary>
public class CombatManager : MonoBehaviour
{
    [Header("Player Stats")]
    public int playerMaxHP = 60;
    public int playerMaxEnergy = 3;

    [Header("Enemy Stats")]
    public int enemyMaxHP = 50;

    [Header("UI References")]
    public PlayerUI playerUI;
    public EnemyUI enemyUI;
    public Transform handContainer;
    public GameObject cardPrefab;
    public Button endTurnButton;
    public TextMeshProUGUI endTurnButtonText;
    public TextMeshProUGUI turnIndicatorText;
    public RectTransform turnBanner;

    [Header("Game Over UI")]
    public GameObject victoryPanel;
    public GameObject defeatPanel;
    public Button victoryRestartButton;
    public Button defeatRestartButton;

    [Header("Feedback")]
    public GameObject damagePopupPrefab;
    public Canvas mainCanvas;

    [Header("Character Visuals (Animations)")]
    public CharacterCombatVisual playerVisual;
    public CharacterCombatVisual enemyVisual;

    [Header("Intro Animation References")]
    public RectTransform backgroundRT;
    public CanvasGroup groundFadeCanvasGroup;
    public CanvasGroup playerStatusCanvasGroup;
    public CanvasGroup enemyStatusCanvasGroup;

    // Private state
    private int playerHP;
    private int playerBlock;
    private int playerEnergy;
    private int enemyHP;
    private int enemyBlock;

    private List<CardData> drawPile = new List<CardData>();
    private List<CardData> hand = new List<CardData>();
    private List<CardData> discardPile = new List<CardData>();

    private List<GameObject> handCardObjects = new List<GameObject>();

    private bool isPlayerTurn = false;
    private bool isActionInProgress = false;
    private bool combatEnded = false;
    private bool isDrawingCards = false;

    // Enemy AI state
    private string enemyNextAction;
    private int enemyNextValue;
    private int enemyTurnCount = 0;

    private struct EnemyAction
    {
        public string type;
        public int value;
        public EnemyAction(string t, int v) { type = t; value = v; }
    }

    private EnemyAction[] enemyPattern = new EnemyAction[]
    {
        new EnemyAction("Attack", 8),
        new EnemyAction("Attack", 12),
        new EnemyAction("Defend", 6),
        new EnemyAction("Attack", 10),
        new EnemyAction("Attack", 6),
    };

    void Start()
    {
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (defeatPanel != null) defeatPanel.SetActive(false);

        if (endTurnButton != null)
            endTurnButton.onClick.AddListener(OnEndTurnClicked);
        if (victoryRestartButton != null)
            victoryRestartButton.onClick.AddListener(RestartCombat);
        if (defeatRestartButton != null)
            defeatRestartButton.onClick.AddListener(RestartCombat);

        StartCoroutine(SceneIntroRoutine());
    }

    private IEnumerator SceneIntroRoutine()
    {
        // 1. Initial State: Background is fully visible at bottom, characters start outside visible screen
        if (backgroundRT != null)
        {
            backgroundRT.anchoredPosition = Vector2.zero;
            backgroundRT.localScale = Vector3.one;
        }
        if (groundFadeCanvasGroup != null)
        {
            groundFadeCanvasGroup.alpha = 1f;
        }
        if (playerStatusCanvasGroup != null) playerStatusCanvasGroup.alpha = 0f;
        if (enemyStatusCanvasGroup != null) enemyStatusCanvasGroup.alpha = 0f;

        Vector2 playerHome = playerVisual != null ? playerVisual.GetHomePosition() : new Vector2(-380f, -50f);
        Vector2 enemyHome = enemyVisual != null ? enemyVisual.GetHomePosition() : new Vector2(380f, -50f);

        Vector2 playerStart = new Vector2(-1350f, playerHome.y);
        Vector2 enemyStart = new Vector2(1350f, enemyHome.y);

        if (playerVisual != null && playerVisual.rectTransform != null)
            playerVisual.rectTransform.anchoredPosition = playerStart;
        if (enemyVisual != null && enemyVisual.rectTransform != null)
            enemyVisual.rectTransform.anchoredPosition = enemyStart;

        // Brief pause to showcase the loaded town scene
        yield return new WaitForSeconds(0.40f);

        // 2. Animate background going from bottom to up until 30% of the bottom part of the scene is empty
        float canvasHeight = 1080f;
        if (mainCanvas != null)
        {
            RectTransform canvasRT = mainCanvas.GetComponent<RectTransform>();
            if (canvasRT != null && canvasRT.rect.height > 0)
                canvasHeight = canvasRT.rect.height;
        }
        float targetY = canvasHeight * 0.30f; // Exactly 30% of the scene height

        Vector2 bgStartPos = Vector2.zero;
        Vector2 bgEndPos = new Vector2(0f, targetY);

        float bgDuration = 1.35f;
        float elapsed = 0f;
        while (elapsed < bgDuration)
        {
            elapsed += Time.deltaTime;
            float t = UITween.Evaluate(Mathf.Clamp01(elapsed / bgDuration), EaseType.EaseOutCubic);
            if (backgroundRT != null)
            {
                backgroundRT.anchoredPosition = Vector2.Lerp(bgStartPos, bgEndPos, t);
            }
            yield return null;
        }

        if (backgroundRT != null)
        {
            backgroundRT.anchoredPosition = bgEndPos;
        }

        yield return new WaitForSeconds(0.15f);

        // 3. Character Entrance Animation (Requirement 7)
        // Player moves in from outside left, Enemy moves in from outside right
        float entranceDuration = 1.25f;
        bool playerArrived = false;
        bool enemyArrived = false;

        if (playerVisual != null)
        {
            playerVisual.WalkInFromOutside(playerStart, entranceDuration, () => playerArrived = true);
        }
        else playerArrived = true;

        if (enemyVisual != null)
        {
            enemyVisual.WalkInFromOutside(enemyStart, entranceDuration, () => enemyArrived = true);
        }
        else enemyArrived = true;

        while (!playerArrived || !enemyArrived)
            yield return null;

        // 4. Fade in character status HUDs (Name plates, HP bars, Enemy intent)
        float hudDuration = 0.35f;
        elapsed = 0f;
        while (elapsed < hudDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / hudDuration);
            if (playerStatusCanvasGroup != null) playerStatusCanvasGroup.alpha = t;
            if (enemyStatusCanvasGroup != null) enemyStatusCanvasGroup.alpha = t;
            yield return null;
        }
        if (playerStatusCanvasGroup != null) playerStatusCanvasGroup.alpha = 1f;
        if (enemyStatusCanvasGroup != null) enemyStatusCanvasGroup.alpha = 1f;

        yield return new WaitForSeconds(0.25f);

        // 5. Begin combat
        StartCombat();
    }

    public void StartCombat()
    {
        combatEnded = false;
        isActionInProgress = false;
        isDrawingCards = false;
        enemyTurnCount = 0;

        if (playerStatusCanvasGroup != null) playerStatusCanvasGroup.alpha = 1f;
        if (enemyStatusCanvasGroup != null) enemyStatusCanvasGroup.alpha = 1f;

        playerHP = playerMaxHP;
        playerBlock = 0;
        playerEnergy = playerMaxEnergy;
        enemyHP = enemyMaxHP;
        enemyBlock = 0;

        if (playerUI != null)
        {
            playerUI.Initialize("Soldier", playerMaxHP);
            playerUI.UpdateEnergy(playerEnergy, playerMaxEnergy);
        }
        if (enemyUI != null)
        {
            enemyUI.Initialize("Orc", enemyMaxHP);
        }

        if (playerVisual != null) playerVisual.StartIdle();
        if (enemyVisual != null) enemyVisual.StartIdle();

        BuildDeck();
        ShuffleDeck(drawPile);

        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (defeatPanel != null) defeatPanel.SetActive(false);

        PlanEnemyAction();
        StartPlayerTurn();
    }

    private void BuildDeck()
    {
        drawPile.Clear();
        hand.Clear();
        discardPile.Clear();

        Color attackColor = new Color(0.55f, 0.15f, 0.15f, 1f); // Dark red
        Color defendColor = new Color(0.15f, 0.35f, 0.55f, 1f); // Dark blue
        Color heavyColor = new Color(0.6f, 0.2f, 0.05f, 1f);    // Dark orange
        Color utilityColor = new Color(0.15f, 0.45f, 0.2f, 1f);  // Dark green

        // 5x Strike
        for (int i = 0; i < 5; i++)
            drawPile.Add(new CardData("Strike", 1, 6, 0, 0, "Deal <color=#ff6b6b><b>6</b></color> damage.", attackColor));

        // 3x Defend
        for (int i = 0; i < 3; i++)
            drawPile.Add(new CardData("Defend", 1, 0, 5, 0, "Gain <color=#4dabf7><b>5</b></color> Block.", defendColor));

        // 1x Heavy Strike
        drawPile.Add(new CardData("Heavy Strike", 2, 12, 0, 0, "Deal <color=#ff6b6b><b>12</b></color> damage.", heavyColor));

        // 1x Quick Study
        drawPile.Add(new CardData("Strategize", 1, 0, 0, 2, "Draw <color=#51cf66><b>2</b></color> cards.", utilityColor));
    }

    private void ShuffleDeck(List<CardData> deck)
    {
        for (int i = deck.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            var temp = deck[i];
            deck[i] = deck[j];
            deck[j] = temp;
        }
    }

    // ─── TURN MANAGEMENT ───

    private void StartPlayerTurn()
    {
        if (combatEnded) return;

        isPlayerTurn = true;
        isActionInProgress = false;

        playerBlock = 0;
        if (playerUI != null) playerUI.UpdateHP(playerHP, playerBlock);

        playerEnergy = playerMaxEnergy;
        if (playerUI != null) playerUI.UpdateEnergy(playerEnergy, playerMaxEnergy);

        if (endTurnButton != null) endTurnButton.interactable = true;
        if (turnIndicatorText != null) turnIndicatorText.text = "YOUR TURN";

        // 1. Pop out "YOUR TURN" banner sweeping from right to left!
        StartCoroutine(AnimateYourTurnBanner());

        // 2. Deal 5 cards with smooth dealing transition from draw pile!
        StartCoroutine(DrawCardsSmoothRoutine(5));
    }

    private IEnumerator AnimateYourTurnBanner()
    {
        if (turnBanner == null) yield break;

        turnBanner.gameObject.SetActive(true);
        CanvasGroup cg = turnBanner.GetComponent<CanvasGroup>();
        if (cg == null) cg = turnBanner.gameObject.AddComponent<CanvasGroup>();

        float startX = 600f;
        float centerX = 0f;
        float exitX = -600f;

        turnBanner.anchoredPosition = new Vector2(startX, turnBanner.anchoredPosition.y);
        turnBanner.localScale = Vector3.one * 0.95f;
        cg.alpha = 0f;

        // Phase 1: Ease-out slide in + fade in
        float durationIn = 0.30f;
        float elapsed = 0f;
        while (elapsed < durationIn)
        {
            elapsed += Time.deltaTime;
            float t = UITween.Evaluate(Mathf.Clamp01(elapsed / durationIn), EaseType.EaseOutCubic);
            turnBanner.anchoredPosition = new Vector2(Mathf.Lerp(startX, centerX, t), turnBanner.anchoredPosition.y);
            turnBanner.localScale = Vector3.Lerp(Vector3.one * 0.95f, Vector3.one, t);
            cg.alpha = t;
            yield return null;
        }

        turnBanner.anchoredPosition = new Vector2(centerX, turnBanner.anchoredPosition.y);
        turnBanner.localScale = Vector3.one;
        cg.alpha = 1f;

        // Phase 2: Hold
        yield return new WaitForSeconds(0.40f);

        // Phase 3: Ease-in slide out + fade out
        float durationOut = 0.24f;
        elapsed = 0f;
        while (elapsed < durationOut)
        {
            elapsed += Time.deltaTime;
            float t = UITween.Evaluate(Mathf.Clamp01(elapsed / durationOut), EaseType.EaseInQuad);
            turnBanner.anchoredPosition = new Vector2(Mathf.Lerp(centerX, exitX, t), turnBanner.anchoredPosition.y);
            turnBanner.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.95f, t);
            cg.alpha = 1f - t;
            yield return null;
        }

        turnBanner.anchoredPosition = new Vector2(exitX, turnBanner.anchoredPosition.y);
        turnBanner.gameObject.SetActive(false);
    }

    private void OnEndTurnClicked()
    {
        if (!isPlayerTurn || combatEnded || isActionInProgress || isDrawingCards) return;
        StartCoroutine(EndTurnSequence());
    }

    private IEnumerator EndTurnSequence()
    {
        isPlayerTurn = false;
        if (endTurnButton != null) endTurnButton.interactable = false;
        if (turnIndicatorText != null) turnIndicatorText.text = "ENEMY TURN";

        DiscardHand();

        yield return new WaitForSeconds(0.35f);

        yield return StartCoroutine(ExecuteEnemyTurn());

        if (!combatEnded)
        {
            PlanEnemyAction();
            StartPlayerTurn();
        }
    }

    // ─── SMOOTH CARD DRAWING TRANSITIONS ───

    public IEnumerator DrawCardsSmoothRoutine(int count)
    {
        isDrawingCards = true;

        for (int i = 0; i < count; i++)
        {
            if (drawPile.Count == 0)
            {
                if (discardPile.Count == 0) break;
                drawPile.AddRange(discardPile);
                discardPile.Clear();
                ShuffleDeck(drawPile);
            }

            CardData card = drawPile[0];
            drawPile.RemoveAt(0);
            hand.Add(card);

            // Spawn new card at Draw Pile position and animate smoothly into hand
            yield return StartCoroutine(DealSingleCardRoutine(card, hand.Count - 1));

            UpdatePileCounters();
            yield return new WaitForSeconds(0.06f); // Pleasant staggered dealing rhythm
        }

        isDrawingCards = false;
        UpdateAllCardStates();
        UpdatePileCounters();
    }

    private IEnumerator DealSingleCardRoutine(CardData card, int cardIndex)
    {
        if (cardPrefab == null || handContainer == null) yield break;

        // 1. Calculate the target fan layout for all cards currently in hand
        CalculateFanSlot(hand.Count, cardIndex, out Vector2 targetPos, out Quaternion targetRot);

        // 2. Also smoothly reposition any pre-existing cards into their updated slots
        for (int j = 0; j < handCardObjects.Count; j++)
        {
            if (handCardObjects[j] != null)
            {
                CalculateFanSlot(hand.Count, j, out Vector2 slotPos, out Quaternion slotRot);
                CardUI existingUI = handCardObjects[j].GetComponent<CardUI>();
                if (existingUI != null)
                {
                    existingUI.SetOriginalTransform(slotPos, slotRot, animate: true);
                }
            }
        }

        // 3. Create the new card starting at the Draw Pile position (bottom-left)
        GameObject cardObj = Instantiate(cardPrefab, handContainer);
        cardObj.name = $"Card_{cardIndex}_{card.cardName}";
        cardObj.SetActive(true);

        RectTransform rt = cardObj.GetComponent<RectTransform>();
        CardUI cardUI = cardObj.GetComponent<CardUI>();
        if (cardUI != null)
        {
            cardUI.Initialize(card, this, cardIndex);
        }

        handCardObjects.Add(cardObj);

        // Start at draw pile (bottom-left, small scale, tilted)
        Vector3 spawnPos = new Vector3(-540f, -40f, 0f);
        Quaternion spawnRot = Quaternion.Euler(0, 0, 25f);
        Vector3 spawnScale = Vector3.one * 0.25f;

        rt.anchoredPosition = spawnPos;
        rt.localRotation = spawnRot;
        rt.localScale = spawnScale;

        // 4. Smoothly glide card into its target fan slot
        float duration = 0.24f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = 1f - Mathf.Pow(1f - Mathf.Clamp01(elapsed / duration), 3); // Smooth ease-out
            rt.anchoredPosition = Vector2.Lerp(spawnPos, targetPos, t);
            rt.localRotation = Quaternion.Slerp(spawnRot, targetRot, t);
            rt.localScale = Vector3.Lerp(spawnScale, Vector3.one, t);
            yield return null;
        }

        rt.anchoredPosition = targetPos;
        rt.localRotation = targetRot;
        rt.localScale = Vector3.one;

        if (cardUI != null)
        {
            cardUI.SetOriginalTransform(targetPos, targetRot, animate: false);
        }
    }

    private void CalculateFanSlot(int totalCards, int index, out Vector2 pos, out Quaternion rot)
    {
        if (totalCards <= 0)
        {
            pos = Vector2.zero;
            rot = Quaternion.identity;
            return;
        }

        // Spacing calibrated for 244px wide cards in 1920x1080 screen
        float maxHandWidth = 1050f;
        float defaultSpacing = 220f;
        float cardSpacing = defaultSpacing;

        if (totalCards > 1 && (totalCards - 1) * cardSpacing > maxHandWidth)
        {
            cardSpacing = maxHandWidth / (totalCards - 1);
        }

        float angleStep;
        if (totalCards <= 1) angleStep = 0f;
        else if (totalCards == 2) angleStep = 5f;
        else if (totalCards == 3) angleStep = 4.2f;
        else if (totalCards <= 5) angleStep = 3.2f;
        else angleStep = 16f / (totalCards - 1);

        float centerIndex = (totalCards - 1) / 2f;
        float offset = index - centerIndex;
        float arcRadius = 1400f;

        float posX = offset * cardSpacing;
        float rotZ = -offset * angleStep;
        rot = Quaternion.Euler(0f, 0f, rotZ);

        float angleRad = rotZ * Mathf.Deg2Rad;
        float posY = -(1f - Mathf.Cos(angleRad)) * arcRadius;

        pos = new Vector2(posX, posY);
    }

    private void DiscardHand()
    {
        discardPile.AddRange(hand);
        hand.Clear();
        ClearHandDisplay();
        UpdatePileCounters();
    }

    // ─── CARD PLAYING ───

    public bool CanPlayCard(CardData card)
    {
        return isPlayerTurn && !combatEnded && !isActionInProgress && !isDrawingCards && playerEnergy >= card.energyCost;
    }

    public bool IsPlayerTurn()
    {
        return isPlayerTurn && !combatEnded && !isActionInProgress && !isDrawingCards;
    }

    public void PlayCard(int handIndex)
    {
        if (handIndex < 0 || handIndex >= hand.Count) return;

        CardData card = hand[handIndex];
        if (!CanPlayCard(card)) return;

        // Spend energy
        playerEnergy -= card.energyCost;
        if (playerUI != null) playerUI.UpdateEnergy(playerEnergy, playerMaxEnergy);

        // Remove card from hand
        hand.RemoveAt(handIndex);
        discardPile.Add(card);

        // Smoothly re-fan remaining cards in hand
        RepositionRemainingCards();
        UpdatePileCounters();

        if (card.damage > 0)
        {
            isActionInProgress = true;
            if (endTurnButton != null) endTurnButton.interactable = false;

            if (playerVisual != null && enemyVisual != null)
            {
                Vector2 targetPos = enemyVisual.GetHomePosition() - new Vector2(130f, 0);
                bool isHeavy = card.damage >= 12;

                playerVisual.WalkAndAttack(targetPos, isHeavy,
                    onHit: () =>
                    {
                        DealDamageToEnemy(card.damage);
                        enemyVisual.PlayHurt();
                        StartCoroutine(ShakeObject(enemyVisual.rectTransform));
                    },
                    onComplete: () =>
                    {
                        isActionInProgress = false;
                        if (endTurnButton != null) endTurnButton.interactable = isPlayerTurn && !combatEnded;
                        UpdateAllCardStates();

                        if (enemyHP <= 0)
                        {
                            enemyVisual.PlayDeath();
                            StartCoroutine(VictorySequence());
                        }
                    });
            }
            else
            {
                DealDamageToEnemy(card.damage);
                isActionInProgress = false;
                if (endTurnButton != null) endTurnButton.interactable = isPlayerTurn && !combatEnded;
                UpdateAllCardStates();
                if (enemyHP <= 0)
                    StartCoroutine(VictorySequence());
            }
        }
        else
        {
            if (card.block > 0)
            {
                playerBlock += card.block;
                if (playerUI != null)
                {
                    playerUI.UpdateHP(playerHP, playerBlock);
                    SpawnPopup(playerUI.transform, $"+{card.block} Block", new Color(0.3f, 0.6f, 0.95f));
                    StartCoroutine(ShakeObject(playerUI.GetComponent<RectTransform>()));
                }
            }

            int extraDraw = card.drawCards;
            if (extraDraw > 0)
            {
                StartCoroutine(DrawCardsSmoothRoutine(extraDraw));
                if (playerUI != null)
                    SpawnPopup(playerUI.transform, $"+{extraDraw} Cards", new Color(0.3f, 0.85f, 0.4f));
            }

            UpdateAllCardStates();
        }
    }

    private void RepositionRemainingCards()
    {
        ClearHandDisplay();

        if (cardPrefab == null || handContainer == null) return;

        for (int i = 0; i < hand.Count; i++)
        {
            CalculateFanSlot(hand.Count, i, out Vector2 pos, out Quaternion rot);

            GameObject cardObj = Instantiate(cardPrefab, handContainer);
            cardObj.name = $"Card_{i}_{hand[i].cardName}";
            cardObj.SetActive(true);

            RectTransform rt = cardObj.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.localRotation = rot;

            CardUI cardUI = cardObj.GetComponent<CardUI>();
            if (cardUI != null)
            {
                cardUI.Initialize(hand[i], this, i);
                cardUI.SetOriginalTransform(pos, rot, animate: true);
            }

            handCardObjects.Add(cardObj);
        }
    }

    // ─── TARGETING LOGIC (Drag Attack to Enemy, Defend/Buff to Player) ───

    public bool IsPointerOverEnemy(Vector2 screenPoint)
    {
        if (enemyVisual == null || enemyVisual.rectTransform == null) return false;

        RectTransform rt = enemyVisual.rectTransform;
        Camera cam = (mainCanvas != null && mainCanvas.renderMode != RenderMode.ScreenSpaceOverlay) ? mainCanvas.worldCamera : null;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, screenPoint, cam, out Vector2 localPoint))
        {
            Rect r = rt.rect;
            // Generous targeting expansion (90px all sides) for effortless drag-targeting
            r.xMin -= 90f;
            r.xMax += 90f;
            r.yMin -= 90f;
            r.yMax += 90f;
            if (r.Contains(localPoint)) return true;
        }

        Vector2 enemyScreenPos = RectTransformUtility.WorldToScreenPoint(cam, rt.position);
        if (Vector2.Distance(screenPoint, enemyScreenPos) < 220f)
            return true;

        return false;
    }

    public bool IsPointerOverPlayer(Vector2 screenPoint)
    {
        if (playerVisual == null || playerVisual.rectTransform == null) return false;

        RectTransform rt = playerVisual.rectTransform;
        Camera cam = (mainCanvas != null && mainCanvas.renderMode != RenderMode.ScreenSpaceOverlay) ? mainCanvas.worldCamera : null;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, screenPoint, cam, out Vector2 localPoint))
        {
            Rect r = rt.rect;
            // Generous targeting expansion (90px all sides) for effortless drag-targeting
            r.xMin -= 90f;
            r.xMax += 90f;
            r.yMin -= 90f;
            r.yMax += 90f;
            if (r.Contains(localPoint)) return true;
        }

        Vector2 playerScreenPos = RectTransformUtility.WorldToScreenPoint(cam, rt.position);
        if (Vector2.Distance(screenPoint, playerScreenPos) < 220f)
            return true;

        return false;
    }

    private CardData hoveredCard = null;

    public void OnCardHoverStart(CardData card)
    {
        if (!isPlayerTurn || combatEnded || isActionInProgress) return;
        hoveredCard = card;

        if (card.targetType == CardTargetType.Enemy)
        {
            if (enemyVisual != null)
                enemyVisual.SetTargetHighlighted(true, false, new Color(1f, 0.22f, 0.22f));
        }
        else
        {
            if (playerVisual != null)
                playerVisual.SetTargetHighlighted(true, false, new Color(0.22f, 0.65f, 1f));
        }
    }

    public void OnCardHoverEnd(CardData card)
    {
        if (hoveredCard == card)
        {
            hoveredCard = null;
        }

        if (enemyVisual != null) enemyVisual.SetTargetHighlighted(false, false, Color.white);
        if (playerVisual != null) playerVisual.SetTargetHighlighted(false, false, Color.white);
    }

    public void OnCardDragStart(CardData card)
    {
        hoveredCard = null;
        if (card.targetType == CardTargetType.Enemy)
        {
            if (enemyVisual != null)
                enemyVisual.SetTargetHighlighted(true, false, new Color(1f, 0.22f, 0.22f));
        }
        else
        {
            if (playerVisual != null)
                playerVisual.SetTargetHighlighted(true, false, new Color(0.22f, 0.65f, 1f));
        }
    }

    public void OnCardDragUpdate(CardData card, Vector2 screenPoint)
    {
        if (card.targetType == CardTargetType.Enemy)
        {
            bool isOver = IsPointerOverEnemy(screenPoint);
            if (enemyVisual != null)
                enemyVisual.SetTargetHighlighted(true, isOver, new Color(1f, 0.22f, 0.22f));
        }
        else
        {
            bool isOver = IsPointerOverPlayer(screenPoint);
            if (playerVisual != null)
                playerVisual.SetTargetHighlighted(true, isOver, new Color(0.22f, 0.65f, 1f));
        }
    }

    public bool OnCardDragEnd(CardData card, int handIndex, Vector2 screenPoint)
    {
        hoveredCard = null;
        if (enemyVisual != null) enemyVisual.SetTargetHighlighted(false, false, Color.white);
        if (playerVisual != null) playerVisual.SetTargetHighlighted(false, false, Color.white);

        bool isValidTarget = false;
        if (card.targetType == CardTargetType.Enemy)
        {
            isValidTarget = IsPointerOverEnemy(screenPoint);
        }
        else
        {
            isValidTarget = IsPointerOverPlayer(screenPoint);
        }

        if (isValidTarget && CanPlayCard(card))
        {
            PlayCard(handIndex);
            return true;
        }

        return false;
    }

    public void OnCardClicked(CardData card)
    {
        if (card.targetType == CardTargetType.Enemy)
        {
            StartCoroutine(ShowTemporaryHint("Drag Attack cards onto the Orc!"));
        }
        else
        {
            StartCoroutine(ShowTemporaryHint("Drag Defend/Buff cards onto the Soldier!"));
        }
    }

    private IEnumerator ShowTemporaryHint(string msg)
    {
        if (turnIndicatorText == null) yield break;
        string orig = turnIndicatorText.text;
        turnIndicatorText.text = $"<color=#ffd166>{msg}</color>";
        yield return new WaitForSeconds(1.3f);
        if (isPlayerTurn && !combatEnded)
            turnIndicatorText.text = orig;
    }

    private void DealDamageToEnemy(int damage)
    {
        int remainingDamage = damage;

        if (enemyBlock > 0)
        {
            if (enemyBlock >= remainingDamage)
            {
                enemyBlock -= remainingDamage;
                remainingDamage = 0;
            }
            else
            {
                remainingDamage -= enemyBlock;
                enemyBlock = 0;
            }
        }

        if (remainingDamage > 0)
        {
            enemyHP = Mathf.Max(0, enemyHP - remainingDamage);
        }

        if (enemyUI != null)
        {
            enemyUI.UpdateHP(enemyHP, enemyBlock);
            SpawnPopup(enemyUI.transform, $"-{damage}", new Color(0.95f, 0.25f, 0.25f));
        }
    }

    private void DealDamageToPlayer(int damage)
    {
        int remainingDamage = damage;

        if (playerBlock > 0)
        {
            if (playerBlock >= remainingDamage)
            {
                playerBlock -= remainingDamage;
                remainingDamage = 0;
            }
            else
            {
                remainingDamage -= playerBlock;
                playerBlock = 0;
            }
        }

        if (remainingDamage > 0)
        {
            playerHP = Mathf.Max(0, playerHP - remainingDamage);
        }

        if (playerUI != null)
        {
            playerUI.UpdateHP(playerHP, playerBlock);
            SpawnPopup(playerUI.transform, $"-{damage}", new Color(0.95f, 0.25f, 0.25f));
        }
    }

    // ─── ENEMY AI ───

    private void PlanEnemyAction()
    {
        EnemyAction action = enemyPattern[enemyTurnCount % enemyPattern.Length];
        enemyNextAction = action.type;
        enemyNextValue = action.value;
        if (enemyUI != null) enemyUI.ShowIntent(enemyNextAction, enemyNextValue);
    }

    private IEnumerator ExecuteEnemyTurn()
    {
        enemyBlock = 0;
        if (enemyUI != null) enemyUI.UpdateHP(enemyHP, enemyBlock);

        yield return new WaitForSeconds(0.3f);

        if (enemyNextAction == "Attack")
        {
            isActionInProgress = true;
            if (enemyVisual != null && playerVisual != null)
            {
                Vector2 targetPos = playerVisual.GetHomePosition() + new Vector2(130f, 0);
                enemyVisual.WalkAndAttack(targetPos, false,
                    onHit: () =>
                    {
                        DealDamageToPlayer(enemyNextValue);
                        playerVisual.PlayHurt();
                        StartCoroutine(ShakeObject(playerVisual.rectTransform));
                    },
                    onComplete: () =>
                    {
                        isActionInProgress = false;
                        if (playerHP <= 0)
                        {
                            playerVisual.PlayDeath();
                            StartCoroutine(DefeatSequence());
                        }
                    });

                while (isActionInProgress)
                    yield return null;
            }
            else
            {
                DealDamageToPlayer(enemyNextValue);
                isActionInProgress = false;
                if (playerHP <= 0)
                    StartCoroutine(DefeatSequence());
            }
        }
        else if (enemyNextAction == "Defend")
        {
            enemyBlock += enemyNextValue;
            if (enemyUI != null)
            {
                enemyUI.UpdateHP(enemyHP, enemyBlock);
                SpawnPopup(enemyUI.transform, $"+{enemyNextValue} Block", new Color(0.3f, 0.6f, 0.95f));
            }
            yield return new WaitForSeconds(0.4f);
        }

        enemyTurnCount++;
        yield return new WaitForSeconds(0.4f);
    }

    private void ClearHandDisplay()
    {
        hoveredCard = null;
        if (enemyVisual != null) enemyVisual.SetTargetHighlighted(false, false, Color.white);
        if (playerVisual != null) playerVisual.SetTargetHighlighted(false, false, Color.white);

        foreach (var obj in handCardObjects)
        {
            if (obj != null) Destroy(obj);
        }
        handCardObjects.Clear();
    }

    private void UpdateAllCardStates()
    {
        foreach (var obj in handCardObjects)
        {
            if (obj != null)
            {
                CardUI cardUI = obj.GetComponent<CardUI>();
                if (cardUI != null) cardUI.UpdatePlayableState();
            }
        }
    }

    private void UpdatePileCounters()
    {
        if (playerUI != null)
            playerUI.UpdatePiles(drawPile.Count, discardPile.Count);
    }

    // ─── GAME OVER ───

    private IEnumerator VictorySequence()
    {
        combatEnded = true;
        isPlayerTurn = false;
        if (endTurnButton != null) endTurnButton.interactable = false;
        if (turnIndicatorText != null) turnIndicatorText.text = "VICTORY!";

        yield return new WaitForSeconds(1.2f);

        if (victoryPanel != null) victoryPanel.SetActive(true);
    }

    private IEnumerator DefeatSequence()
    {
        combatEnded = true;
        isPlayerTurn = false;
        if (endTurnButton != null) endTurnButton.interactable = false;
        if (turnIndicatorText != null) turnIndicatorText.text = "DEFEATED";

        yield return new WaitForSeconds(1.2f);

        if (defeatPanel != null) defeatPanel.SetActive(true);
    }

    public void RestartCombat()
    {
        ClearHandDisplay();
        StartCombat();
    }

    // ─── VISUAL FEEDBACK ───

    private void SpawnPopup(Transform target, string text, Color color)
    {
        if (damagePopupPrefab == null || mainCanvas == null) return;

        GameObject popup = Instantiate(damagePopupPrefab, mainCanvas.transform);
        RectTransform popupRT = popup.GetComponent<RectTransform>();

        Vector3 worldPos = target.position;
        popupRT.position = worldPos + new Vector3(Random.Range(-25f, 25f), 45f, 0f);

        TextMeshProUGUI popupText = popup.GetComponentInChildren<TextMeshProUGUI>();
        if (popupText != null)
        {
            popupText.text = text;
            popupText.color = color;
        }

        StartCoroutine(AnimatePopup(popup));
    }

    private IEnumerator AnimatePopup(GameObject popup)
    {
        if (popup == null) yield break;

        RectTransform rt = popup.GetComponent<RectTransform>();
        TextMeshProUGUI text = popup.GetComponentInChildren<TextMeshProUGUI>();
        Vector3 startPos = rt.position;
        float duration = 1.1f;
        float elapsed = 0f;

        while (elapsed < duration && popup != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            rt.position = startPos + new Vector3(0, t * 70f, 0);
            if (text != null)
            {
                Color c = text.color;
                c.a = 1f - Mathf.Pow(t, 2);
                text.color = c;
            }

            yield return null;
        }

        if (popup != null) Destroy(popup);
    }

    private IEnumerator ShakeObject(RectTransform target)
    {
        if (target == null) yield break;
        Vector2 orig = target.anchoredPosition;
        float elapsed = 0f;
        float duration = 0.22f;
        float magnitude = 14f;

        while (elapsed < duration && target != null)
        {
            elapsed += Time.deltaTime;
            float damper = 1f - (elapsed / duration);
            float x = orig.x + Random.Range(-1f, 1f) * magnitude * damper;
            float y = orig.y + Random.Range(-1f, 1f) * magnitude * damper;
            target.anchoredPosition = new Vector2(x, y);
            yield return null;
        }

        if (target != null)
            target.anchoredPosition = orig;
    }
}
