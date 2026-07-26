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

    private void Start()
    {
        AudioManager.Instance?.PlayLoopingSfx(SfxId.TrainOnTheRun);
    }

    public void PlayAgain()
    {
        AudioManager.Instance?.StopLoopingSfx(SfxId.TrainOnTheRun);
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
