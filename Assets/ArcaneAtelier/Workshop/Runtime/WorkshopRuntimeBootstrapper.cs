using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArcaneAtelier.Workshop
{
    public static class WorkshopRuntimeBootstrapper
    {
        private const string WorkshopSceneName = "WorkshopScene";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsurePlayableWorkshopInUntitledScene()
        {
            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.name != WorkshopSceneName)
            {
                return;
            }

            if (Object.FindAnyObjectByType<WorkshopSceneController>() != null)
            {
                return;
            }

            var root = new GameObject("Spell Assembly Root");
            root.AddComponent<WorkshopGridView>();
            root.AddComponent<WorkshopHudPresenter>();
            root.AddComponent<WorkshopSceneController>();
        }
    }
}
