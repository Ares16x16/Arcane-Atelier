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
                return;
            }

            if (scene.name != WorkshopSceneName && scene.name != BattleSceneName)
            {
                return;
            }

            if (scene.name == BattleSceneName)
            {
                return;
            }

            if (!BattleResultBridge.TryConsume(out BattleResult result))
            {
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
                if (controller.TryApplyRewardById(result.DefeatRewardId, out WorkshopRewardDefinition reward))
                {
                    controller.SetStatusMessage($"Victory over {FormatBossName(result)}. Reward applied: {reward.DisplayName}.");
                    return;
                }

                if (!string.IsNullOrWhiteSpace(result.DefeatRewardId))
                {
                    controller.SetStatusMessage($"Victory over {FormatBossName(result)}. Reward '{result.DefeatRewardId}' was not found.");
                    return;
                }

                controller.SetStatusMessage($"Victory over {FormatBossName(result)}. No battle reward is configured yet.");
                return;
            }

            if (result.ResultType == BattleResultType.Defeat)
            {
                controller.SetStatusMessage($"Defeated by {FormatBossName(result)}. Rebuild your deck and try again.");
                return;
            }

            controller.SetStatusMessage("Returned from battle.");
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
