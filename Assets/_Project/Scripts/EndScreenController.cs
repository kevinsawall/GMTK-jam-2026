using UnityEngine;

public sealed class EndScreenController : MonoBehaviour
{
    [SerializeField] private GameObject endScreenPanel;
    [SerializeField] private GameObject creditsPanel;
    [SerializeField] private string gameplaySceneName = "02_Gameplay";
    [SerializeField] private string mainMenuSceneName = "01_MainMenu";

    private void Awake()
    {
        Time.timeScale = 1f;
        ShowEndScreen();
    }

    public void PlayAgain()
    {
        SceneLoader.Load(gameplaySceneName);
    }

    public void ReturnToMainMenu()
    {
        SceneLoader.Load(mainMenuSceneName);
    }

    public void ShowCredits()
    {
        endScreenPanel.SetActive(false);
        creditsPanel.SetActive(true);
    }

    public void ShowEndScreen()
    {
        endScreenPanel.SetActive(true);
        creditsPanel.SetActive(false);
    }
}
