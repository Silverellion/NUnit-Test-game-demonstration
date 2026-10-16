using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Bootstraps the entire Slay the Spire inspired card combat scene at runtime.
/// Builds the open combat arena, character animations (walk up, strike, walk back),
/// centered hand, drag-to-play worn fantasy cards, and HUD counters.
/// </summary>
public class CombatSceneBootstrapper : MonoBehaviour
{
    private Canvas mainCanvas;
    private CombatManager combatManager;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoStartGameIfMissing()
    {
        if (FindAnyObjectByType<CombatManager>() == null && FindAnyObjectByType<CombatSceneBootstrapper>() == null)
        {
            Debug.Log("[CombatSceneBootstrapper] Auto-creating CombatBootstrapper in active scene.");
            GameObject bootstrapperObj = new GameObject("CombatBootstrapper");
            bootstrapperObj.AddComponent<CombatSceneBootstrapper>();
        }
    }

    void Awake()
    {
        BuildScene();
    }

    private void BuildScene()
    {
        // ─── CAMERA ───
        if (Camera.main == null && FindAnyObjectByType<Camera>() == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            Camera cam = camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.08f, 0.12f, 1f);
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.transform.position = new Vector3(0, 0, -10f);
        }

        // ─── CANVAS ───
        GameObject canvasObj = new GameObject("MainCanvas");
        mainCanvas = canvasObj.AddComponent<Canvas>();
        mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        mainCanvas.sortingOrder = 0;
        mainCanvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.Normal | AdditionalCanvasShaderChannels.Tangent;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        scaler.dynamicPixelsPerUnit = 4f;

        canvasObj.AddComponent<GraphicRaycaster>();

        // ─── EVENT SYSTEM (supports new Input System & legacy) ───
        var es = FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
        if (es == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            es = eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
#if ENABLE_INPUT_SYSTEM
            eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif
        }
        else
        {
#if ENABLE_INPUT_SYSTEM
            var standalone = es.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            if (standalone != null)
            {
                Destroy(standalone);
                if (es.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() == null)
                {
                    es.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                }
            }
#endif
        }

        // ─── SOLID BLACK LAND (Bottom 30% of scene) ───
        GameObject blackBackdrop = CreateUIElement("BlackBackdrop", canvasObj.transform);
        RectTransform blackBackdropRT = blackBackdrop.GetComponent<RectTransform>();
        StretchFull(blackBackdropRT);
        Image blackBackdropImg = blackBackdrop.AddComponent<Image>();
        blackBackdropImg.color = Color.black;
        blackBackdropImg.raycastTarget = false;

        GameObject groundFadeObj = CreateUIElement("GroundFadeOverlay", canvasObj.transform);
        RectTransform groundFadeRT = groundFadeObj.GetComponent<RectTransform>();
        groundFadeRT.anchorMin = new Vector2(0, 0);
        groundFadeRT.anchorMax = new Vector2(1, 0.30f);
        groundFadeRT.offsetMin = Vector2.zero;
        groundFadeRT.offsetMax = Vector2.zero;
        CanvasGroup groundFadeCG = groundFadeObj.AddComponent<CanvasGroup>();

        Image solidImg = groundFadeObj.AddComponent<Image>();
        solidImg.color = Color.black;
        solidImg.raycastTarget = false;

        // ─── BACKGROUND
        GameObject bgObj = CreateUIElement("ArenaBackground", canvasObj.transform);
        RectTransform bgRT = bgObj.GetComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0f, 0.15f);
        bgRT.anchorMax = new Vector2(1f, 1.15f);
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;

        Image bgImage = bgObj   .AddComponent<Image>();
        bgImage.color = Color.white;

        AnimatedBackground animBg = bgObj.AddComponent<AnimatedBackground>();
        animBg.backgroundImage = bgImage;
        animBg.fps = 10f;
        animBg.LoadFrames();

        // Seamless Fade Gradient attached to bottom of background
        // Transitions effortlessly into the 30% solid black bottom part of the scene
        GameObject bgTransition = CreateUIElement("BottomTransitionFade", bgObj.transform);
        RectTransform transRT = bgTransition.GetComponent<RectTransform>();
        transRT.anchorMin = new Vector2(0, 0);
        transRT.anchorMax = new Vector2(1, 0);
        transRT.pivot = new Vector2(0.5f, 0);
        transRT.anchoredPosition = Vector2.zero;
        transRT.sizeDelta = new Vector2(0, 240f);
        Image transImg = bgTransition.AddComponent<Image>();
        transImg.sprite = UISprites.BlackFadeGradient;
        transImg.color = Color.white;
        transImg.raycastTarget = false;

        // ─── BATTLEFIELD ARENA (Top 70% of screen) ───
        GameObject arenaArea = CreateUIElement("BattlefieldArea", canvasObj.transform);
        RectTransform arenaRT = arenaArea.GetComponent<RectTransform>();
        arenaRT.anchorMin = new Vector2(0, 0.30f);
        arenaRT.anchorMax = new Vector2(1, 1f);
        arenaRT.offsetMin = Vector2.zero;
        arenaRT.offsetMax = Vector2.zero;

        // Subtle arena floor line (smooth fade, rounded ends)
        GameObject floorLine = CreateUIElement("ArenaFloor", arenaArea.transform);
        RectTransform floorRT = floorLine.GetComponent<RectTransform>();
        floorRT.anchorMin = new Vector2(0.05f, 0.30f);
        floorRT.anchorMax = new Vector2(0.95f, 0.30f);
        floorRT.sizeDelta = new Vector2(0, 4f);
        Image floorImage = floorLine.AddComponent<Image>();
        floorImage.sprite = UISprites.DividerLine;
        floorImage.color = new Color(1f, 1f, 1f, 0.35f);

        // ─── SOLDIER (Left side of battlefield) ───
        PlayerUI playerUI = BuildPlayerCharacter(arenaArea.transform, out CharacterCombatVisual playerVisual, out CanvasGroup playerStatusCG);

        // ─── ORC (Right side of battlefield) ───
        EnemyUI enemyUI = BuildEnemyCharacter(arenaArea.transform, out CharacterCombatVisual enemyVisual, out CanvasGroup enemyStatusCG);

        // ─── HAND AREA (Bottom 33% of screen) ───
        GameObject handArea = CreateUIElement("HandArea", canvasObj.transform);
        RectTransform handAreaRT = handArea.GetComponent<RectTransform>();
        handAreaRT.anchorMin = new Vector2(0, 0);
        handAreaRT.anchorMax = new Vector2(1, 0.33f);
        handAreaRT.offsetMin = Vector2.zero;
        handAreaRT.offsetMax = Vector2.zero;

        // Dark navy hand area backing with rounded top corners, gradient & gold top rim
        Image handBg = handArea.AddComponent<Image>();
        handBg.sprite = UISprites.HandPanel;
        handBg.type = Image.Type.Sliced;
        handBg.color = Color.white;

        // ─── HAND CONTAINER (Centered in the middle of the screen!) ───
        GameObject handContainer = CreateUIElement("HandContainer", handArea.transform);
        RectTransform handContRT = handContainer.GetComponent<RectTransform>();
        handContRT.anchorMin = new Vector2(0.5f, 0);
        handContRT.anchorMax = new Vector2(0.5f, 0);
        handContRT.pivot = new Vector2(0.5f, 0);
        handContRT.anchoredPosition = new Vector2(0f, 12f);
        handContRT.sizeDelta = new Vector2(1150, 480);

        // ─── TURN INDICATOR (Above Hand) ───
        GameObject turnIndicator = CreateTextElement("TurnIndicator", canvasObj.transform, "YOUR TURN", 28, TextAlignmentOptions.Center, bold: true);
        RectTransform turnRT = turnIndicator.GetComponent<RectTransform>();
        turnRT.anchorMin = new Vector2(0.5f, 0.34f);
        turnRT.anchorMax = new Vector2(0.5f, 0.34f);
        turnRT.pivot = new Vector2(0.5f, 0f);
        turnRT.anchoredPosition = Vector2.zero;
        turnRT.sizeDelta = new Vector2(300, 36);
        TextMeshProUGUI turnText = turnIndicator.GetComponent<TextMeshProUGUI>();
        turnText.color = new Color(0.95f, 0.85f, 0.50f);

        // ─── ENERGY ORB (Bottom Left of screen) ───
        GameObject energyOrb = CreateUIElement("EnergyOrb", handArea.transform);
        RectTransform energyRT = energyOrb.GetComponent<RectTransform>();
        energyRT.anchorMin = new Vector2(0, 0.5f);
        energyRT.anchorMax = new Vector2(0, 0.5f);
        energyRT.pivot = new Vector2(0.5f, 0.5f);
        energyRT.anchoredPosition = new Vector2(130f, 0f);
        energyRT.sizeDelta = new Vector2(90, 90);

        Image energyBg = energyOrb.AddComponent<Image>();
        energyBg.sprite = UISprites.CostBadgePlayable;
        energyBg.color = Color.white;

        GameObject energyTextObj = CreateTextElement("EnergyText", energyOrb.transform, "3/3\n<size=12>ENERGY</size>", 26, TextAlignmentOptions.Center, bold: true);
        RectTransform energyTextRT = energyTextObj.GetComponent<RectTransform>();
        StretchFull(energyTextRT);
        TextMeshProUGUI energyTMP = energyTextObj.GetComponent<TextMeshProUGUI>();
        energyTMP.color = Color.white;
        playerUI.energyText = energyTMP;

        // ─── END TURN BUTTON (Bottom Right of screen) ───
        GameObject endTurnBtn = CreateButton("EndTurnButton", handArea.transform, "END TURN",
            new Color(0.65f, 0.22f, 0.18f), new Color(0.98f, 0.92f, 0.75f), UISprites.EndTurnButton);
        RectTransform endTurnRT = endTurnBtn.GetComponent<RectTransform>();
        endTurnRT.anchorMin = new Vector2(1, 0.5f);
        endTurnRT.anchorMax = new Vector2(1, 0.5f);
        endTurnRT.pivot = new Vector2(0.5f, 0.5f);
        endTurnRT.anchoredPosition = new Vector2(-130f, 0f);
        endTurnRT.sizeDelta = new Vector2(155, 54);

        // ─── DRAW PILE (Far Bottom Left) ───
        GameObject drawPileObj = BuildPileBadge("DrawPile", handArea.transform,
            new Vector2(50f, 32f), "DRAW", new Color(0.18f, 0.35f, 0.60f));
        TextMeshProUGUI drawCount = drawPileObj.transform.Find("Count").GetComponent<TextMeshProUGUI>();
        playerUI.drawPileText = drawCount;

        // ─── DISCARD PILE (Far Bottom Right) ───
        GameObject discardPileObj = BuildPileBadge("DiscardPile", handArea.transform,
            new Vector2(-50f, 32f), "DISCARD", new Color(0.50f, 0.25f, 0.18f));
        TextMeshProUGUI discardCount = discardPileObj.transform.Find("Count").GetComponent<TextMeshProUGUI>();
        playerUI.discardPileText = discardCount;

        // ─── CARD PREFAB (Worn fantasy trading card) ───
        GameObject cardPrefab = BuildCardPrefab();
        cardPrefab.SetActive(false);

        // ─── DAMAGE POPUP PREFAB ───
        GameObject popupPrefab = BuildPopupPrefab();
        popupPrefab.SetActive(false);

        // ─── GAME OVER PANELS ───
        GameObject victoryPanel = BuildGameOverPanel("VictoryPanel", canvasObj.transform,
            "VICTORY!", "The Orc has been vanquished.", new Color(0.95f, 0.85f, 0.35f));
        GameObject defeatPanel = BuildGameOverPanel("DefeatPanel", canvasObj.transform,
            "DEFEAT", "You have fallen in combat.", new Color(0.90f, 0.25f, 0.25f));

        // ─── YOUR TURN BANNER ───
        GameObject turnBannerObj = BuildTurnBanner(canvasObj.transform);

        // ─── COMBAT MANAGER ───
        GameObject managerObj = new GameObject("CombatManager");
        combatManager = managerObj.AddComponent<CombatManager>();
        combatManager.playerUI = playerUI;
        combatManager.enemyUI = enemyUI;
        combatManager.playerVisual = playerVisual;
        combatManager.enemyVisual = enemyVisual;
        combatManager.backgroundRT = bgRT;
        combatManager.groundFadeCanvasGroup = groundFadeCG;
        combatManager.playerStatusCanvasGroup = playerStatusCG;
        combatManager.enemyStatusCanvasGroup = enemyStatusCG;
        combatManager.handContainer = handContainer.transform;
        combatManager.cardPrefab = cardPrefab;
        combatManager.endTurnButton = endTurnBtn.GetComponent<Button>();
        combatManager.endTurnButtonText = endTurnBtn.GetComponentInChildren<TextMeshProUGUI>();
        combatManager.turnIndicatorText = turnText;
        combatManager.turnBanner = turnBannerObj.GetComponent<RectTransform>();
        combatManager.victoryPanel = victoryPanel;
        combatManager.defeatPanel = defeatPanel;
        combatManager.victoryRestartButton = victoryPanel.GetComponentInChildren<Button>(true);
        combatManager.defeatRestartButton = defeatPanel.GetComponentInChildren<Button>(true);
        combatManager.damagePopupPrefab = popupPrefab;
        combatManager.mainCanvas = mainCanvas;

#if UNITY_EDITOR
        ValidateSceneUI();
#endif
    }

#if UNITY_EDITOR
    public static void ValidateSceneUI()
    {
        int warningCount = 0;
        foreach (var graphic in FindObjectsByType<Graphic>())
        {
            // Character battle sprites and outlines inside SpriteContainer are intentionally flipped to face opponents
            if (graphic.GetComponentInParent<CharacterCombatVisual>() != null)
                continue;
                
            if (graphic.transform.lossyScale.x < 0f)
            {
                Debug.LogWarning($"[UI Validator] Mirrored UI Graphic detected on '{graphic.gameObject.name}' (lossyScale.x = {graphic.transform.lossyScale.x})!", graphic);
                warningCount++;
            }
        }

        foreach (var tmp in FindObjectsByType<TMP_Text>())
        {
            if (tmp.transform.lossyScale.x < 0f)
            {
                Debug.LogWarning($"[UI Validator] Mirrored TMP_Text detected on '{tmp.gameObject.name}' (lossyScale.x = {tmp.transform.lossyScale.x})!", tmp);
                warningCount++;
            }
        }

        if (warningCount == 0)
        {
            Debug.Log("[UI Validator] Validation passed: No mirrored TMP_Text or UI Graphic transforms detected.");
        }
    }
#endif

    // ─── SOLDIER BUILDER ───

    private PlayerUI BuildPlayerCharacter(Transform arenaParent, out CharacterCombatVisual visual, out CanvasGroup statusCG)
    {
        GameObject playerRoot = CreateUIElement("Soldier", arenaParent);
        RectTransform rootRT = playerRoot.GetComponent<RectTransform>();
        rootRT.anchorMin = new Vector2(0.5f, 0.5f);
        rootRT.anchorMax = new Vector2(0.5f, 0.5f);
        rootRT.pivot = new Vector2(0.5f, 0f); // Feet anchored to ground line
        rootRT.anchoredPosition = new Vector2(-380f, -50f);
        rootRT.sizeDelta = new Vector2(152, 184);

        // Targeting ring indicator beneath soldier
        GameObject targetInd = CreateUIElement("TargetIndicator", playerRoot.transform);
        RectTransform indRT = targetInd.GetComponent<RectTransform>();
        indRT.anchorMin = new Vector2(0.5f, 0f);
        indRT.anchorMax = new Vector2(0.5f, 0f);
        indRT.pivot = new Vector2(0.5f, 0.5f);
        indRT.anchoredPosition = new Vector2(0f, 10f);
        indRT.sizeDelta = new Vector2(130, 38);
        Image indImg = targetInd.AddComponent<Image>();
        indImg.color = new Color(0.25f, 0.70f, 1f, 0.45f);
        indImg.raycastTarget = false;
        targetInd.SetActive(false);

        GameObject spriteContainer = CreateUIElement("SpriteContainer", playerRoot.transform);
        RectTransform spriteRT = spriteContainer.GetComponent<RectTransform>();
        spriteRT.anchorMin = new Vector2(0.5f, 0f);
        spriteRT.anchorMax = new Vector2(0.5f, 0f);
        spriteRT.pivot = new Vector2(0.5f, 0f);
        spriteRT.anchoredPosition = Vector2.zero;
        spriteRT.sizeDelta = new Vector2(152, 184);

        // Child 0: OutlineRoot (renders BEHIND character sprite)
        GameObject outlineObj = CreateUIElement("OutlineRoot", spriteContainer.transform);
        RectTransform outlineRT = outlineObj.GetComponent<RectTransform>();
        outlineRT.anchorMin = new Vector2(0.5f, 0f);
        outlineRT.anchorMax = new Vector2(0.5f, 0f);
        outlineRT.pivot = new Vector2(0.5f, 0f);
        outlineRT.anchoredPosition = new Vector2(0f, -6f);
        outlineRT.sizeDelta = new Vector2(176, 208);

        // Child 1: CharacterSprite (renders IN FRONT of outline)
        GameObject charObj = CreateUIElement("CharacterSprite", spriteContainer.transform);
        RectTransform charRT = charObj.GetComponent<RectTransform>();
        charRT.anchorMin = new Vector2(0.5f, 0f);
        charRT.anchorMax = new Vector2(0.5f, 0f);
        charRT.pivot = new Vector2(0.5f, 0f);
        charRT.anchoredPosition = Vector2.zero;
        charRT.sizeDelta = new Vector2(152, 184);
        Image spriteImage = charObj.AddComponent<Image>();
        spriteImage.raycastTarget = false;

        visual = playerRoot.AddComponent<CharacterCombatVisual>();
        visual.characterImage = spriteImage;
        visual.spriteContainer = spriteRT;
        visual.rectTransform = rootRT;
        visual.outlineRoot = outlineObj;
        visual.targetIndicator = targetInd;
        visual.baseFacingRight = true;
        visual.SetFacing(true);
        visual.SetHomePosition(new Vector2(-380f, -50f));
        visual.SetupOutline();

#if UNITY_EDITOR
        visual.LoadSpritesFromFolder("Assets/Assets/Soldier with shadows", "Soldier");
#endif

        PlayerUI playerUI = playerRoot.AddComponent<PlayerUI>();
        playerUI.playerSprite = spriteImage;

        // Floating Status Bar beneath Soldier (wrapped in CanvasGroup for intro animation)
        GameObject hudObj = CreateUIElement("PlayerHUD", playerRoot.transform);
        RectTransform hudRT = hudObj.GetComponent<RectTransform>();
        hudRT.anchorMin = Vector2.zero;
        hudRT.anchorMax = Vector2.one;
        hudRT.offsetMin = Vector2.zero;
        hudRT.offsetMax = Vector2.zero;
        statusCG = hudObj.AddComponent<CanvasGroup>();

        BuildPlayerStatusDisplay(hudObj.transform, playerUI);

        return playerUI;
    }

    private void BuildPlayerStatusDisplay(Transform parent, PlayerUI playerUI)
    {
        GameObject statusObj = CreateUIElement("StatusDisplay", parent);
        RectTransform statusRT = statusObj.GetComponent<RectTransform>();
        statusRT.anchorMin = new Vector2(0.5f, 0);
        statusRT.anchorMax = new Vector2(0.5f, 0);
        statusRT.pivot = new Vector2(0.5f, 1f);
        statusRT.anchoredPosition = new Vector2(0f, -12f);
        statusRT.sizeDelta = new Vector2(190, 52);

        // Name plate
        GameObject nameObj = CreateUIElement("NamePlate", statusObj.transform);
        RectTransform nameRT = nameObj.GetComponent<RectTransform>();
        nameRT.anchorMin = new Vector2(0, 0.55f);
        nameRT.anchorMax = new Vector2(1, 1f);
        nameRT.offsetMin = Vector2.zero;
        nameRT.offsetMax = Vector2.zero;
        Image nameBg = nameObj.AddComponent<Image>();
        nameBg.sprite = UISprites.NamePlate;
        nameBg.type = Image.Type.Sliced;
        nameBg.color = Color.white;

        GameObject nameTextObj = CreateTextElement("Name", nameObj.transform, "SOLDIER", 16, TextAlignmentOptions.Center, bold: true, autoSize: true, minSize: 11f, maxSize: 18f);
        RectTransform ntRT = nameTextObj.GetComponent<RectTransform>();
        StretchFull(ntRT);
        TextMeshProUGUI nameTMP = nameTextObj.GetComponent<TextMeshProUGUI>();
        nameTMP.color = new Color(0.95f, 0.90f, 0.75f);
        playerUI.nameText = nameTMP;

        // HP Bar
        BuildHPBar(statusObj.transform, playerUI);
    }

    // ─── ORC BUILDER ───

    private EnemyUI BuildEnemyCharacter(Transform arenaParent, out CharacterCombatVisual visual, out CanvasGroup statusCG)
    {
        GameObject enemyRoot = CreateUIElement("Orc", arenaParent);
        RectTransform rootRT = enemyRoot.GetComponent<RectTransform>();
        rootRT.anchorMin = new Vector2(0.5f, 0.5f);
        rootRT.anchorMax = new Vector2(0.5f, 0.5f);
        rootRT.pivot = new Vector2(0.5f, 0f); // Feet anchored to ground line
        rootRT.anchoredPosition = new Vector2(380f, -50f);
        rootRT.sizeDelta = new Vector2(192, 192);

        // Targeting ring indicator beneath orc
        GameObject targetInd = CreateUIElement("TargetIndicator", enemyRoot.transform);
        RectTransform indRT = targetInd.GetComponent<RectTransform>();
        indRT.anchorMin = new Vector2(0.5f, 0f);
        indRT.anchorMax = new Vector2(0.5f, 0f);
        indRT.pivot = new Vector2(0.5f, 0.5f);
        indRT.anchoredPosition = new Vector2(0f, 10f);
        indRT.sizeDelta = new Vector2(150, 42);
        Image indImg = targetInd.AddComponent<Image>();
        indImg.color = new Color(1f, 0.25f, 0.25f, 0.45f);
        indImg.raycastTarget = false;
        targetInd.SetActive(false);

        GameObject spriteContainer = CreateUIElement("SpriteContainer", enemyRoot.transform);
        RectTransform spriteRT = spriteContainer.GetComponent<RectTransform>();
        spriteRT.anchorMin = new Vector2(0.5f, 0f);
        spriteRT.anchorMax = new Vector2(0.5f, 0f);
        spriteRT.pivot = new Vector2(0.5f, 0f);
        spriteRT.anchoredPosition = Vector2.zero;
        spriteRT.sizeDelta = new Vector2(192, 192);

        // Child 0: OutlineRoot (renders BEHIND character sprite)
        GameObject outlineObj = CreateUIElement("OutlineRoot", spriteContainer.transform);
        RectTransform outlineRT = outlineObj.GetComponent<RectTransform>();
        outlineRT.anchorMin = new Vector2(0.5f, 0f);
        outlineRT.anchorMax = new Vector2(0.5f, 0f);
        outlineRT.pivot = new Vector2(0.5f, 0f);
        outlineRT.anchoredPosition = new Vector2(0f, -6f);
        outlineRT.sizeDelta = new Vector2(216, 216);

        // Child 1: CharacterSprite (renders IN FRONT of outline)
        GameObject charObj = CreateUIElement("CharacterSprite", spriteContainer.transform);
        RectTransform charRT = charObj.GetComponent<RectTransform>();
        charRT.anchorMin = new Vector2(0.5f, 0f);
        charRT.anchorMax = new Vector2(0.5f, 0f);
        charRT.pivot = new Vector2(0.5f, 0f);
        charRT.anchoredPosition = Vector2.zero;
        charRT.sizeDelta = new Vector2(192, 192);
        Image spriteImage = charObj.AddComponent<Image>();
        spriteImage.raycastTarget = false;

        visual = enemyRoot.AddComponent<CharacterCombatVisual>();
        visual.characterImage = spriteImage;
        visual.spriteContainer = spriteRT;
        visual.rectTransform = rootRT;
        visual.outlineRoot = outlineObj;
        visual.targetIndicator = targetInd;
        visual.baseFacingRight = false;
        visual.SetFacing(false); // Orc faces left towards soldier, only sprite container is flipped
        visual.SetHomePosition(new Vector2(380f, -50f));
        visual.SetupOutline();

#if UNITY_EDITOR
        visual.LoadSpritesFromFolder("Assets/Assets/Orc with shadows", "Orc");
#endif

        EnemyUI enemyUI = enemyRoot.AddComponent<EnemyUI>();
        enemyUI.enemySprite = spriteImage;

        // Floating Status Bar beneath Orc & Intent Badge (wrapped in CanvasGroup for intro animation)
        GameObject hudObj = CreateUIElement("EnemyHUD", enemyRoot.transform);
        RectTransform hudRT = hudObj.GetComponent<RectTransform>();
        hudRT.anchorMin = Vector2.zero;
        hudRT.anchorMax = Vector2.one;
        hudRT.offsetMin = Vector2.zero;
        hudRT.offsetMax = Vector2.zero;
        statusCG = hudObj.AddComponent<CanvasGroup>();

        BuildEnemyStatusDisplay(hudObj.transform, enemyUI);
        BuildEnemyIntentBadge(hudObj.transform, enemyUI);

        return enemyUI;
    }

    private void BuildEnemyStatusDisplay(Transform parent, EnemyUI enemyUI)
    {
        GameObject statusObj = CreateUIElement("StatusDisplay", parent);
        RectTransform statusRT = statusObj.GetComponent<RectTransform>();
        statusRT.anchorMin = new Vector2(0.5f, 0);
        statusRT.anchorMax = new Vector2(0.5f, 0);
        statusRT.pivot = new Vector2(0.5f, 1f);
        statusRT.anchoredPosition = new Vector2(0f, -12f);
        statusRT.sizeDelta = new Vector2(190, 52);

        // Name plate
        GameObject nameObj = CreateUIElement("NamePlate", statusObj.transform);
        RectTransform nameRT = nameObj.GetComponent<RectTransform>();
        nameRT.anchorMin = new Vector2(0, 0.55f);
        nameRT.anchorMax = new Vector2(1, 1f);
        nameRT.offsetMin = Vector2.zero;
        nameRT.offsetMax = Vector2.zero;
        Image nameBg = nameObj.AddComponent<Image>();
        nameBg.sprite = UISprites.NamePlate;
        nameBg.type = Image.Type.Sliced;
        nameBg.color = Color.white;

        GameObject nameTextObj = CreateTextElement("Name", nameObj.transform, "ORC", 16, TextAlignmentOptions.Center, bold: true, autoSize: true, minSize: 11f, maxSize: 18f);
        RectTransform ntRT = nameTextObj.GetComponent<RectTransform>();
        StretchFull(ntRT);
        TextMeshProUGUI nameTMP = nameTextObj.GetComponent<TextMeshProUGUI>();
        nameTMP.color = new Color(0.98f, 0.78f, 0.72f);
        enemyUI.nameText = nameTMP;

        // HP Bar
        BuildEnemyHPBar(statusObj.transform, enemyUI);
    }

    private void BuildEnemyIntentBadge(Transform parent, EnemyUI enemyUI)
    {
        GameObject intentBadge = CreateUIElement("IntentBadge", parent);
        RectTransform badgeRT = intentBadge.GetComponent<RectTransform>();
        badgeRT.anchorMin = new Vector2(0.5f, 1f);
        badgeRT.anchorMax = new Vector2(0.5f, 1f);
        badgeRT.pivot = new Vector2(0.5f, 0f);
        badgeRT.anchoredPosition = new Vector2(0f, 16f);
        badgeRT.sizeDelta = new Vector2(120, 44);

        Image badgeBg = intentBadge.AddComponent<Image>();
        badgeBg.sprite = UISprites.IntentBadgeAttack;
        badgeBg.type = Image.Type.Sliced;
        badgeBg.color = Color.white;

        // Intent Icon (Sword / Shield) on left
        GameObject iconObj = CreateUIElement("IntentIcon", intentBadge.transform);
        RectTransform iconRT = iconObj.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0f, 0.5f);
        iconRT.anchorMax = new Vector2(0f, 0.5f);
        iconRT.pivot = new Vector2(0.5f, 0.5f);
        iconRT.anchoredPosition = new Vector2(24f, 0f);
        iconRT.sizeDelta = new Vector2(26, 26);
        Image iconImg = iconObj.AddComponent<Image>();
        iconImg.sprite = UISprites.GetSwordIcon();
        iconImg.preserveAspect = true;
        iconImg.raycastTarget = false;

        GameObject intentTextObj = CreateTextElement("IntentText", intentBadge.transform, "ATK 8", 16, TextAlignmentOptions.Center, bold: true, autoSize: true, minSize: 10f, maxSize: 16f);
        RectTransform intentTextRT = intentTextObj.GetComponent<RectTransform>();
        intentTextRT.anchorMin = new Vector2(0.35f, 0f);
        intentTextRT.anchorMax = new Vector2(1f, 1f);
        intentTextRT.offsetMin = Vector2.zero;
        intentTextRT.offsetMax = Vector2.zero;
        TextMeshProUGUI intentTMP = intentTextObj.GetComponent<TextMeshProUGUI>();
        intentTMP.color = Color.white;

        enemyUI.intentContainer = intentBadge;
        enemyUI.intentIcon = iconImg;
        enemyUI.intentText = intentTMP;
    }

    private void BuildHPBar(Transform parent, PlayerUI playerUI)
    {
        GameObject barRoot = CreateUIElement("HPBarRoot", parent);
        RectTransform barRT = barRoot.GetComponent<RectTransform>();
        barRT.anchorMin = new Vector2(0, 0);
        barRT.anchorMax = new Vector2(1, 0.50f);
        barRT.offsetMin = Vector2.zero;
        barRT.offsetMax = Vector2.zero;

        // Bar background track
        Image bgImg = barRoot.AddComponent<Image>();
        bgImg.sprite = UISprites.BarTrack;
        bgImg.type = Image.Type.Sliced;
        bgImg.color = Color.white;

        // Fill area
        GameObject fillArea = CreateUIElement("FillArea", barRoot.transform);
        RectTransform fillRT = fillArea.GetComponent<RectTransform>();
        StretchFull(fillRT);

        GameObject fillObj = CreateUIElement("Fill", fillArea.transform);
        RectTransform fillImgRT = fillObj.GetComponent<RectTransform>();
        StretchFull(fillImgRT);
        Image fillImg = fillObj.AddComponent<Image>();
        fillImg.sprite = UISprites.BarFill;
        fillImg.type = Image.Type.Sliced;
        fillImg.color = new Color(0.25f, 0.80f, 0.35f, 1f);

        Slider slider = barRoot.AddComponent<Slider>();
        slider.fillRect = fillImgRT;
        slider.minValue = 0;
        slider.maxValue = 60;
        slider.value = 60;
        slider.interactable = false;

        GameObject hpTextObj = CreateTextElement("HPText", barRoot.transform, "60/60", 13, TextAlignmentOptions.Center, bold: false);
        RectTransform textRT = hpTextObj.GetComponent<RectTransform>();
        StretchFull(textRT);
        TextMeshProUGUI hpTMP = hpTextObj.GetComponent<TextMeshProUGUI>();
        hpTMP.color = Color.white;

        // Block badge
        GameObject blockBadge = CreateUIElement("BlockBadge", barRoot.transform);
        RectTransform blockRT = blockBadge.GetComponent<RectTransform>();
        blockRT.anchorMin = new Vector2(0, 0.5f);
        blockRT.anchorMax = new Vector2(0, 0.5f);
        blockRT.pivot = new Vector2(1f, 0.5f);
        blockRT.anchoredPosition = new Vector2(-6f, 0f);
        blockRT.sizeDelta = new Vector2(46, 28);

        Image blockBg = blockBadge.AddComponent<Image>();
        blockBg.sprite = UISprites.BlockBadge;
        blockBg.type = Image.Type.Sliced;
        blockBg.color = Color.white;

        GameObject blockTextObj = CreateTextElement("BlockText", blockBadge.transform, "0", 14, TextAlignmentOptions.Center, bold: true);
        RectTransform btRT = blockTextObj.GetComponent<RectTransform>();
        StretchFull(btRT);
        TextMeshProUGUI blockTMP = blockTextObj.GetComponent<TextMeshProUGUI>();
        blockTMP.color = Color.white;

        blockBadge.SetActive(false);

        playerUI.hpSlider = slider;
        playerUI.hpFillImage = fillImg;
        playerUI.hpText = hpTMP;
        playerUI.blockContainer = blockBadge;
        playerUI.blockText = blockTMP;
    }

    private void BuildEnemyHPBar(Transform parent, EnemyUI enemyUI)
    {
        GameObject barRoot = CreateUIElement("HPBarRoot", parent);
        RectTransform barRT = barRoot.GetComponent<RectTransform>();
        barRT.anchorMin = new Vector2(0, 0);
        barRT.anchorMax = new Vector2(1, 0.50f);
        barRT.offsetMin = Vector2.zero;
        barRT.offsetMax = Vector2.zero;

        Image bgImg = barRoot.AddComponent<Image>();
        bgImg.sprite = UISprites.BarTrack;
        bgImg.type = Image.Type.Sliced;
        bgImg.color = Color.white;

        GameObject fillArea = CreateUIElement("FillArea", barRoot.transform);
        RectTransform fillRT = fillArea.GetComponent<RectTransform>();
        StretchFull(fillRT);

        GameObject fillObj = CreateUIElement("Fill", fillArea.transform);
        RectTransform fillImgRT = fillObj.GetComponent<RectTransform>();
        StretchFull(fillImgRT);
        Image fillImg = fillObj.AddComponent<Image>();
        fillImg.sprite = UISprites.BarFill;
        fillImg.type = Image.Type.Sliced;
        fillImg.color = new Color(0.85f, 0.25f, 0.25f, 1f);

        Slider slider = barRoot.AddComponent<Slider>();
        slider.fillRect = fillImgRT;
        slider.minValue = 0;
        slider.maxValue = 50;
        slider.value = 50;
        slider.interactable = false;

        GameObject hpTextObj = CreateTextElement("HPText", barRoot.transform, "50/50", 13, TextAlignmentOptions.Center, bold: false);
        RectTransform textRT = hpTextObj.GetComponent<RectTransform>();
        StretchFull(textRT);
        TextMeshProUGUI hpTMP = hpTextObj.GetComponent<TextMeshProUGUI>();
        hpTMP.color = Color.white;

        // Block badge
        GameObject blockBadge = CreateUIElement("BlockBadge", barRoot.transform);
        RectTransform blockRT = blockBadge.GetComponent<RectTransform>();
        blockRT.anchorMin = new Vector2(1, 0.5f);
        blockRT.anchorMax = new Vector2(1, 0.5f);
        blockRT.pivot = new Vector2(0f, 0.5f);
        blockRT.anchoredPosition = new Vector2(6f, 0f);
        blockRT.sizeDelta = new Vector2(46, 28);

        Image blockBg = blockBadge.AddComponent<Image>();
        blockBg.sprite = UISprites.BlockBadge;
        blockBg.type = Image.Type.Sliced;
        blockBg.color = Color.white;

        GameObject blockTextObj = CreateTextElement("BlockText", blockBadge.transform, "0", 14, TextAlignmentOptions.Center, bold: true);
        RectTransform btRT = blockTextObj.GetComponent<RectTransform>();
        StretchFull(btRT);
        TextMeshProUGUI blockTMP = blockTextObj.GetComponent<TextMeshProUGUI>();
        blockTMP.color = Color.white;

        blockBadge.SetActive(false);

        enemyUI.hpSlider = slider;
        enemyUI.hpFillImage = fillImg;
        enemyUI.hpText = hpTMP;
        enemyUI.blockContainer = blockBadge;
        enemyUI.blockText = blockTMP;
    }

    // ─── CARD PREFAB (Worn fantasy trading card inspired by Slay the Spire) ───

    private GameObject BuildCardPrefab()
    {
        GameObject card = CreateUIElement("CardTemplate", mainCanvas.transform);
        RectTransform cardRT = card.GetComponent<RectTransform>();
        cardRT.sizeDelta = new Vector2(244, 468);
        cardRT.pivot = new Vector2(0.5f, 0.5f);

        // Soft drop shadow behind card
        GameObject shadowObj = CreateUIElement("CardShadow", card.transform);
        RectTransform shadowRT = shadowObj.GetComponent<RectTransform>();
        StretchFull(shadowRT);
        shadowRT.offsetMin = new Vector2(-16, -20);
        shadowRT.offsetMax = new Vector2(16, 12);
        Image shadowImg = shadowObj.AddComponent<Image>();
        shadowImg.sprite = UISprites.SoftShadow;
        shadowImg.type = Image.Type.Sliced;
        shadowImg.color = Color.white;
        shadowImg.raycastTarget = false;

        // 1. Outer Rounded Gold / Iron Frame Border
        Image outerBorder = card.AddComponent<Image>();
        outerBorder.sprite = UISprites.CardBorderPlayable;
        outerBorder.type = Image.Type.Sliced;
        outerBorder.color = Color.white;

        // 2. Inner Dark Card Stock
        GameObject cardInner = CreateUIElement("CardInner", card.transform);
        RectTransform innerRT = cardInner.GetComponent<RectTransform>();
        StretchFull(innerRT);
        innerRT.offsetMin = new Vector2(5, 5);
        innerRT.offsetMax = new Vector2(-5, -5);
        Image innerImage = cardInner.AddComponent<Image>();
        innerImage.sprite = UISprites.CardBackground;
        innerImage.type = Image.Type.Sliced;
        innerImage.color = Color.white;

        // 3. Artwork Box (Strictly 1:1 Square - 220x220, top tucked underneath Title Banner to eliminate any dark horizontal seam)
        GameObject artFrame = CreateUIElement("ArtworkFrame", cardInner.transform);
        RectTransform artFrameRT = artFrame.GetComponent<RectTransform>();
        artFrameRT.anchorMin = new Vector2(0.5f, 1f);
        artFrameRT.anchorMax = new Vector2(0.5f, 1f);
        artFrameRT.pivot = new Vector2(0.5f, 1f);
        artFrameRT.anchoredPosition = new Vector2(0f, -46f);
        artFrameRT.sizeDelta = new Vector2(220f, 222f);
        Image artFrameImage = artFrame.AddComponent<Image>();
        artFrameImage.sprite = UISprites.ArtworkFrame;
        artFrameImage.type = Image.Type.Sliced;
        artFrameImage.color = Color.white;

        GameObject artArea = CreateUIElement("ArtworkPlaceholder", artFrame.transform);
        RectTransform artRT = artArea.GetComponent<RectTransform>();
        StretchFull(artRT);
        artRT.offsetMin = new Vector2(3, 3);
        artRT.offsetMax = new Vector2(-3, -3);
        Image artImage = artArea.AddComponent<Image>();
        artImage.sprite = UISprites.GetCardArtwork("Strike");
        artImage.preserveAspect = true;
        artImage.color = Color.white;

        // 4. Top Banner (Title Bar) with rounded top corners (renders OVER the top of ArtworkFrame, seamlessly flush)
        GameObject headerBanner = CreateUIElement("HeaderBanner", cardInner.transform);
        RectTransform headerRT = headerBanner.GetComponent<RectTransform>();
        headerRT.anchorMin = new Vector2(0f, 1f);
        headerRT.anchorMax = new Vector2(1f, 1f);
        headerRT.pivot = new Vector2(0.5f, 1f);
        headerRT.anchoredPosition = Vector2.zero;
        headerRT.sizeDelta = new Vector2(0f, 48f);
        Image headerImage = headerBanner.AddComponent<Image>();
        headerImage.sprite = UISprites.GetCardHeader("ATTACK");
        headerImage.type = Image.Type.Sliced;
        headerImage.color = Color.white;

        // Title text (Cinzel Bold, auto-size enabled)
        GameObject titleObj = CreateTextElement("CardTitle", headerBanner.transform, "STRIKE", 18, TextAlignmentOptions.Center, bold: true, autoSize: true, minSize: 13f, maxSize: 18f);
        RectTransform titleRT = titleObj.GetComponent<RectTransform>();
        StretchFull(titleRT);
        titleRT.offsetMin = new Vector2(36, 0); // Leave room for energy gem on the left
        titleRT.offsetMax = new Vector2(-10, 0);
        TextMeshProUGUI titleTMP = titleObj.GetComponent<TextMeshProUGUI>();
        titleTMP.color = new Color(0.98f, 0.94f, 0.82f);

        // 5. Energy Orb (Top-Left corner, overlapping the frame)
        GameObject energyOrb = CreateUIElement("EnergyOrb", card.transform);
        RectTransform orbRT = energyOrb.GetComponent<RectTransform>();
        orbRT.anchorMin = new Vector2(0, 1);
        orbRT.anchorMax = new Vector2(0, 1);
        orbRT.pivot = new Vector2(0.5f, 0.5f);
        orbRT.anchoredPosition = new Vector2(10f, -10f);
        orbRT.sizeDelta = new Vector2(46, 46);
        Image orbImage = energyOrb.AddComponent<Image>();
        orbImage.sprite = UISprites.CostBadgePlayable;
        orbImage.color = Color.white;

        // Energy Orb Number
        GameObject costObj = CreateTextElement("CostText", energyOrb.transform, "1", 24, TextAlignmentOptions.Center, bold: true);
        RectTransform costRT = costObj.GetComponent<RectTransform>();
        StretchFull(costRT);
        TextMeshProUGUI costTMP = costObj.GetComponent<TextMeshProUGUI>();
        costTMP.color = Color.white;

        // 6. Card Type Ribbon
        GameObject typeRibbon = CreateUIElement("TypeRibbon", cardInner.transform);
        RectTransform ribbonRT = typeRibbon.GetComponent<RectTransform>();
        ribbonRT.anchorMin = new Vector2(0.5f, 1f);
        ribbonRT.anchorMax = new Vector2(0.5f, 1f);
        ribbonRT.pivot = new Vector2(0.5f, 1f);
        ribbonRT.anchoredPosition = new Vector2(0f, -268f);
        ribbonRT.sizeDelta = new Vector2(160f, 24f);
        Image ribbonImage = typeRibbon.AddComponent<Image>();
        ribbonImage.sprite = UISprites.GetPill("TypeRibbon", 128, 32, new Color(0.06f, 0.08f, 0.12f, 0.85f), new Color(0.14f, 0.17f, 0.24f, 0.85f), 1f, new Color(0.4f, 0.45f, 0.6f, 0.4f));
        ribbonImage.type = Image.Type.Sliced;
        ribbonImage.color = Color.white;

        GameObject typeTextObj = CreateTextElement("TypeText", typeRibbon.transform, "ATTACK", 12, TextAlignmentOptions.Center, bold: true, autoSize: true, minSize: 10f, maxSize: 14f);
        RectTransform typeTextRT = typeTextObj.GetComponent<RectTransform>();
        StretchFull(typeTextRT);
        TextMeshProUGUI typeTMP = typeTextObj.GetComponent<TextMeshProUGUI>();
        typeTMP.color = new Color(0.85f, 0.82f, 0.72f);

        // 7. Description Box at the Bottom
        GameObject descBox = CreateUIElement("DescriptionBox", cardInner.transform);
        RectTransform descBoxRT = descBox.GetComponent<RectTransform>();
        descBoxRT.anchorMin = new Vector2(0.5f, 1f);
        descBoxRT.anchorMax = new Vector2(0.5f, 1f);
        descBoxRT.pivot = new Vector2(0.5f, 1f);
        descBoxRT.anchoredPosition = new Vector2(0f, -295f);
        descBoxRT.sizeDelta = new Vector2(220f, 153f);
        Image descBoxImage = descBox.AddComponent<Image>();
        descBoxImage.sprite = UISprites.GetRoundedRect("DescBox", 128, 128, 10f, new Color(0.05f, 0.06f, 0.09f, 0.90f), new Color(0.09f, 0.11f, 0.16f, 0.90f), 1f, new Color(0.25f, 0.28f, 0.38f, 0.5f));
        descBoxImage.type = Image.Type.Sliced;
        descBoxImage.color = Color.white;

        GameObject descObj = CreateTextElement("DescText", descBox.transform, "Deal 6 damage.", 16, TextAlignmentOptions.Center, bold: false, autoSize: true, minSize: 12f, maxSize: 16f);
        RectTransform descRT = descObj.GetComponent<RectTransform>();
        StretchFull(descRT);
        descRT.offsetMin = new Vector2(10, 8);
        descRT.offsetMax = new Vector2(-10, -8);
        TextMeshProUGUI descTMP = descObj.GetComponent<TextMeshProUGUI>();
        descTMP.color = new Color(0.92f, 0.92f, 0.95f);

        // 8. Attach CardUI
        CardUI cardUI = card.AddComponent<CardUI>();
        cardUI.cardBorder = outerBorder;
        cardUI.cardBackground = headerImage;
        cardUI.artworkPlaceholder = artImage;
        cardUI.nameText = titleTMP;
        cardUI.costText = costTMP;
        cardUI.typeText = typeTMP;
        cardUI.descriptionText = descTMP;
        cardUI.costCircle = orbImage;

        return card;
    }

    private GameObject BuildPileBadge(string name, Transform parent, Vector2 pos, string label, Color color)
    {
        GameObject badge = CreateUIElement(name, parent);
        RectTransform rt = badge.GetComponent<RectTransform>();
        rt.anchorMin = (pos.x >= 0) ? new Vector2(0, 0) : new Vector2(1, 0);
        rt.anchorMax = rt.anchorMin;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(90, 64);

        Image bg = badge.AddComponent<Image>();
        bg.sprite = (pos.x >= 0) ? UISprites.DrawPileBadge : UISprites.DiscardPileBadge;
        bg.type = Image.Type.Sliced;
        bg.color = Color.white;

        GameObject countObj = CreateTextElement("Count", badge.transform, "10", 22, TextAlignmentOptions.Center, bold: true);
        RectTransform countRT = countObj.GetComponent<RectTransform>();
        countRT.anchorMin = new Vector2(0, 0.35f);
        countRT.anchorMax = new Vector2(1, 1f);
        countRT.offsetMin = Vector2.zero;
        countRT.offsetMax = Vector2.zero;
        TextMeshProUGUI countTMP = countObj.GetComponent<TextMeshProUGUI>();
        countTMP.color = Color.white;

        GameObject lblObj = CreateTextElement("Label", badge.transform, label, 11, TextAlignmentOptions.Center, bold: true);
        RectTransform lblRT = lblObj.GetComponent<RectTransform>();
        lblRT.anchorMin = new Vector2(0, 0);
        lblRT.anchorMax = new Vector2(1, 0.40f);
        lblRT.offsetMin = Vector2.zero;
        lblRT.offsetMax = Vector2.zero;
        TextMeshProUGUI lblTMP = lblObj.GetComponent<TextMeshProUGUI>();
        lblTMP.color = new Color(0.92f, 0.90f, 0.82f);

        return badge;
    }

    private GameObject BuildPopupPrefab()
    {
        GameObject popup = CreateUIElement("PopupPrefab", mainCanvas.transform);
        RectTransform popupRT = popup.GetComponent<RectTransform>();
        popupRT.sizeDelta = new Vector2(180, 45);

        GameObject textObj = CreateTextElement("PopupText", popup.transform, "-6", 28, TextAlignmentOptions.Center, bold: true);
        RectTransform textRT = textObj.GetComponent<RectTransform>();
        StretchFull(textRT);
        TextMeshProUGUI textTMP = textObj.GetComponent<TextMeshProUGUI>();
        textTMP.color = new Color(0.98f, 0.35f, 0.35f);

        return popup;
    }

    private GameObject BuildGameOverPanel(string name, Transform parent, string title, string subtitle, Color accentColor)
    {
        GameObject panel = CreateUIElement(name, parent);
        RectTransform panelRT = panel.GetComponent<RectTransform>();
        StretchFull(panelRT);

        Image overlay = panel.AddComponent<Image>();
        overlay.color = new Color(0, 0, 0, 0.82f);

        GameObject box = CreateUIElement("DialogBox", panel.transform);
        RectTransform boxRT = box.GetComponent<RectTransform>();
        boxRT.anchorMin = new Vector2(0.5f, 0.5f);
        boxRT.anchorMax = new Vector2(0.5f, 0.5f);
        boxRT.pivot = new Vector2(0.5f, 0.5f);
        boxRT.sizeDelta = new Vector2(520, 330);

        // Soft drop shadow under dialog box
        GameObject shadowObj = CreateUIElement("DialogShadow", box.transform);
        RectTransform shadowRT = shadowObj.GetComponent<RectTransform>();
        StretchFull(shadowRT);
        shadowRT.offsetMin = new Vector2(-16, -20);
        shadowRT.offsetMax = new Vector2(16, 12);
        Image shadowImg = shadowObj.AddComponent<Image>();
        shadowImg.sprite = UISprites.SoftShadow;
        shadowImg.type = Image.Type.Sliced;
        shadowImg.color = Color.white;

        Image boxBg = box.AddComponent<Image>();
        boxBg.sprite = UISprites.GetRoundedRect("DialogBox", 128, 128, 22f,
            new Color(0.06f, 0.08f, 0.12f, 0.98f),
            new Color(0.12f, 0.15f, 0.22f, 0.98f),
            2.5f, new Color(0.88f, 0.74f, 0.38f, 0.90f));
        boxBg.type = Image.Type.Sliced;
        boxBg.color = Color.white;

        GameObject titleObj = CreateTextElement("Title", box.transform, title, 38, TextAlignmentOptions.Center, bold: true);
        RectTransform titleRT = titleObj.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0.05f, 0.65f);
        titleRT.anchorMax = new Vector2(0.95f, 0.92f);
        titleRT.offsetMin = Vector2.zero;
        titleRT.offsetMax = Vector2.zero;
        TextMeshProUGUI titleTMP = titleObj.GetComponent<TextMeshProUGUI>();
        titleTMP.color = accentColor;

        GameObject subObj = CreateTextElement("Subtitle", box.transform, subtitle, 18, TextAlignmentOptions.Center, bold: false);
        RectTransform subRT = subObj.GetComponent<RectTransform>();
        subRT.anchorMin = new Vector2(0.05f, 0.40f);
        subRT.anchorMax = new Vector2(0.95f, 0.65f);
        subRT.offsetMin = Vector2.zero;
        subRT.offsetMax = Vector2.zero;
        subObj.GetComponent<TextMeshProUGUI>().color = new Color(0.85f, 0.88f, 0.92f);

        GameObject restartBtn = CreateButton("RestartButton", box.transform, "PLAY AGAIN",
            new Color(0.20f, 0.55f, 0.30f), Color.white, UISprites.PlayAgainButton);
        RectTransform btnRT = restartBtn.GetComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(0.25f, 0.10f);
        btnRT.anchorMax = new Vector2(0.75f, 0.32f);
        btnRT.offsetMin = Vector2.zero;
        btnRT.offsetMax = Vector2.zero;

        panel.SetActive(false);
        return panel;
    }

    private GameObject BuildTurnBanner(Transform parent)
    {
        GameObject banner = CreateUIElement("YourTurnBanner", parent);
        RectTransform rt = banner.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(1f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(2200f, 35f);
        rt.sizeDelta = new Vector2(0f, 130f);

        Image bg = banner.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.07f, 0.11f, 0.94f);
        bg.raycastTarget = false;

        // Top Gold Trim Line (Soft fade divider line)
        GameObject topTrim = CreateUIElement("TopTrim", banner.transform);
        RectTransform topRT = topTrim.GetComponent<RectTransform>();
        topRT.anchorMin = new Vector2(0f, 1f);
        topRT.anchorMax = new Vector2(1f, 1f);
        topRT.pivot = new Vector2(0.5f, 1f);
        topRT.anchoredPosition = Vector2.zero;
        topRT.sizeDelta = new Vector2(0f, 4f);
        Image topImg = topTrim.AddComponent<Image>();
        topImg.sprite = UISprites.DividerLine;
        topImg.color = Color.white;
        topImg.raycastTarget = false;

        // Bottom Gold Trim Line
        GameObject botTrim = CreateUIElement("BottomTrim", banner.transform);
        RectTransform botRT = botTrim.GetComponent<RectTransform>();
        botRT.anchorMin = new Vector2(0f, 0f);
        botRT.anchorMax = new Vector2(1f, 0f);
        botRT.pivot = new Vector2(0.5f, 0f);
        botRT.anchoredPosition = Vector2.zero;
        botRT.sizeDelta = new Vector2(0f, 4f);
        Image botImg = botTrim.AddComponent<Image>();
        botImg.sprite = UISprites.DividerLine;
        botImg.color = Color.white;
        botImg.raycastTarget = false;

        // Big, Bold Text: "YOUR TURN"
        GameObject textObj = CreateTextElement("BannerText", banner.transform,
            "<size=48><color=#ffe066><b>YOUR TURN</b></color></size>\n<size=17><color=#d0d8e8>— CHOOSE YOUR ACTIONS —</color></size>",
            30, TextAlignmentOptions.Center, bold: true);
        RectTransform textRT = textObj.GetComponent<RectTransform>();
        StretchFull(textRT);

        banner.SetActive(false);
        return banner;
    }

    // ─── UTILITY HELPERS ───

    private static TMP_FontAsset defaultFontAsset;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetStatics()
    {
        defaultFontAsset = null;
    }

    public static TMP_FontAsset GetDefaultFont()
    {
        if (defaultFontAsset == null)
        {
            defaultFontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
#if UNITY_EDITOR
            if (defaultFontAsset == null)
            {
                defaultFontAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                    "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            }
#endif
        }
        return defaultFontAsset;
    }

    private GameObject CreateUIElement(string name, Transform parent)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<RectTransform>();
        return obj;
    }

    private GameObject CreateTextElement(string name, Transform parent, string text, int fontSize, TextAlignmentOptions alignment, bool bold = false, bool autoSize = false, float minSize = 10f, float maxSize = 24f)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<RectTransform>();

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        if (bold)
            UIFonts.ApplyTitle(tmp, autoSize, minSize, maxSize > 0 ? maxSize : fontSize);
        else
            UIFonts.ApplyBody(tmp, true, autoSize, minSize, maxSize > 0 ? maxSize : fontSize);

        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.overflowMode = TextOverflowModes.Truncate;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.extraPadding = true;
        tmp.raycastTarget = false;
        tmp.isOrthographic = true;

        return obj;
    }

    private GameObject CreateButton(string name, Transform parent, string text, Color bgColor, Color textColor, Sprite customSprite = null)
    {
        GameObject btnObj = CreateUIElement(name, parent);
        Image btnImage = btnObj.AddComponent<Image>();
        if (customSprite != null)
        {
            btnImage.sprite = customSprite;
            btnImage.type = Image.Type.Sliced;
            btnImage.color = Color.white;
        }
        else
        {
            btnImage.sprite = UISprites.GetPill("DefaultButton", 140, 50, bgColor * 0.8f, bgColor, 2f, new Color(1f, 1f, 1f, 0.4f));
            btnImage.type = Image.Type.Sliced;
            btnImage.color = Color.white;
        }

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
        colors.pressedColor = new Color(0.85f, 0.85f, 0.85f);
        btn.colors = colors;

        // Smooth hover and press scale effect
        btnObj.AddComponent<UIButtonEffect>();

        GameObject btnText = CreateTextElement("Text", btnObj.transform, text, 18, TextAlignmentOptions.Center, bold: true);
        RectTransform textRT = btnText.GetComponent<RectTransform>();
        StretchFull(textRT);
        TextMeshProUGUI tmp = btnText.GetComponent<TextMeshProUGUI>();
        tmp.color = textColor;
        tmp.raycastTarget = false;

        return btnObj;
    }

    private void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
