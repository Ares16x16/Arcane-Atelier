using ArcaneAtelier.Battle;
using ArcaneAtelier.Workshop;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class MainMenuManager : MonoBehaviour
{
    private const string PrologueSceneName = "PrologueScene";

    public void StartGame()
    {
        WorkshopBattlePayloadBridge.Clear();
        BattleResultBridge.Clear();
        RunProgressBridge.Reset();
        SceneManager.LoadScene(PrologueSceneName);
    }

    public void QuitGame()
    {
        Debug.Log("Exiting Game...");
        Application.Quit();
    }
}
