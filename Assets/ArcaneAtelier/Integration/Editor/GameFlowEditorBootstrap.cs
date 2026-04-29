using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ArcaneAtelier.Integration.Editor
{
    [InitializeOnLoad]
    public static class GameFlowEditorBootstrap
    {
        private const string MainMenuScenePath = "Assets/Scenes/MainMenuScene.unity";

        static GameFlowEditorBootstrap()
        {
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
            EditorApplication.delayCall += ConfigurePlayModeStartScene;
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode)
            {
                return;
            }

            ConfigurePlayModeStartScene();
        }

        private static void ConfigurePlayModeStartScene()
        {
            if (Application.isBatchMode)
            {
                return;
            }

            SceneAsset mainMenuScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuScenePath);
            if (mainMenuScene == null)
            {
                Debug.LogWarning($"GameFlowEditorBootstrap: could not find '{MainMenuScenePath}'.");
                EditorSceneManager.playModeStartScene = null;
                return;
            }

            EditorSceneManager.playModeStartScene = mainMenuScene;
        }
    }
}
