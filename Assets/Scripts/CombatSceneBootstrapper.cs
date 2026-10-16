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
        scaler.referenceResolution = new Vector2(1600, 900);
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

        // ─── BACKGROUND (Dark atmospheric fantasy arena) ───
        GameObject bgObj = CreateUIElement("ArenaBackground", canvasObj.transform);
        RectTransform bgRT = bgObj.GetComponent<RectTransform>();
        StretchFull(bgRT);
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0.09f, 0.09f, 0.13f, 1f); // Deep dungeon slate

        // ─── BATTLEFIELD ARENA (Top 72% of screen) ───
        GameObject arenaArea = CreateUIElement("BattlefieldArea", canvasObj.transform);
        RectTransform arenaRT = arenaArea.GetComponent<RectTransform>();
        arenaRT.anchorMin = new Vector2(0, 0.28f);
        arenaRT.anchorMax = new Vector2(1, 1f);
        arenaRT.offsetMin = Vector2.zero;
        arenaRT.offsetMax = Vector2.zero;

        // Subtle arena floor line
        GameObject floorLine = CreateUIElement("ArenaFloor", arenaArea.transform);
        RectTransform floorRT = floorLine.GetComponent<RectTransform>();
        floorRT.anchorMin = new Vector2(0.05f, 0.32f);
        floorRT.anchorMax = new Vector2(0.95f, 0.32f);
        floorRT.sizeDelta = new Vector2(0, 4f);
        Image floorImage = floorLine.AddComponent<Image>();
        floorImage.color = new Color(0.20f, 0.20f, 0.28f, 0.45f);

        // ─── SOLDIER (Left side of battlefield) ───
        PlayerUI playerUI = BuildPlayerCharacter(arenaArea.transform, out CharacterCombatVisual playerVisual);

        // ─── ORC (Right side of battlefield) ───
        EnemyUI enemyUI = BuildEnemyCharacter(arenaArea.transform, out CharacterCombatVisual enemyVisual);

        // ─── HAND AREA (Bottom 28% of screen) ───
        GameObject handArea = CreateUIElement("HandArea", canvasObj.transform);
        RectTransform handAreaRT = handArea.GetComponent<RectTransform>();
        handAreaRT.anchorMin = new Vector2(0, 0);
        handAreaRT.anchorMax = new Vector2(1, 0.28f);
        handAreaRT.offsetMin = Vector2.zero;
        handAreaRT.offsetMax = Vector2.zero;

        // Dark hand area backing
        Image handBg = handArea.AddComponent<Image>();
        handBg.color = new Color(0.06f, 0.06f, 0.09f, 0.96f);

        // Top decorative gold rim for hand area
        GameObject handBorder = CreateUIElement("HandTopBorder", handArea.transform);
        RectTransform borderRT = handBorder.GetComponent<RectTransform>();
        borderRT.anchorMin = new Vector2(0, 1f);
        borderRT.anchorMax = new Vector2(1, 1f);
        borderRT.pivot = new Vector2(0.5f, 1f);
        borderRT.sizeDelta = new Vector2(0, 3f);
        Image borderImage = handBorder.AddComponent<Image>();
        borderImage.color = new Color(0.75f, 0.60f, 0.28f, 0.75f); // Worn gold trim

        // ─── HAND CONTAINER (Centered in the middle of the screen!) ───
        GameObject handContainer = CreateUIElement("HandContainer", handArea.transform);
        RectTransform handContRT = handContainer.GetComponent<RectTransform>();
        handContRT.anchorMin = new Vector2(0.5f, 0);
        handContRT.anchorMax = new Vector2(0.5f, 0);
        handContRT.pivot = new Vector2(0.5f, 0);
        handContRT.anchoredPosition = new Vector2(0f, 10f);
        handContRT.sizeDelta = new Vector2(860, 235);

        // ─── TURN INDICATOR (Above Hand) ───
        GameObject turnIndicator = CreateTextElement("TurnIndicator", canvasObj.transform, "YOUR TURN", 28, TextAlignmentOptions.Center);
        RectTransform turnRT = turnIndicator.GetComponent<RectTransform>();
        turnRT.anchorMin = new Vector2(0.5f, 0.29f);
        turnRT.anchorMax = new Vector2(0.5f, 0.29f);
        turnRT.pivot = new Vector2(0.5f, 0f);
        turnRT.anchoredPosition = Vector2.zero;
        turnRT.sizeDelta = new Vector2(300, 36);
        TextMeshProUGUI turnText = turnIndicator.GetComponent<TextMeshProUGUI>();
        turnText.color = new Color(0.95f, 0.85f, 0.50f);
        turnText.fontStyle = FontStyles.Bold;

        // ─── ENERGY ORB (Bottom Left of screen) ───
        GameObject energyOrb = CreateUIElement("EnergyOrb", handArea.transform);
        RectTransform energyRT = energyOrb.GetComponent<RectTransform>();
        energyRT.anchorMin = new Vector2(0, 0.5f);
        energyRT.anchorMax = new Vector2(0, 0.5f);
        energyRT.pivot = new Vector2(0.5f, 0.5f);
        energyRT.anchoredPosition = new Vector2(130f, 0f);
        energyRT.sizeDelta = new Vector2(85, 85);

        Image energyBg = energyOrb.AddComponent<Image>();
        energyBg.color = new Color(0.10f, 0.40f, 0.80f, 1f); // Glowing blue orb

        GameObject energyTextObj = CreateTextElement("EnergyText", energyOrb.transform, "3/3\n<size=12>ENERGY</size>", 26, TextAlignmentOptions.Center);
        RectTransform energyTextRT = energyTextObj.GetComponent<RectTransform>();
        StretchFull(energyTextRT);
        TextMeshProUGUI energyTMP = energyTextObj.GetComponent<TextMeshProUGUI>();
        energyTMP.color = Color.white;
        energyTMP.fontStyle = FontStyles.Bold;
        playerUI.energyText = energyTMP;

        // ─── END TURN BUTTON (Bottom Right of screen) ───
        GameObject endTurnBtn = CreateButton("EndTurnButton", handArea.transform, "END TURN",
            new Color(0.65f, 0.22f, 0.18f), new Color(0.95f, 0.88f, 0.65f));
        RectTransform endTurnRT = endTurnBtn.GetComponent<RectTransform>();
        endTurnRT.anchorMin = new Vector2(1, 0.5f);
        endTurnRT.anchorMax = new Vector2(1, 0.5f);
        endTurnRT.pivot = new Vector2(0.5f, 0.5f);
        endTurnRT.anchoredPosition = new Vector2(-130f, 0f);
        endTurnRT.sizeDelta = new Vector2(145, 52);

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
    }

    // ─── SOLDIER BUILDER ───

    private PlayerUI BuildPlayerCharacter(Transform arenaParent, out CharacterCombatVisual visual)
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

        Image spriteImage = playerRoot.AddComponent<Image>();
        spriteImage.raycastTarget = false;

        visual = playerRoot.AddComponent<CharacterCombatVisual>();
        visual.characterImage = spriteImage;
        visual.rectTransform = rootRT;
        visual.targetIndicator = targetInd;
        visual.baseFacingRight = true;
        visual.SetFacing(true);
        visual.SetHomePosition(new Vector2(-380f, -50f));

#if UNITY_EDITOR
        visual.LoadSpritesFromFolder("Assets/Assets/Soldier with shadows", "Soldier");
#endif

        PlayerUI playerUI = playerRoot.AddComponent<PlayerUI>();
        playerUI.playerSprite = spriteImage;

        // Floating Status Bar beneath Soldier
        BuildPlayerStatusDisplay(playerRoot.transform, playerUI);

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
        statusRT.sizeDelta = new Vector2(180, 46);

        // Name
        GameObject nameObj = CreateTextElement("Name", statusObj.transform, "SOLDIER", 18, TextAlignmentOptions.Center);
        RectTransform nameRT = nameObj.GetComponent<RectTransform>();
        nameRT.anchorMin = new Vector2(0, 0.55f);
        nameRT.anchorMax = new Vector2(1, 1f);
        nameRT.offsetMin = Vector2.zero;
        nameRT.offsetMax = Vector2.zero;
        TextMeshProUGUI nameTMP = nameObj.GetComponent<TextMeshProUGUI>();
        nameTMP.color = new Color(0.92f, 0.85f, 0.65f);
        nameTMP.fontStyle = FontStyles.Bold;
        playerUI.nameText = nameTMP;

        // HP Bar
        BuildHPBar(statusObj.transform, playerUI);
    }

    // ─── ORC BUILDER ───

    private EnemyUI BuildEnemyCharacter(Transform arenaParent, out CharacterCombatVisual visual)
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

        Image spriteImage = enemyRoot.AddComponent<Image>();
        spriteImage.raycastTarget = false;

        visual = enemyRoot.AddComponent<CharacterCombatVisual>();
        visual.characterImage = spriteImage;
        visual.rectTransform = rootRT;
        visual.targetIndicator = targetInd;
        visual.baseFacingRight = false;
        visual.SetFacing(false); // Orc faces left towards soldier
        visual.SetHomePosition(new Vector2(380f, -50f));

#if UNITY_EDITOR
        visual.LoadSpritesFromFolder("Assets/Assets/Orc with shadows", "Orc");
#endif

        EnemyUI enemyUI = enemyRoot.AddComponent<EnemyUI>();
        enemyUI.enemySprite = spriteImage;

        // Floating Status Bar beneath Orc
        BuildEnemyStatusDisplay(enemyRoot.transform, enemyUI);

        // Intent badge floating above Orc
        BuildEnemyIntentBadge(enemyRoot.transform, enemyUI);

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
        statusRT.sizeDelta = new Vector2(180, 46);

        // Name
        GameObject nameObj = CreateTextElement("Name", statusObj.transform, "ORC", 18, TextAlignmentOptions.Center);
        RectTransform nameRT = nameObj.GetComponent<RectTransform>();
        nameRT.anchorMin = new Vector2(0, 0.55f);
        nameRT.anchorMax = new Vector2(1, 1f);
        nameRT.offsetMin = Vector2.zero;
        nameRT.offsetMax = Vector2.zero;
        TextMeshProUGUI nameTMP = nameObj.GetComponent<TextMeshProUGUI>();
        nameTMP.color = new Color(0.95f, 0.70f, 0.65f);
        nameTMP.fontStyle = FontStyles.Bold;
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
        badgeRT.anchoredPosition = new Vector2(0f, 15f);
        badgeRT.sizeDelta = new Vector2(110, 42);

        Image badgeBg = intentBadge.AddComponent<Image>();
        badgeBg.color = new Color(0.18f, 0.12f, 0.14f, 0.90f);

        GameObject intentTextObj = CreateTextElement("IntentText", intentBadge.transform, "ATK 8", 20, TextAlignmentOptions.Center);
        RectTransform intentTextRT = intentTextObj.GetComponent<RectTransform>();
        StretchFull(intentTextRT);
        TextMeshProUGUI intentTMP = intentTextObj.GetComponent<TextMeshProUGUI>();
        intentTMP.color = new Color(1f, 0.40f, 0.40f);
        intentTMP.fontStyle = FontStyles.Bold;

        enemyUI.intentContainer = intentBadge;
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

        // Bar background
        Image bgImg = barRoot.AddComponent<Image>();
        bgImg.color = new Color(0.12f, 0.12f, 0.16f, 1f);

        // Fill area
        GameObject fillArea = CreateUIElement("FillArea", barRoot.transform);
        RectTransform fillRT = fillArea.GetComponent<RectTransform>();
        StretchFull(fillRT);

        GameObject fillObj = CreateUIElement("Fill", fillArea.transform);
        RectTransform fillImgRT = fillObj.GetComponent<RectTransform>();
        StretchFull(fillImgRT);
        Image fillImg = fillObj.AddComponent<Image>();
        fillImg.color = new Color(0.25f, 0.80f, 0.35f, 1f);

        Slider slider = barRoot.AddComponent<Slider>();
        slider.fillRect = fillImgRT;
        slider.minValue = 0;
        slider.maxValue = 60;
        slider.value = 60;
        slider.interactable = false;

        GameObject hpTextObj = CreateTextElement("HPText", barRoot.transform, "60/60", 14, TextAlignmentOptions.Center);
        RectTransform textRT = hpTextObj.GetComponent<RectTransform>();
        StretchFull(textRT);
        TextMeshProUGUI hpTMP = hpTextObj.GetComponent<TextMeshProUGUI>();
        hpTMP.color = Color.white;
        hpTMP.fontStyle = FontStyles.Bold;

        // Block badge
        GameObject blockBadge = CreateUIElement("BlockBadge", barRoot.transform);
        RectTransform blockRT = blockBadge.GetComponent<RectTransform>();
        blockRT.anchorMin = new Vector2(0, 0.5f);
        blockRT.anchorMax = new Vector2(0, 0.5f);
        blockRT.pivot = new Vector2(1f, 0.5f);
        blockRT.anchoredPosition = new Vector2(-6f, 0f);
        blockRT.sizeDelta = new Vector2(32, 32);

        Image blockBg = blockBadge.AddComponent<Image>();
        blockBg.color = new Color(0.18f, 0.45f, 0.85f, 1f);

        GameObject blockTextObj = CreateTextElement("BlockText", blockBadge.transform, "0", 14, TextAlignmentOptions.Center);
        RectTransform btRT = blockTextObj.GetComponent<RectTransform>();
        StretchFull(btRT);
        TextMeshProUGUI blockTMP = blockTextObj.GetComponent<TextMeshProUGUI>();
        blockTMP.color = Color.white;
        blockTMP.fontStyle = FontStyles.Bold;

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
        bgImg.color = new Color(0.12f, 0.12f, 0.16f, 1f);

        GameObject fillArea = CreateUIElement("FillArea", barRoot.transform);
        RectTransform fillRT = fillArea.GetComponent<RectTransform>();
        StretchFull(fillRT);

        GameObject fillObj = CreateUIElement("Fill", fillArea.transform);
        RectTransform fillImgRT = fillObj.GetComponent<RectTransform>();
        StretchFull(fillImgRT);
        Image fillImg = fillObj.AddComponent<Image>();
        fillImg.color = new Color(0.85f, 0.25f, 0.25f, 1f);

        Slider slider = barRoot.AddComponent<Slider>();
        slider.fillRect = fillImgRT;
        slider.minValue = 0;
        slider.maxValue = 50;
        slider.value = 50;
        slider.interactable = false;

        GameObject hpTextObj = CreateTextElement("HPText", barRoot.transform, "50/50", 14, TextAlignmentOptions.Center);
        RectTransform textRT = hpTextObj.GetComponent<RectTransform>();
        StretchFull(textRT);
        TextMeshProUGUI hpTMP = hpTextObj.GetComponent<TextMeshProUGUI>();
        hpTMP.color = Color.white;
        hpTMP.fontStyle = FontStyles.Bold;

        // Block badge
        GameObject blockBadge = CreateUIElement("BlockBadge", barRoot.transform);
        RectTransform blockRT = blockBadge.GetComponent<RectTransform>();
        blockRT.anchorMin = new Vector2(1, 0.5f);
        blockRT.anchorMax = new Vector2(1, 0.5f);
        blockRT.pivot = new Vector2(0f, 0.5f);
        blockRT.anchoredPosition = new Vector2(6f, 0f);
        blockRT.sizeDelta = new Vector2(32, 32);

        Image blockBg = blockBadge.AddComponent<Image>();
        blockBg.color = new Color(0.18f, 0.45f, 0.85f, 1f);

        GameObject blockTextObj = CreateTextElement("BlockText", blockBadge.transform, "0", 14, TextAlignmentOptions.Center);
        RectTransform btRT = blockTextObj.GetComponent<RectTransform>();
        StretchFull(btRT);
        TextMeshProUGUI blockTMP = blockTextObj.GetComponent<TextMeshProUGUI>();
        blockTMP.color = Color.white;
        blockTMP.fontStyle = FontStyles.Bold;

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
        cardRT.sizeDelta = new Vector2(155, 225);
        cardRT.pivot = new Vector2(0.5f, 0.5f);

        // 1. Outer Dark Illustrated Fantasy Frame Border
        Image outerBorder = card.AddComponent<Image>();
        outerBorder.color = new Color(0.92f, 0.78f, 0.35f, 1f); // Metallic Gold trim

        // 2. Inner Dark Card Stock
        GameObject cardInner = CreateUIElement("CardInner", card.transform);
        RectTransform innerRT = cardInner.GetComponent<RectTransform>();
        innerRT.anchorMin = Vector2.zero;
        innerRT.anchorMax = Vector2.one;
        innerRT.offsetMin = new Vector2(3, 3);
        innerRT.offsetMax = new Vector2(-3, -3);
        Image innerImage = cardInner.AddComponent<Image>();
        innerImage.color = new Color(0.12f, 0.11f, 0.15f, 1f); // Dark worn slate

        // 3. Top Banner (Title Bar)
        GameObject headerBanner = CreateUIElement("HeaderBanner", cardInner.transform);
        RectTransform headerRT = headerBanner.GetComponent<RectTransform>();
        headerRT.anchorMin = new Vector2(0, 0.84f);
        headerRT.anchorMax = new Vector2(1, 1f);
        headerRT.offsetMin = new Vector2(4, 2);
        headerRT.offsetMax = new Vector2(-4, -3);
        Image headerImage = headerBanner.AddComponent<Image>();
        headerImage.color = new Color(0.35f, 0.12f, 0.14f, 1f);

        // Title text (prominent, bold, readable)
        GameObject titleObj = CreateTextElement("CardTitle", headerBanner.transform, "STRIKE", 16, TextAlignmentOptions.Center);
        RectTransform titleRT = titleObj.GetComponent<RectTransform>();
        StretchFull(titleRT);
        TextMeshProUGUI titleTMP = titleObj.GetComponent<TextMeshProUGUI>();
        titleTMP.color = new Color(0.96f, 0.92f, 0.82f);
        titleTMP.fontStyle = FontStyles.Bold;

        // 4. Energy Orb (Top-Left corner, overlapping the frame)
        GameObject energyOrb = CreateUIElement("EnergyOrb", card.transform);
        RectTransform orbRT = energyOrb.GetComponent<RectTransform>();
        orbRT.anchorMin = new Vector2(0, 1);
        orbRT.anchorMax = new Vector2(0, 1);
        orbRT.pivot = new Vector2(0.5f, 0.5f);
        orbRT.anchoredPosition = new Vector2(8f, -8f);
        orbRT.sizeDelta = new Vector2(34, 34);
        Image orbImage = energyOrb.AddComponent<Image>();
        orbImage.color = new Color(0.12f, 0.55f, 0.95f, 1f); // Electric Blue Gem

        // Energy Orb Number
        GameObject costObj = CreateTextElement("CostText", energyOrb.transform, "1", 20, TextAlignmentOptions.Center);
        RectTransform costRT = costObj.GetComponent<RectTransform>();
        StretchFull(costRT);
        TextMeshProUGUI costTMP = costObj.GetComponent<TextMeshProUGUI>();
        costTMP.color = Color.white;
        costTMP.fontStyle = FontStyles.Bold;

        // 5. Artwork Box in the Center (Recessed, no images, user can add images later)
        GameObject artFrame = CreateUIElement("ArtworkFrame", cardInner.transform);
        RectTransform artFrameRT = artFrame.GetComponent<RectTransform>();
        artFrameRT.anchorMin = new Vector2(0.04f, 0.44f);
        artFrameRT.anchorMax = new Vector2(0.96f, 0.83f);
        artFrameRT.offsetMin = Vector2.zero;
        artFrameRT.offsetMax = Vector2.zero;
        Image artFrameImage = artFrame.AddComponent<Image>();
        artFrameImage.color = new Color(0.24f, 0.26f, 0.32f, 1f);

        GameObject artArea = CreateUIElement("ArtworkPlaceholder", artFrame.transform);
        RectTransform artRT = artArea.GetComponent<RectTransform>();
        artRT.anchorMin = Vector2.zero;
        artRT.anchorMax = Vector2.one;
        artRT.offsetMin = new Vector2(2, 2);
        artRT.offsetMax = new Vector2(-2, -2);
        Image artImage = artArea.AddComponent<Image>();
        artImage.color = new Color(0.14f, 0.15f, 0.19f, 1f);

        // Placeholder watermark
        GameObject artWatermark = CreateTextElement("ArtLabel", artArea.transform,
            "<size=12><color=#606880>— ARTWORK —</color></size>",
            12, TextAlignmentOptions.Center);
        RectTransform wmRT = artWatermark.GetComponent<RectTransform>();
        StretchFull(wmRT);

        // 6. Card Type Ribbon
        GameObject typeRibbon = CreateUIElement("TypeRibbon", cardInner.transform);
        RectTransform ribbonRT = typeRibbon.GetComponent<RectTransform>();
        ribbonRT.anchorMin = new Vector2(0.08f, 0.37f);
        ribbonRT.anchorMax = new Vector2(0.92f, 0.43f);
        ribbonRT.offsetMin = Vector2.zero;
        ribbonRT.offsetMax = Vector2.zero;
        Image ribbonImage = typeRibbon.AddComponent<Image>();
        ribbonImage.color = new Color(0.18f, 0.19f, 0.24f, 0.85f);

        GameObject typeTextObj = CreateTextElement("TypeText", typeRibbon.transform, "ATTACK", 12, TextAlignmentOptions.Center);
        RectTransform typeTextRT = typeTextObj.GetComponent<RectTransform>();
        StretchFull(typeTextRT);
        TextMeshProUGUI typeTMP = typeTextObj.GetComponent<TextMeshProUGUI>();
        typeTMP.color = new Color(0.75f, 0.72f, 0.65f);
        typeTMP.fontStyle = FontStyles.Bold;

        // 7. Description Box at the Bottom
        GameObject descBox = CreateUIElement("DescriptionBox", cardInner.transform);
        RectTransform descBoxRT = descBox.GetComponent<RectTransform>();
        descBoxRT.anchorMin = new Vector2(0.04f, 0.03f);
        descBoxRT.anchorMax = new Vector2(0.96f, 0.35f);
        descBoxRT.offsetMin = Vector2.zero;
        descBoxRT.offsetMax = Vector2.zero;
        Image descBoxImage = descBox.AddComponent<Image>();
        descBoxImage.color = new Color(0.09f, 0.10f, 0.13f, 0.95f);

        GameObject descObj = CreateTextElement("DescText", descBox.transform, "Deal 6 damage.", 16, TextAlignmentOptions.Center);
        RectTransform descRT = descObj.GetComponent<RectTransform>();
        descRT.anchorMin = Vector2.zero;
        descRT.anchorMax = Vector2.one;
        descRT.offsetMin = new Vector2(4, 4);
        descRT.offsetMax = new Vector2(-4, -4);
        TextMeshProUGUI descTMP = descObj.GetComponent<TextMeshProUGUI>();
        descTMP.color = new Color(0.90f, 0.90f, 0.92f);
        descTMP.textWrappingMode = TextWrappingModes.Normal;

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
        rt.pivot = (pos.x >= 0) ? new Vector2(0.5f, 0.5f) : new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(80, 56);

        Image bg = badge.AddComponent<Image>();
        bg.color = color;

        GameObject countObj = CreateTextElement("Count", badge.transform, "10", 20, TextAlignmentOptions.Center);
        RectTransform countRT = countObj.GetComponent<RectTransform>();
        countRT.anchorMin = new Vector2(0, 0.35f);
        countRT.anchorMax = new Vector2(1, 1f);
        countRT.offsetMin = Vector2.zero;
        countRT.offsetMax = Vector2.zero;
        TextMeshProUGUI countTMP = countObj.GetComponent<TextMeshProUGUI>();
        countTMP.color = Color.white;
        countTMP.fontStyle = FontStyles.Bold;

        GameObject lblObj = CreateTextElement("Label", badge.transform, label, 11, TextAlignmentOptions.Center);
        RectTransform lblRT = lblObj.GetComponent<RectTransform>();
        lblRT.anchorMin = new Vector2(0, 0);
        lblRT.anchorMax = new Vector2(1, 0.40f);
        lblRT.offsetMin = Vector2.zero;
        lblRT.offsetMax = Vector2.zero;
        TextMeshProUGUI lblTMP = lblObj.GetComponent<TextMeshProUGUI>();
        lblTMP.color = new Color(0.85f, 0.85f, 0.90f);

        return badge;
    }

    private GameObject BuildPopupPrefab()
    {
        GameObject popup = CreateUIElement("PopupPrefab", mainCanvas.transform);
        RectTransform popupRT = popup.GetComponent<RectTransform>();
        popupRT.sizeDelta = new Vector2(180, 45);

        GameObject textObj = CreateTextElement("PopupText", popup.transform, "-6", 28, TextAlignmentOptions.Center);
        RectTransform textRT = textObj.GetComponent<RectTransform>();
        StretchFull(textRT);
        TextMeshProUGUI textTMP = textObj.GetComponent<TextMeshProUGUI>();
        textTMP.fontStyle = FontStyles.Bold;
        textTMP.color = new Color(0.95f, 0.25f, 0.25f);

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
        boxRT.sizeDelta = new Vector2(500, 320);

        Image boxBg = box.AddComponent<Image>();
        boxBg.color = new Color(0.12f, 0.12f, 0.16f, 0.98f);

        GameObject titleObj = CreateTextElement("Title", box.transform, title, 44, TextAlignmentOptions.Center);
        RectTransform titleRT = titleObj.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0.05f, 0.65f);
        titleRT.anchorMax = new Vector2(0.95f, 0.92f);
        titleRT.offsetMin = Vector2.zero;
        titleRT.offsetMax = Vector2.zero;
        TextMeshProUGUI titleTMP = titleObj.GetComponent<TextMeshProUGUI>();
        titleTMP.color = accentColor;
        titleTMP.fontStyle = FontStyles.Bold;

        GameObject subObj = CreateTextElement("Subtitle", box.transform, subtitle, 20, TextAlignmentOptions.Center);
        RectTransform subRT = subObj.GetComponent<RectTransform>();
        subRT.anchorMin = new Vector2(0.05f, 0.40f);
        subRT.anchorMax = new Vector2(0.95f, 0.65f);
        subRT.offsetMin = Vector2.zero;
        subRT.offsetMax = Vector2.zero;
        subObj.GetComponent<TextMeshProUGUI>().color = new Color(0.85f, 0.85f, 0.90f);

        GameObject restartBtn = CreateButton("RestartButton", box.transform, "PLAY AGAIN",
            new Color(0.20f, 0.55f, 0.30f), Color.white);
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
        bg.color = new Color(0.06f, 0.08f, 0.12f, 0.94f);
        bg.raycastTarget = false;

        // Top Gold Trim Line
        GameObject topTrim = CreateUIElement("TopTrim", banner.transform);
        RectTransform topRT = topTrim.GetComponent<RectTransform>();
        topRT.anchorMin = new Vector2(0f, 1f);
        topRT.anchorMax = new Vector2(1f, 1f);
        topRT.pivot = new Vector2(0.5f, 1f);
        topRT.anchoredPosition = Vector2.zero;
        topRT.sizeDelta = new Vector2(0f, 4f);
        Image topImg = topTrim.AddComponent<Image>();
        topImg.color = new Color(0.95f, 0.82f, 0.35f, 1f);
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
        botImg.color = new Color(0.95f, 0.82f, 0.35f, 1f);
        botImg.raycastTarget = false;

        // Big, Bold Text: "YOUR TURN"
        GameObject textObj = CreateTextElement("BannerText", banner.transform,
            "<size=52><color=#ffe066><b>YOUR TURN</b></color></size>\n<size=18><color=#b0c4de>— CHOOSE YOUR ACTIONS —</color></size>",
            32, TextAlignmentOptions.Center);
        RectTransform textRT = textObj.GetComponent<RectTransform>();
        StretchFull(textRT);

        banner.SetActive(false);
        return banner;
    }

    // ─── UTILITY HELPERS ───

    private static TMP_FontAsset defaultFontAsset;
    private static Material sharpFontMaterial;

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

    /// <summary>
    /// Returns a material instance with boosted _Sharpness for crisper SDF text edges.
    /// </summary>
    public static Material GetSharpFontMaterial()
    {
        if (sharpFontMaterial == null)
        {
            TMP_FontAsset font = GetDefaultFont();
            if (font != null && font.material != null)
            {
                sharpFontMaterial = new Material(font.material);
                sharpFontMaterial.name = "LiberationSans SDF Sharp (Instance)";
                sharpFontMaterial.SetFloat("_Sharpness", 0.5f);
            }
        }
        return sharpFontMaterial;
    }

    private GameObject CreateUIElement(string name, Transform parent)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<RectTransform>();
        return obj;
    }

    private GameObject CreateTextElement(string name, Transform parent, string text, int fontSize, TextAlignmentOptions alignment)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<RectTransform>();

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        TMP_FontAsset font = GetDefaultFont();
        if (font != null)
        {
            tmp.font = font;
            Material sharpMat = GetSharpFontMaterial();
            if (sharpMat != null)
                tmp.fontMaterial = sharpMat;
            else
                tmp.fontSharedMaterial = font.material;
        }

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

    private GameObject CreateButton(string name, Transform parent, string text, Color bgColor, Color textColor)
    {
        GameObject btnObj = CreateUIElement(name, parent);
        Image btnImage = btnObj.AddComponent<Image>();
        btnImage.color = bgColor;

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
        btn.colors = colors;

        GameObject btnText = CreateTextElement("Text", btnObj.transform, text, 20, TextAlignmentOptions.Center);
        RectTransform textRT = btnText.GetComponent<RectTransform>();
        StretchFull(textRT);
        TextMeshProUGUI tmp = btnText.GetComponent<TextMeshProUGUI>();
        tmp.color = textColor;
        tmp.fontStyle = FontStyles.Bold;
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
