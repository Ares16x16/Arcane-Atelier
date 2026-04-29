using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using ArcaneAtelier.Battle;
using ArcaneAtelier.Workshop;

public class BattleUIPresenter : MonoBehaviour
{
    private const string WorkshopSceneName = "WorkshopScene";
    private const string MainMenuSceneName = "MainMenuScene";
    private const float Margin = 20f;
    private const float TopHudHeight = 94f;
    private const float BottomDockHeight = 236f;
    private const float PlayerHudWidth = 278f;
    private const float ControlHudWidth = 216f;

    public BattleSceneController controller;

    [Header("Boss UI")]
    public TextMeshProUGUI bossNameText;
    public TextMeshProUGUI bossHealthValueText; // For "1200 / 1200"
    public Slider bossHealthSlider;

    [Header("Player UI")]
    public TextMeshProUGUI playerNameText;
    public TextMeshProUGUI playerHealthValueText; // For "100 / 100"
    public Slider playerHealthSlider;

    [Header("Results Panel UI")]
    public GameObject resultsPanel;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI bossNameResultText;
    public TextMeshProUGUI turnsText;
    public TextMeshProUGUI damageText;
    public TextMeshProUGUI rewardText;

    [Header("Card Hand")]
    public Transform handContentParent; // Drag the 'Content' object here
    public GameObject cardPrefab;

    private Canvas rootCanvas;
    private RectTransform canvasRect;
    private RectTransform playerPanelRect;
    private RectTransform bossPanelRect;
    private RectTransform handPanelRect;
    private RectTransform handViewportRect;
    private RectTransform handScrollRect;
    private TextMeshProUGUI turnText;
    private TextMeshProUGUI energyText;
    private TextMeshProUGUI intentText;
    private TextMeshProUGUI statusText;
    private TextMeshProUGUI drawPileText;
    private TextMeshProUGUI discardPileText;
    private TextMeshProUGUI playerShieldText;
    private TextMeshProUGUI bossShieldText;
    private TextMeshProUGUI handHeaderText;
    private Button endTurnButton;
    private Image endTurnButtonImage;
    private GameObject runSummaryPanel;
    private TextMeshProUGUI runSummaryTitleText;
    private TextMeshProUGUI runSummaryBodyText;
    private Button returnToMenuButton;
    private bool layoutReady;
    private int knownHandVersion = -1;

    private readonly List<RuntimeCardView> runtimeCards = new List<RuntimeCardView>();

    private sealed class RuntimeCardView
    {
        public Button Button;
        public Image Background;
        public Color BaseColor;
    }

    private void Awake()
    {
        if (controller == null)
        {
            controller = GetComponent<BattleSceneController>();
        }

        EnsureLayout();
    }

    void Start()
    {
        if (resultsPanel != null)
        {
            resultsPanel.SetActive(false);
        }
        if (runSummaryPanel != null)
        {
            runSummaryPanel.SetActive(false);
        }

        UpdateUI();
        PopulateHand();
    }

    void Update()
    {
        EnsureLayout();
        UpdateUI();
        if (controller != null && knownHandVersion != controller.HandVersion)
        {
            PopulateHand();
        }

        UpdateCardInteractivity();
    }

    public void PopulateHand()
    {
        EnsureLayout();

        if (handContentParent == null)
        {
            Debug.LogError("BattleUIPresenter: handContentParent is not assigned in the Inspector!");
            return;
        }

        foreach (Transform child in handContentParent)
        {
            Destroy(child.gameObject);
        }

        runtimeCards.Clear();

        if (controller == null || controller.Cards == null)
        {
            return;
        }

        for (int index = 0; index < controller.Cards.Count; index++)
        {
            var cardEntry = controller.Cards[index];
            RuntimeCardView view = CreateRuntimeCard(cardEntry, index);
            runtimeCards.Add(view);
        }

        knownHandVersion = controller.HandVersion;
        LayoutRebuilder.ForceRebuildLayoutImmediate(handContentParent as RectTransform);
        UpdateCardInteractivity();
    }

    public void UpdateUI()
    {
        if (controller == null || controller.Player == null || controller.Boss == null) return;

        // Update Boss
        if (bossNameText != null)
        {
            bossNameText.text = controller.Boss.DisplayName;
        }
        if (bossHealthSlider != null)
        {
            bossHealthSlider.maxValue = controller.Boss.MaxHealth;
            bossHealthSlider.value = controller.Boss.CurrentHealth;
        }
        if (bossHealthValueText != null)
        {
            bossHealthValueText.text = $"{controller.Boss.CurrentHealth} / {controller.Boss.MaxHealth}";
        }
        if (bossShieldText != null)
        {
            bossShieldText.text = $"Shield {controller.Boss.Shield}";
        }

        // Update Player
        if (playerNameText != null)
        {
            playerNameText.text = controller.Player.DisplayName;
            playerNameText.gameObject.SetActive(!string.IsNullOrWhiteSpace(controller.Player.DisplayName));
        }
        if (playerHealthSlider != null)
        {
            playerHealthSlider.maxValue = controller.Player.MaxHealth;
            playerHealthSlider.value = controller.Player.CurrentHealth;
        }
        if (playerHealthValueText != null)
        {
            playerHealthValueText.text = $"{controller.Player.CurrentHealth} / {controller.Player.MaxHealth}";
        }
        if (turnText != null)
        {
            turnText.text = $"Turn {controller.TurnNumber}";
        }
        if (energyText != null)
        {
            energyText.text = $"Energy {controller.CurrentEnergy}/{controller.EnergyPerTurn}";
        }
        if (playerShieldText != null)
        {
            playerShieldText.text = $"Shield {controller.Player.Shield}";
        }
        if (intentText != null)
        {
            intentText.text = $"Intent: {controller.CurrentBossIntent}";
        }
        if (statusText != null)
        {
            statusText.text = $"{controller.EncounterLabel}\n{controller.LastActionMessage}";
        }
        if (drawPileText != null)
        {
            drawPileText.text = $"Deck {controller.DrawPileCount}";
        }
        if (discardPileText != null)
        {
            discardPileText.text = $"Spent {controller.DiscardPileCount}";
        }
        if (handHeaderText != null)
        {
            handHeaderText.text = $"Cards In Hand {controller.Cards.Count}";
        }
        if (endTurnButton != null)
        {
            endTurnButton.interactable = !controller.BattleEnded;
        }
        if (endTurnButtonImage != null)
        {
            endTurnButtonImage.color = controller.BattleEnded
                ? new Color(0.32f, 0.29f, 0.27f, 0.95f)
                : new Color(0.79f, 0.62f, 0.24f, 0.96f);
        }

    }

    private void EnsureLayout()
    {
        if (layoutReady)
        {
            return;
        }

        rootCanvas = Object.FindAnyObjectByType<Canvas>();
        if (rootCanvas == null)
        {
            return;
        }

        canvasRect = rootCanvas.transform as RectTransform;
        CanvasScaler scaler = rootCanvas.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        ResolveSceneReferences();
        ConfigureBackdrop();
        ConfigurePlayerPanel();
        ConfigureBossPanel();
        ConfigureHandPanel();
        ConfigureControls();
        ConfigureResultsPanel();
        ConfigureRunSummaryPanel();
        layoutReady = true;
    }

    private void ResolveSceneReferences()
    {
        if (bossNameText == null)
        {
            bossNameText = FindText("BossName");
        }

        if (bossHealthValueText == null)
        {
            bossHealthValueText = FindText("BossHealthNumber");
        }

        if (playerNameText == null)
        {
            playerNameText = FindText("PlayerName");
        }

        if (playerHealthValueText == null)
        {
            playerHealthValueText = FindText("PlayerHealthNumber");
        }

        if (titleText == null)
        {
            if (resultsPanel != null)
            {
                TextMeshProUGUI[] texts = resultsPanel.GetComponentsInChildren<TextMeshProUGUI>(true);
                titleText = texts.Length > 0 ? texts[0] : null;
            }
            else
            {
                titleText = FindText("Text (TMP)");
            }
        }

        if (playerPanelRect == null && playerNameText != null)
        {
            playerPanelRect = playerNameText.transform.parent as RectTransform;
        }

        if (bossPanelRect == null && bossNameText != null)
        {
            bossPanelRect = bossNameText.transform.parent as RectTransform;
        }

        if (handPanelRect == null)
        {
            Transform handRoot = FindTransform("CardHandArea");
            handPanelRect = handRoot as RectTransform;
        }

        if (handContentParent == null)
        {
            Transform content = FindTransform("Content");
            handContentParent = content;
        }

        if (handContentParent != null)
        {
            handViewportRect = handContentParent.parent as RectTransform;
            if (handViewportRect != null)
            {
                handScrollRect = handViewportRect.parent as RectTransform;
            }
        }
    }

    private void ConfigureBackdrop()
    {
        Transform backgroundTransform = FindTransform("Background");
        if (backgroundTransform == null)
        {
            return;
        }

        RectTransform backgroundRect = backgroundTransform as RectTransform;
        Stretch(backgroundRect, 0f, 0f, 0f, 0f);
        Image backgroundImage = backgroundTransform.GetComponent<Image>();
        if (backgroundImage != null)
        {
            backgroundImage.color = new Color(0.06f, 0.07f, 0.09f, 1f);
        }

        Image topBand = GetOrCreateImage(backgroundTransform, "TopBand", new Color(0.02f, 0.03f, 0.05f, 0.42f));
        topBand.rectTransform.SetAsFirstSibling();
        Stretch(topBand.rectTransform, 0f, 0f, 0f, 0f);
        topBand.rectTransform.anchorMin = new Vector2(0f, 1f);
        topBand.rectTransform.anchorMax = new Vector2(1f, 1f);
        topBand.rectTransform.pivot = new Vector2(0.5f, 1f);
        topBand.rectTransform.sizeDelta = new Vector2(0f, TopHudHeight + Margin);
        topBand.rectTransform.anchoredPosition = Vector2.zero;

        Image bottomDock = GetOrCreateImage(backgroundTransform, "BottomDock", new Color(0.01f, 0.02f, 0.03f, 0.8f));
        Stretch(bottomDock.rectTransform, 0f, 0f, 0f, 0f);
        bottomDock.rectTransform.anchorMin = new Vector2(0f, 0f);
        bottomDock.rectTransform.anchorMax = new Vector2(1f, 0f);
        bottomDock.rectTransform.pivot = new Vector2(0.5f, 0f);
        bottomDock.rectTransform.sizeDelta = new Vector2(0f, BottomDockHeight + Margin * 2f);
        bottomDock.rectTransform.anchoredPosition = Vector2.zero;

        Image stageGlow = GetOrCreateImage(backgroundTransform, "StageGlow", new Color(0.12f, 0.17f, 0.22f, 0.34f));
        Stretch(stageGlow.rectTransform, 220f, 220f, 180f, 280f);
    }

    private void ConfigurePlayerPanel()
    {
        if (playerPanelRect == null)
        {
            return;
        }

        Anchor(playerPanelRect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(PlayerHudWidth, 110f), new Vector2(Margin, -Margin));
        StylePanel(playerPanelRect, new Color(0.78f, 0.61f, 0.31f));

        if (playerHealthValueText == null)
        {
            playerHealthValueText = GetOrCreateText(playerPanelRect, "PlayerHealthValueText", 24, TextAlignmentOptions.Left);
        }

        ConfigureText(playerNameText, 32, TextAlignmentOptions.Left);
        ConfigureText(playerHealthValueText, 24, TextAlignmentOptions.Left);
        playerHealthValueText.color = new Color(0.97f, 0.95f, 0.9f);

        if (playerNameText != null)
        {
            Anchor(playerNameText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(-28f, 24f), new Vector2(18f, -18f));
            playerNameText.gameObject.SetActive(false);
        }

        if (playerHealthSlider != null)
        {
            RectTransform sliderRect = playerHealthSlider.transform as RectTransform;
            Anchor(sliderRect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(-36f, 24f), new Vector2(0f, -46f));
        }

        if (playerHealthValueText != null)
        {
            Anchor(playerHealthValueText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(-36f, 24f), new Vector2(18f, -74f));
        }

        turnText = GetOrCreateText(playerPanelRect, "TurnText", 24, TextAlignmentOptions.TopRight);
        Anchor(turnText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(112f, 24f), new Vector2(-18f, -18f));

        energyText = GetOrCreateText(playerPanelRect, "EnergyText", 22, TextAlignmentOptions.BottomLeft);
        Anchor(energyText.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(130f, 24f), new Vector2(18f, 16f));

        playerShieldText = GetOrCreateText(playerPanelRect, "ShieldText", 22, TextAlignmentOptions.BottomRight);
        Anchor(playerShieldText.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(120f, 24f), new Vector2(-18f, 16f));
        playerShieldText.color = new Color(0.96f, 0.87f, 0.66f);
    }

    private void ConfigureBossPanel()
    {
        if (bossPanelRect == null)
        {
            return;
        }

        Anchor(bossPanelRect, new Vector2(0.5f, 0.64f), new Vector2(0.5f, 0.64f), new Vector2(520f, 184f), Vector2.zero);
        StylePanel(bossPanelRect, new Color(0.37f, 0.72f, 0.94f));

        intentText = GetOrCreateText(bossPanelRect, "IntentText", 22, TextAlignmentOptions.Center);
        Anchor(intentText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(470f, 26f), new Vector2(0f, -22f));

        ConfigureText(bossNameText, 36, TextAlignmentOptions.Center);
        ConfigureText(bossHealthValueText, 28, TextAlignmentOptions.Center);

        if (bossNameText != null)
        {
            Anchor(bossNameText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(420f, 42f), new Vector2(0f, 20f));
        }

        if (bossHealthSlider != null)
        {
            RectTransform sliderRect = bossHealthSlider.transform as RectTransform;
            Anchor(sliderRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(340f, 22f), new Vector2(0f, -10f));
        }

        if (bossHealthValueText != null)
        {
            Anchor(bossHealthValueText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(220f, 30f), new Vector2(0f, -44f));
        }

        bossShieldText = GetOrCreateText(bossPanelRect, "BossShieldText", 20, TextAlignmentOptions.Center);
        Anchor(bossShieldText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(220f, 24f), new Vector2(0f, -72f));
    }

    private void ConfigureHandPanel()
    {
        if (handPanelRect == null || handContentParent == null)
        {
            return;
        }

        Anchor(handPanelRect, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(-Margin * 2f, BottomDockHeight), new Vector2(0f, Margin));
        StylePanel(handPanelRect, new Color(0.88f, 0.74f, 0.33f));

        if (handScrollRect != null)
        {
            Anchor(handScrollRect, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(-290f, -86f), new Vector2(-126f, -8f));
            ScrollRect scrollRect = handScrollRect.GetComponent<ScrollRect>();
            if (scrollRect != null)
            {
                scrollRect.horizontal = true;
                scrollRect.vertical = false;
            }
        }

        if (handViewportRect != null)
        {
            Stretch(handViewportRect, 0f, 0f, 0f, 0f);
            Image viewportImage = handViewportRect.GetComponent<Image>() ?? handViewportRect.gameObject.AddComponent<Image>();
            viewportImage.color = new Color(0.09f, 0.11f, 0.14f, 0.88f);
        }

        RectTransform contentRect = handContentParent as RectTransform;
        HorizontalLayoutGroup layoutGroup = handContentParent.GetComponent<HorizontalLayoutGroup>() ?? handContentParent.gameObject.AddComponent<HorizontalLayoutGroup>();
        layoutGroup.spacing = 14f;
        layoutGroup.padding = new RectOffset(8, 8, 0, 0);
        layoutGroup.childAlignment = TextAnchor.MiddleLeft;
        layoutGroup.childControlHeight = false;
        layoutGroup.childControlWidth = false;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.childForceExpandWidth = false;

        ContentSizeFitter fitter = handContentParent.GetComponent<ContentSizeFitter>() ?? handContentParent.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        contentRect.anchorMin = new Vector2(0f, 0.5f);
        contentRect.anchorMax = new Vector2(0f, 0.5f);
        contentRect.pivot = new Vector2(0f, 0.5f);
        contentRect.sizeDelta = new Vector2(0f, 204f);
        contentRect.anchoredPosition = new Vector2(18f, -10f);

        handHeaderText = GetOrCreateText(handPanelRect, "HandHeaderText", 28, TextAlignmentOptions.Left);
        handHeaderText.color = new Color(0.96f, 0.87f, 0.66f);
        Anchor(handHeaderText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(320f, 30f), new Vector2(22f, -18f));

        drawPileText = GetOrCreateText(handPanelRect, "DrawPileText", 18, TextAlignmentOptions.Center);
        drawPileText.color = new Color(0.76f, 0.8f, 0.86f);
        Anchor(drawPileText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(86f, 24f), new Vector2(-248f, -18f));

        discardPileText = GetOrCreateText(handPanelRect, "DiscardPileText", 18, TextAlignmentOptions.Center);
        discardPileText.color = new Color(0.76f, 0.8f, 0.86f);
        Anchor(discardPileText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(98f, 24f), new Vector2(-148f, -18f));
    }

    private void ConfigureControls()
    {
        if (canvasRect == null || handPanelRect == null)
        {
            return;
        }

        RectTransform controlRoot = GetOrCreateRect(canvasRect, "BattleControls");
        Stretch(controlRoot, 0f, 0f, 0f, 0f);

        RectTransform statusPanel = GetOrCreateRect(controlRoot, "StatusPanel");
        Anchor(statusPanel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(940f, 88f), new Vector2(0f, -Margin));
        StylePanel(statusPanel, new Color(0.54f, 0.4f, 0.85f));

        statusText = GetOrCreateText(statusPanel, "StatusText", 18, TextAlignmentOptions.TopLeft);
        statusText.color = new Color(0.83f, 0.84f, 0.88f);
        Stretch(statusText.rectTransform, 18f, 18f, 16f, 16f);

        RectTransform controlPanel = GetOrCreateRect(handPanelRect, "ControlPanel");
        Anchor(controlPanel, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(126f, 42f), new Vector2(-18f, -18f));
        StylePanel(controlPanel, new Color(0.92f, 0.45f, 0.24f));

        endTurnButton = GetOrCreateButton(controlPanel, "EndTurnButton", "End Turn");
        RectTransform buttonRect = endTurnButton.transform as RectTransform;
        Stretch(buttonRect, 6f, 6f, 6f, 6f);
        endTurnButton.onClick.RemoveAllListeners();
        endTurnButton.onClick.AddListener(() => controller?.EndPlayerTurn());
        endTurnButtonImage = endTurnButton.GetComponent<Image>();

        Transform testButtons = FindTransform("TestButtons");
        if (testButtons != null)
        {
            testButtons.gameObject.SetActive(false);
        }
    }

    private void ConfigureResultsPanel()
    {
        if (resultsPanel == null)
        {
            return;
        }

        RectTransform resultsRect = resultsPanel.transform as RectTransform;
        Stretch(resultsRect, 0f, 0f, 0f, 0f);
        Image overlay = resultsRect.GetComponent<Image>() ?? resultsRect.gameObject.AddComponent<Image>();
        overlay.color = new Color(0.04f, 0.03f, 0.02f, 0.94f);

        RectTransform resultWindow = resultsRect.childCount > 0 ? resultsRect.GetChild(0) as RectTransform : resultsRect;
        if (resultWindow != null)
        {
            StylePanel(resultWindow, new Color(0.88f, 0.74f, 0.33f));
        }

        Button continueButton = resultsPanel.GetComponentInChildren<Button>(true);
        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(OnReturnToWorkshop);
        }
    }

    private void ConfigureRunSummaryPanel()
    {
        if (canvasRect == null)
        {
            return;
        }

        RectTransform panelRect = GetOrCreateRect(canvasRect, "RunSummaryPanel");
        Stretch(panelRect, 0f, 0f, 0f, 0f);
        runSummaryPanel = panelRect.gameObject;

        Image overlay = panelRect.GetComponent<Image>() ?? panelRect.gameObject.AddComponent<Image>();
        overlay.color = new Color(0.04f, 0.03f, 0.02f, 0.96f);

        RectTransform windowRect = GetOrCreateRect(panelRect, "Window");
        Anchor(windowRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(1160f, 760f), Vector2.zero);
        StylePanel(windowRect, new Color(0.88f, 0.74f, 0.33f));

        runSummaryTitleText = GetOrCreateText(windowRect, "SummaryTitle", 44, TextAlignmentOptions.Left);
        runSummaryTitleText.color = new Color(0.97f, 0.95f, 0.9f);
        Anchor(runSummaryTitleText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(-80f, 52f), new Vector2(0f, -32f));

        runSummaryBodyText = GetOrCreateText(windowRect, "SummaryBody", 24, TextAlignmentOptions.TopLeft);
        runSummaryBodyText.color = new Color(0.83f, 0.84f, 0.88f);
        Stretch(runSummaryBodyText.rectTransform, 40f, 40f, 100f, 120f);

        returnToMenuButton = GetOrCreateButton(windowRect, "ReturnToMenuButton", "Return To Menu");
        RectTransform buttonRect = returnToMenuButton.transform as RectTransform;
        Anchor(buttonRect, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(240f, 64f), new Vector2(-38f, 34f));
        returnToMenuButton.onClick.RemoveAllListeners();
        returnToMenuButton.onClick.AddListener(OnReturnToMenu);

        runSummaryPanel.SetActive(false);
    }

    private RuntimeCardView CreateRuntimeCard(WorkshopBattleCardEntry cardEntry, int cardIndex)
    {
        GameObject cardObject = new GameObject($"Card_{cardIndex}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
        cardObject.transform.SetParent(handContentParent, false);

        RectTransform rect = cardObject.transform as RectTransform;
        rect.sizeDelta = new Vector2(182f, 204f);

        Image background = cardObject.GetComponent<Image>();
        background.color = GetCardColor(cardEntry.Element);
        ApplyCardFrame(rect, cardEntry.Element);

        Button button = cardObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.96f, 0.84f, 1f);
        colors.pressedColor = new Color(0.86f, 0.82f, 0.72f, 1f);
        colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.7f);
        button.colors = colors;
        button.targetGraphic = background;
        button.onClick.AddListener(() => controller?.PlayCard(cardIndex));

        LayoutElement layoutElement = cardObject.GetComponent<LayoutElement>();
        layoutElement.preferredWidth = 182f;
        layoutElement.preferredHeight = 204f;

        Image costBadge = GetOrCreateImage(cardObject.transform, "CostBadge", new Color(0.18f, 0.15f, 0.12f, 0.96f));
        Anchor(costBadge.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(34f, 34f), new Vector2(12f, -12f));
        TextMeshProUGUI costText = GetOrCreateText(costBadge.transform as RectTransform, "CostText", 20, TextAlignmentOptions.Center);
        Stretch(costText.rectTransform, 0f, 0f, 0f, 0f);
        costText.text = controller != null ? controller.GetCardEnergyCost(cardIndex).ToString() : "1";

        TextMeshProUGUI nameText = GetOrCreateText(rect, "NameText", 19, TextAlignmentOptions.TopLeft);
        nameText.color = new Color(0.97f, 0.95f, 0.9f);
        Anchor(nameText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(-52f, 44f), new Vector2(44f, -10f));
        nameText.text = cardEntry.DisplayName;

        TextMeshProUGUI bodyText = GetOrCreateText(rect, "BodyText", 16, TextAlignmentOptions.TopLeft);
        bodyText.color = new Color(0.83f, 0.84f, 0.88f);
        Anchor(bodyText.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-24f, 78f), new Vector2(0f, -2f));
        bodyText.text = controller != null ? controller.GetCardSummary(cardEntry) : string.Empty;

        TextMeshProUGUI footerText = GetOrCreateText(rect, "FooterText", 13, TextAlignmentOptions.BottomLeft);
        footerText.color = new Color(0.67f, 0.72f, 0.79f);
        Anchor(footerText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(-24f, 28f), new Vector2(12f, 10f));
        footerText.text = $"{cardEntry.Role}  {cardEntry.Element}";

        return new RuntimeCardView
        {
            Button = button,
            Background = background,
            BaseColor = background.color
        };
    }

    private void UpdateCardInteractivity()
    {
        if (controller == null)
        {
            return;
        }

        for (int index = 0; index < runtimeCards.Count; index++)
        {
            RuntimeCardView view = runtimeCards[index];
            bool canPlay = controller.CanPlayCard(index);
            view.Button.interactable = canPlay;
            view.Background.color = canPlay
                ? new Color(view.BaseColor.r, view.BaseColor.g, view.BaseColor.b, 0.96f)
                : new Color(0.34f, 0.31f, 0.3f, 0.72f);
        }
    }

    private TextMeshProUGUI FindText(string name)
    {
        Transform found = FindTransform(name);
        return found != null ? found.GetComponent<TextMeshProUGUI>() : null;
    }

    private Transform FindTransform(string name)
    {
        foreach (Transform child in rootCanvas.transform.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == name)
            {
                return child;
            }
        }

        return null;
    }

    private static void ConfigureText(TextMeshProUGUI text, float fontSize, TextAlignmentOptions alignment)
    {
        if (text == null)
        {
            return;
        }

        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = new Color(0.97f, 0.95f, 0.9f);
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow;
    }

    private static RectTransform GetOrCreateRect(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
        {
            return existing as RectTransform;
        }

        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        return gameObject.transform as RectTransform;
    }

    private static TextMeshProUGUI GetOrCreateText(Transform parent, string name, float fontSize, TextAlignmentOptions alignment)
    {
        Transform existing = parent.Find(name);
        TextMeshProUGUI text = existing != null
            ? existing.GetComponent<TextMeshProUGUI>()
            : null;

        if (text == null)
        {
            RectTransform rect = GetOrCreateRect(parent, name);
            text = rect.gameObject.GetComponent<TextMeshProUGUI>() ?? rect.gameObject.AddComponent<TextMeshProUGUI>();
        }

        ConfigureText(text, fontSize, alignment);
        return text;
    }

    private static Image GetOrCreateImage(Transform parent, string name, Color color)
    {
        RectTransform rect = GetOrCreateRect(parent, name);
        Image image = rect.GetComponent<Image>() ?? rect.gameObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    private static Button GetOrCreateButton(Transform parent, string name, string label)
    {
        RectTransform rect = GetOrCreateRect(parent, name);
        Image image = rect.GetComponent<Image>() ?? rect.gameObject.AddComponent<Image>();
        image.color = new Color(0.12f, 0.14f, 0.17f, 0.96f);

        Button button = rect.GetComponent<Button>() ?? rect.gameObject.AddComponent<Button>();
        TextMeshProUGUI buttonLabel = GetOrCreateText(rect, "Label", 26, TextAlignmentOptions.Center);
        buttonLabel.color = new Color(0.96f, 0.87f, 0.66f);
        Stretch(buttonLabel.rectTransform, 0f, 0f, 0f, 0f);
        buttonLabel.text = label;
        return button;
    }

    private static void StylePanel(RectTransform rect, Color accent)
    {
        if (rect == null)
        {
            return;
        }

        Image background = rect.GetComponent<Image>() ?? rect.gameObject.AddComponent<Image>();
        background.color = new Color(0.05f, 0.06f, 0.08f, 0.94f);

        Image shadow = GetOrCreateImage(rect, "Shadow", new Color(0f, 0f, 0f, 0.24f));
        shadow.rectTransform.SetAsFirstSibling();
        Stretch(shadow.rectTransform, -6f, 6f, -8f, 8f);

        Image accentBar = GetOrCreateImage(rect, "AccentBar", accent);
        accentBar.rectTransform.SetSiblingIndex(rect.childCount - 1);
        accentBar.rectTransform.anchorMin = new Vector2(0f, 1f);
        accentBar.rectTransform.anchorMax = new Vector2(1f, 1f);
        accentBar.rectTransform.pivot = new Vector2(0.5f, 1f);
        accentBar.rectTransform.sizeDelta = new Vector2(0f, 4f);
        accentBar.rectTransform.anchoredPosition = Vector2.zero;

        Image top = GetOrCreateImage(rect, "OutlineTop", new Color(0.2f, 0.19f, 0.18f, 1f));
        Image bottom = GetOrCreateImage(rect, "OutlineBottom", new Color(0.2f, 0.19f, 0.18f, 1f));
        Image left = GetOrCreateImage(rect, "OutlineLeft", new Color(0.2f, 0.19f, 0.18f, 1f));
        Image right = GetOrCreateImage(rect, "OutlineRight", new Color(0.2f, 0.19f, 0.18f, 1f));

        top.rectTransform.anchorMin = new Vector2(0f, 1f);
        top.rectTransform.anchorMax = new Vector2(1f, 1f);
        top.rectTransform.pivot = new Vector2(0.5f, 1f);
        top.rectTransform.sizeDelta = new Vector2(0f, 1f);
        top.rectTransform.anchoredPosition = Vector2.zero;

        bottom.rectTransform.anchorMin = new Vector2(0f, 0f);
        bottom.rectTransform.anchorMax = new Vector2(1f, 0f);
        bottom.rectTransform.pivot = new Vector2(0.5f, 0f);
        bottom.rectTransform.sizeDelta = new Vector2(0f, 1f);
        bottom.rectTransform.anchoredPosition = Vector2.zero;

        left.rectTransform.anchorMin = new Vector2(0f, 0f);
        left.rectTransform.anchorMax = new Vector2(0f, 1f);
        left.rectTransform.pivot = new Vector2(0f, 0.5f);
        left.rectTransform.sizeDelta = new Vector2(1f, 0f);
        left.rectTransform.anchoredPosition = Vector2.zero;

        right.rectTransform.anchorMin = new Vector2(1f, 0f);
        right.rectTransform.anchorMax = new Vector2(1f, 1f);
        right.rectTransform.pivot = new Vector2(1f, 0.5f);
        right.rectTransform.sizeDelta = new Vector2(1f, 0f);
        right.rectTransform.anchoredPosition = Vector2.zero;
    }

    private static void ApplyCardFrame(RectTransform rect, WorkshopElementAttribute element)
    {
        Color accent = GetCardColor(element);
        Image topBar = GetOrCreateImage(rect, "CardAccentBar", accent);
        topBar.rectTransform.anchorMin = new Vector2(0f, 1f);
        topBar.rectTransform.anchorMax = new Vector2(1f, 1f);
        topBar.rectTransform.pivot = new Vector2(0.5f, 1f);
        topBar.rectTransform.sizeDelta = new Vector2(0f, 4f);
        topBar.rectTransform.anchoredPosition = Vector2.zero;

        Image top = GetOrCreateImage(rect, "CardOutlineTop", new Color(0.2f, 0.19f, 0.18f, 1f));
        Image bottom = GetOrCreateImage(rect, "CardOutlineBottom", new Color(0.2f, 0.19f, 0.18f, 1f));
        Image left = GetOrCreateImage(rect, "CardOutlineLeft", new Color(0.2f, 0.19f, 0.18f, 1f));
        Image right = GetOrCreateImage(rect, "CardOutlineRight", new Color(0.2f, 0.19f, 0.18f, 1f));

        top.rectTransform.anchorMin = new Vector2(0f, 1f);
        top.rectTransform.anchorMax = new Vector2(1f, 1f);
        top.rectTransform.pivot = new Vector2(0.5f, 1f);
        top.rectTransform.sizeDelta = new Vector2(0f, 1f);
        top.rectTransform.anchoredPosition = Vector2.zero;

        bottom.rectTransform.anchorMin = new Vector2(0f, 0f);
        bottom.rectTransform.anchorMax = new Vector2(1f, 0f);
        bottom.rectTransform.pivot = new Vector2(0.5f, 0f);
        bottom.rectTransform.sizeDelta = new Vector2(0f, 1f);
        bottom.rectTransform.anchoredPosition = Vector2.zero;

        left.rectTransform.anchorMin = new Vector2(0f, 0f);
        left.rectTransform.anchorMax = new Vector2(0f, 1f);
        left.rectTransform.pivot = new Vector2(0f, 0.5f);
        left.rectTransform.sizeDelta = new Vector2(1f, 0f);
        left.rectTransform.anchoredPosition = Vector2.zero;

        right.rectTransform.anchorMin = new Vector2(1f, 0f);
        right.rectTransform.anchorMax = new Vector2(1f, 1f);
        right.rectTransform.pivot = new Vector2(1f, 0.5f);
        right.rectTransform.sizeDelta = new Vector2(1f, 0f);
        right.rectTransform.anchoredPosition = Vector2.zero;
    }

    private static void Stretch(RectTransform rect, float left, float right, float top, float bottom)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
        rect.pivot = new Vector2(0.5f, 0.5f);
    }

    private static void Anchor(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 size, Vector2 anchoredPosition)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2((anchorMin.x + anchorMax.x) * 0.5f, (anchorMin.y + anchorMax.y) * 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;
    }

    private static Color GetCardColor(WorkshopElementAttribute element)
    {
        return element switch
        {
            WorkshopElementAttribute.Fire => new Color(0.55f, 0.2f, 0.12f, 0.96f),
            WorkshopElementAttribute.Water => new Color(0.14f, 0.31f, 0.51f, 0.96f),
            WorkshopElementAttribute.Wind => new Color(0.2f, 0.42f, 0.28f, 0.96f),
            WorkshopElementAttribute.Earth => new Color(0.42f, 0.29f, 0.18f, 0.96f),
            WorkshopElementAttribute.Ice => new Color(0.45f, 0.63f, 0.74f, 0.96f),
            WorkshopElementAttribute.Thunder => new Color(0.46f, 0.35f, 0.16f, 0.96f),
            WorkshopElementAttribute.Light => new Color(0.63f, 0.58f, 0.35f, 0.96f),
            WorkshopElementAttribute.Dark => new Color(0.27f, 0.21f, 0.33f, 0.96f),
            _ => new Color(0.31f, 0.2f, 0.14f, 0.96f)
        };
    }

    // Test functions
    public void KillBoss()
    {
        if (controller == null || controller.Boss == null) return;

        controller.Boss.CurrentHealth = 0;

        BattleResult finalResult = new BattleResult
        {
            ResultType = BattleResultType.Victory,
            BossId = controller.CurrentBossDefinition?.BossId,
            BossDisplayName = controller.Boss.DisplayName,
            TotalDamageDealt = controller.Boss.MaxHealth,
            TurnsElapsed = 1,
            DefeatRewardId = controller.CurrentBossDefinition != null ? controller.CurrentBossDefinition.DefeatRewardId : string.Empty,
        };

        BattleResultBridge.Commit(finalResult);
    }

    public void KillYourself()
    {
        if (controller == null || controller.Boss == null) return;

        controller.Player.CurrentHealth = 0;

        BattleResult finalResult = new BattleResult
        {
            ResultType = BattleResultType.Defeat,
            BossId = controller.CurrentBossDefinition?.BossId,
            BossDisplayName = controller.Boss.DisplayName,
            TotalDamageDealt = 0,
            TurnsElapsed = 1,
        };

        BattleResultBridge.Commit(finalResult);
    }

    private void OnEnable()
    {
        BattleResultBridge.ResultCommitted += ShowResults;
    }

    private void OnDisable()
    {
        BattleResultBridge.ResultCommitted -= ShowResults;
    }

    private void ShowResults()
    {
        var result = BattleResultBridge.CurrentResult;
        if (result == null) return;

        if (result.ResultType == BattleResultType.Defeat || RunProgressBridge.CurrentSummary.RunEnded)
        {
            ShowRunSummary();
            return;
        }

        if (resultsPanel != null)
        {
            resultsPanel.SetActive(true);
        }

        if (titleText != null)
        {
            titleText.text = result.ResultType.ToString().ToUpper();
        }
        if (bossNameResultText != null)
        {
            bossNameResultText.text = $"Enemy: {result.BossDisplayName}";
        }
        if (turnsText != null)
        {
            turnsText.text = $"Turns: {result.TurnsElapsed}";
        }
        if (damageText != null)
        {
            damageText.text = $"Total Damage Dealt: {result.TotalDamageDealt}";
        }

        if (rewardText != null)
        {
            rewardText.text = string.IsNullOrEmpty(result.DefeatRewardId)
                ? "Battle Reward: None configured yet"
                : $"Battle Reward: {result.DefeatRewardId}";
        }

    }

    private void ShowRunSummary()
    {
        if (resultsPanel != null)
        {
            resultsPanel.SetActive(false);
        }

        if (runSummaryPanel != null)
        {
            runSummaryPanel.SetActive(true);
        }

        RunSummaryData summary = RunProgressBridge.CurrentSummary;
        if (runSummaryTitleText != null)
        {
            runSummaryTitleText.text = summary.RunWon
                ? "Run Complete"
                : "Run Failed";
        }

        if (runSummaryBodyText != null)
        {
            runSummaryBodyText.text = BuildRunSummaryBody(summary);
        }
    }

    private static string BuildRunSummaryBody(RunSummaryData summary)
    {
        List<string> lines = new List<string>
        {
            summary.FinalOutcomeDescription,
            string.Empty,
            $"Victories: {summary.Victories}    Defeats: {summary.Defeats}",
            $"Turns Fought: {summary.TotalTurnsElapsed}    Prep Ticks Used: {summary.TotalPrepTicksUsed}",
            $"Cards Forged: {summary.TotalCraftedCardCopies} across {summary.TotalCraftedCardTypes} card type snapshots",
            $"Cards Played: {summary.TotalCardsPlayed}",
            $"Damage Dealt: {summary.TotalDamageDealt}",
            $"Healing Done: {summary.TotalHealingDone}",
            $"Shield Gained: {summary.TotalShieldGained}"
        };

        if (!string.IsNullOrWhiteSpace(summary.LegacyUnlockName))
        {
            lines.Add($"Legacy Sigil Unlocked: {summary.LegacyUnlockName}");
        }

        if (summary.RewardsClaimed.Count > 0)
        {
            lines.Add(string.Empty);
            lines.Add("Rewards Claimed:");
            foreach (string reward in summary.RewardsClaimed)
            {
                lines.Add($"- {reward}");
            }
        }

        if (summary.BattleHistory.Count > 0)
        {
            lines.Add(string.Empty);
            lines.Add("Battle History:");
            foreach (RunBattleRecord record in summary.BattleHistory)
            {
                string outcome = record.Victory ? "Victory" : "Defeat";
                string encounterLine = $"{record.EncounterIndex}. {record.EncounterLabel} - {outcome}";
                string detailLine = $"   {record.BossDisplayName} | Turns {record.TurnsElapsed} | Cards {record.CardsPlayed} | Damage {record.TotalDamageDealt}";
                lines.Add(encounterLine);
                lines.Add(detailLine);
            }
        }

        lines.Add(string.Empty);
        lines.Add("Return to the menu to begin another siege.");
        return string.Join("\n", lines);
    }

    public void OnReturnToWorkshop()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            RunProgressBridge.CurrentSummary.RunEnded
                ? MainMenuSceneName
                : WorkshopSceneName);
    }

    public void OnReturnToMenu()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(MainMenuSceneName);
    }


}
