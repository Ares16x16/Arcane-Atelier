using ArcaneAtelier.Battle;
using ArcaneAtelier.Workshop;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArcaneAtelier.Integration
{
    public static class GameFlowRuntime
    {
        private const string MainMenuSceneName = "MainMenuScene";
        private const string WorkshopSceneName = "WorkshopScene";
        private const string BattleSceneName = "BattleScene";
        private const int NormalEncountersBeforeBoss = 3;

        private static readonly EncounterPlan[] EncounterPlans =
        {
            new EncounterPlan(
                "encounter.ember.wisp",
                "Breach 1: Ember Wisp",
                "A fast outer-ward scout that opens mixed spell fusion after the starter workshop proves basic fusion.",
                "enemy.ember.wisp",
                120,
                "reward.unlock.spell_fusion_intermediate"),
            new EncounterPlan(
                "encounter.hollow.cleric",
                "Breach 2: Hollow Cleric",
                "A sustain-heavy foe that opens advanced fusion for boss preparation.",
                "enemy.hollow.cleric",
                110,
                "reward.unlock.spell_fusion_advanced"),
            new EncounterPlan(
                "encounter.glass.knight",
                "Breach 3: Glass Knight",
                "A shielded duelist that rewards faster shaping before the final siege.",
                "enemy.glass.knight",
                100,
                "reward.boost.shaping")
        };

        private static readonly EncounterPlan FinalBossPlan = new EncounterPlan(
            "encounter.final.earth_golem",
            "Final Boss: Corrupted Earth Golem",
            "The atelier core is under direct assault. Bring your strongest forged loadout into the final breach.",
            "boss.earth.golem",
            140,
            string.Empty);

        private static int clearedNormalEncounters;
        private static bool currentBattleIsBoss;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneCallbacks()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;

            EnsureValidEntryScene();
        }

        private static void EnsureValidEntryScene()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.name == MainMenuSceneName)
            {
                return;
            }

            SceneManager.LoadScene(MainMenuSceneName);
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == MainMenuSceneName)
            {
                WorkshopBattlePayloadBridge.Clear();
                BattleResultBridge.Clear();
                ResetRunState();
                return;
            }

            if (scene.name == BattleSceneName)
            {
                currentBattleIsBoss = RunProgressBridge.CurrentEncounter.IsBoss;
                return;
            }

            if (scene.name != WorkshopSceneName)
            {
                return;
            }

            WorkshopSceneController controller = Object.FindAnyObjectByType<WorkshopSceneController>();
            if (controller == null)
            {
                Debug.LogWarning("GameFlowRuntime: WorkshopScene loaded without a WorkshopSceneController.");
                return;
            }

            if (!BattleResultBridge.TryConsume(out BattleResult result))
            {
                ConfigureNextEncounter(controller, "The atelier is online. Forge your first breach response.");
                return;
            }

            if (result.ResultType != BattleResultType.Victory)
            {
                return;
            }

            if (currentBattleIsBoss)
            {
                currentBattleIsBoss = false;
                return;
            }

            clearedNormalEncounters = Mathf.Min(NormalEncountersBeforeBoss, clearedNormalEncounters + 1);
            string rewardMessage = ApplyConfiguredReward(controller, GetCompletedEncounterPlan(), result);
            ConfigureNextEncounter(controller, rewardMessage);
        }

        private static void ResetRunState()
        {
            clearedNormalEncounters = 0;
            currentBattleIsBoss = false;
            RunProgressBridge.Reset();
        }

        private static void ConfigureNextEncounter(WorkshopSceneController controller, string postBattleStatus)
        {
            EncounterPlan plan = GetNextEncounterPlan();
            bool isBoss = clearedNormalEncounters >= NormalEncountersBeforeBoss;
            WorkshopRewardDefinition reward = controller.DebugRewards.FirstOrDefault(item => item != null && item.Id == plan.RewardId);
            string rewardDisplayName = reward != null ? reward.DisplayName : string.Empty;
            string rewardDescription = reward != null ? reward.Description : string.Empty;

            RunProgressBridge.ConfigureEncounter(
                clearedNormalEncounters + 1,
                NormalEncountersBeforeBoss,
                plan.EncounterId,
                plan.Label,
                plan.Description,
                plan.BossId,
                isBoss,
                plan.RewardId,
                rewardDisplayName,
                rewardDescription);

            controller.SetPreparationBudget(plan.PreparationBudget, plan.Label);
            controller.SetStatusMessage(string.IsNullOrWhiteSpace(postBattleStatus)
                ? $"{plan.Label}. {plan.Description}"
                : $"{postBattleStatus} Next: {plan.Label}. {plan.Description}");
        }

        private static string ApplyConfiguredReward(WorkshopSceneController controller, EncounterPlan plan, BattleResult result)
        {
            if (string.IsNullOrWhiteSpace(plan.RewardId))
            {
                return $"Victory over {FormatBossName(result)}. No workshop reward configured.";
            }

            if (controller.TryApplyRewardById(plan.RewardId, out WorkshopRewardDefinition reward))
            {
                return $"Victory over {FormatBossName(result)}. Reward applied: {reward.DisplayName}. {reward.Description}";
            }

            return $"Victory over {FormatBossName(result)}. Reward '{plan.RewardId}' was not found.";
        }

        private static EncounterPlan GetCompletedEncounterPlan()
        {
            int index = Mathf.Clamp(clearedNormalEncounters - 1, 0, EncounterPlans.Length - 1);
            return EncounterPlans[index];
        }

        private static EncounterPlan GetNextEncounterPlan()
        {
            if (clearedNormalEncounters >= NormalEncountersBeforeBoss)
            {
                return FinalBossPlan;
            }

            return EncounterPlans[Mathf.Clamp(clearedNormalEncounters, 0, EncounterPlans.Length - 1)];
        }

        private static string FormatBossName(BattleResult result)
        {
            if (result == null || string.IsNullOrWhiteSpace(result.BossDisplayName))
            {
                return "the enemy";
            }

            return result.BossDisplayName;
        }

        private readonly struct EncounterPlan
        {
            public EncounterPlan(string encounterId, string label, string description, string bossId, int preparationBudget, string rewardId)
            {
                EncounterId = encounterId;
                Label = label;
                Description = description;
                BossId = bossId;
                PreparationBudget = preparationBudget;
                RewardId = rewardId;
            }

            public string EncounterId { get; }
            public string Label { get; }
            public string Description { get; }
            public string BossId { get; }
            public int PreparationBudget { get; }
            public string RewardId { get; }
        }
    }
}
