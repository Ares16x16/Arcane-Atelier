using ArcaneAtelier.Battle;
using ArcaneAtelier.Workshop;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArcaneAtelier.Integration
{
    public static class GameFlowRuntime
    {
        private const string MainMenuSceneName = "MainMenuScene";
        private const string WorkshopSceneName = "WorkshopScene";
        private const string BattleSceneName = "BattleScene";
        private const int ActOneCombatThreshold = 4;
        private const int ActTwoCombatThreshold = 5;
        private const int ActThreeCombatThreshold = 6;

        private static int currentAct = 1;
        private static int clearedCombatsInAct;
        private static bool bossPending;
        private static bool activeBattleIsBoss;

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

            if (scene.name != WorkshopSceneName && scene.name != BattleSceneName)
            {
                return;
            }

            if (scene.name == BattleSceneName)
            {
                activeBattleIsBoss = bossPending;
                return;
            }

            string postBattleStatus = string.Empty;

            if (!BattleResultBridge.TryConsume(out BattleResult result))
            {
                ConfigureWorkshopPreparation(Object.FindAnyObjectByType<WorkshopSceneController>(), postBattleStatus);
                return;
            }

            WorkshopSceneController controller = Object.FindAnyObjectByType<WorkshopSceneController>();
            if (controller == null)
            {
                Debug.LogWarning("GameFlowRuntime: WorkshopScene loaded without a WorkshopSceneController; battle result could not be applied.");
                return;
            }

            if (result.ResultType == BattleResultType.Victory)
            {
                bool defeatedBoss = activeBattleIsBoss;
                UpdateProgressAfterVictory(defeatedBoss);

                if (controller.TryApplyRewardById(result.DefeatRewardId, out WorkshopRewardDefinition reward))
                {
                    postBattleStatus = $"Victory over {FormatBossName(result)}. Reward applied: {reward.DisplayName}.";
                    ConfigureWorkshopPreparation(controller, postBattleStatus);
                    return;
                }

                if (!string.IsNullOrWhiteSpace(result.DefeatRewardId))
                {
                    postBattleStatus = $"Victory over {FormatBossName(result)}. Reward '{result.DefeatRewardId}' was not found.";
                    ConfigureWorkshopPreparation(controller, postBattleStatus);
                    return;
                }

                postBattleStatus = $"Victory over {FormatBossName(result)}. No battle reward is configured yet.";
                ConfigureWorkshopPreparation(controller, postBattleStatus);
                return;
            }

            if (result.ResultType == BattleResultType.Defeat)
            {
                activeBattleIsBoss = false;
                postBattleStatus = $"Defeated by {FormatBossName(result)}. Rebuild your deck and try again.";
                ConfigureWorkshopPreparation(controller, postBattleStatus);
                return;
            }

            ConfigureWorkshopPreparation(controller, "Returned from battle.");
        }

        private static void ResetRunState()
        {
            currentAct = 1;
            clearedCombatsInAct = 0;
            bossPending = false;
            activeBattleIsBoss = false;
        }

        private static void UpdateProgressAfterVictory(bool defeatedBoss)
        {
            activeBattleIsBoss = false;

            if (defeatedBoss)
            {
                currentAct = Mathf.Min(3, currentAct + 1);
                clearedCombatsInAct = 0;
                bossPending = false;
                return;
            }

            clearedCombatsInAct++;
            if (clearedCombatsInAct >= GetCombatThresholdForAct(currentAct))
            {
                bossPending = true;
            }
        }

        private static void ConfigureWorkshopPreparation(WorkshopSceneController controller, string postBattleStatus)
        {
            if (controller == null)
            {
                return;
            }

            int budget = bossPending ? GetBossPrepBudgetForAct(currentAct) : GetSkirmishPrepBudgetForAct(currentAct);
            string label = bossPending
                ? $"Act {currentAct} Boss"
                : $"Act {currentAct} Skirmish {clearedCombatsInAct + 1}/{GetCombatThresholdForAct(currentAct)}";

            controller.SetPreparationBudget(budget, label);

            if (!string.IsNullOrWhiteSpace(postBattleStatus))
            {
                controller.SetStatusMessage($"{postBattleStatus} Next: {label}, {budget} prep ticks.");
            }
        }

        private static int GetCombatThresholdForAct(int act)
        {
            switch (act)
            {
                case 1:
                    return ActOneCombatThreshold;
                case 2:
                    return ActTwoCombatThreshold;
                default:
                    return ActThreeCombatThreshold;
            }
        }

        private static int GetSkirmishPrepBudgetForAct(int act)
        {
            int baseBudget;
            switch (act)
            {
                case 1:
                    baseBudget = 120;
                    break;
                case 2:
                    baseBudget = 95;
                    break;
                default:
                    baseBudget = 75;
                    break;
            }

            return Mathf.Max(45, baseBudget - clearedCombatsInAct * 5);
        }

        private static int GetBossPrepBudgetForAct(int act)
        {
            switch (act)
            {
                case 1:
                    return 150;
                case 2:
                    return 125;
                default:
                    return 105;
            }
        }

        private static string FormatBossName(BattleResult result)
        {
            if (result == null || string.IsNullOrWhiteSpace(result.BossDisplayName))
            {
                return "the enemy";
            }

            return result.BossDisplayName;
        }
    }
}
