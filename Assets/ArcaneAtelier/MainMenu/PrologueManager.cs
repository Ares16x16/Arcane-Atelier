using ArcaneAtelier.Battle;
using ArcaneAtelier.Workshop;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class Prologuemanager : MonoBehaviour
{
    public void EnterWorkshop()
    {
        SceneManager.LoadScene("WorkshopScene");
    }

}