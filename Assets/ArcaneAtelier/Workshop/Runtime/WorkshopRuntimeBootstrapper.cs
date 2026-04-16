using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArcaneAtelier.Workshop
{
    public static class WorkshopRuntimeBootstrapper
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsurePlayableWorkshopInUntitledScene()
        {
            var activeScene = SceneManager.GetActiveScene();
            if (!string.IsNullOrEmpty(activeScene.path))
            {
                return;
            }

            if (Object.FindFirstObjectByType<WorkshopSceneController>() != null)
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
