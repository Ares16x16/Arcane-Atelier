using ArcaneAtelier.Battle;
using ArcaneAtelier.Workshop;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class MainMenuManager : MonoBehaviour
{
    private const string WorkshopSceneName = "WorkshopScene";

    public void StartGame()
    {
        WorkshopBattlePayloadBridge.Clear();
        BattleResultBridge.Clear();
        SceneManager.LoadScene(WorkshopSceneName);
    }

    public void QuitGame()
    {
        Debug.Log("Exiting Game...");
        Application.Quit();
    }
}
