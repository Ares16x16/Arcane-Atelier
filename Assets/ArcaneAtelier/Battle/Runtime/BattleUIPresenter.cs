using UnityEngine;
using TMPro;
using UnityEngine.UI;
using ArcaneAtelier.Battle;
using ArcaneAtelier.Workshop;

public class BattleUIPresenter : MonoBehaviour
{
    private const string WorkshopSceneName = "WorkshopScene";

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

    void Start()
    {
        resultsPanel.SetActive(false);
        // We wait a frame for the controller's Awake to finish
        Invoke(nameof(UpdateUI), 0.1f);
        Invoke(nameof(PopulateHand), 0.1f);
    }

    void Update()
    {
        UpdateUI();
    }

    public void PopulateHand()
    {
        // Check 1: Is the script actually linked to the UI?
        if (handContentParent == null)
        {
            Debug.LogError("BattleUIPresenter: handContentParent is not assigned in the Inspector!");
            return;
        }

        // Check 2: Is there a button to spawn?
        if (cardPrefab == null)
        {
            Debug.LogError("BattleUIPresenter: cardPrefab is not assigned in the Inspector!");
            return;
        }

        // Clear old cards
        foreach (Transform child in handContentParent)
        {
            Destroy(child.gameObject);
        }

        // Check 3: Does the controller actually have cards?
        if (controller == null || controller.Cards == null) return;

        foreach (var cardEntry in controller.Cards)
        {
            GameObject newCard = Instantiate(cardPrefab, handContentParent);
            var txt = newCard.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null)
            {
                txt.text = $"{cardEntry.DisplayName}\nx{cardEntry.Amount}";
            }
        }
    }

    public void UpdateUI()
    {
        if (controller == null || controller.Player == null || controller.Boss == null) return;

        // Update Boss
        bossNameText.text = controller.Boss.DisplayName;
        bossHealthSlider.maxValue = controller.Boss.MaxHealth;
        bossHealthSlider.value = controller.Boss.CurrentHealth;
        bossHealthValueText.text = $"{controller.Boss.CurrentHealth} / {controller.Boss.MaxHealth}";

        // Update Player
        playerNameText.text = controller.Player.DisplayName;
        playerHealthSlider.maxValue = controller.Player.MaxHealth;
        playerHealthSlider.value = controller.Player.CurrentHealth;
        playerHealthValueText.text = $"{controller.Player.CurrentHealth} / {controller.Player.MaxHealth}";

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

        resultsPanel.SetActive(true);

        titleText.text = result.ResultType.ToString().ToUpper();
        bossNameResultText.text = $"Enemy: {result.BossDisplayName}";
        turnsText.text = $"Turns: {result.TurnsElapsed}";
        damageText.text = $"Total Damage Dealt: {result.TotalDamageDealt}";

        if (!string.IsNullOrEmpty(result.DefeatRewardId))
        {
            rewardText.text = $"Battle Reward: {result.DefeatRewardId}";
        }

    }

    public void OnReturnToWorkshop()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(WorkshopSceneName);
    }


}
