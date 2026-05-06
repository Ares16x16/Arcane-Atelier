using UnityEngine;
using UnityEngine.SceneManagement;

public static class PrologueRuntimeBootstrapper
{
    private const string PrologueSceneName = "PrologueScene";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneCallbacks()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != PrologueSceneName)
        {
            return;
        }

        if (Object.FindAnyObjectByType<PrologueManager>() != null)
        {
            return;
        }

        GameObject root = new GameObject("Prologue Root");
        root.AddComponent<PrologueManager>();
    }
}
