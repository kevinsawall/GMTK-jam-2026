using UnityEngine;

public sealed class MainMenuController : MonoBehaviour
{
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject creditsPanel;
    [SerializeField] private string gameplaySceneName = "02_Gameplay";

    private void Awake()
    {
        ShowMainMenu();
    }

    public void ShowOptions()
    {
        SetActivePanel(optionsPanel);
    }

    public void ShowCredits()
    {
        SetActivePanel(creditsPanel);
    }

    public void ShowMainMenu()
    {
        SetActivePanel(null);
    }

    public void PlayGame()
    {
        SceneLoader.Load(gameplaySceneName);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    private void SetActivePanel(GameObject panelToShow)
    {
        optionsPanel.SetActive(panelToShow == optionsPanel);
        creditsPanel.SetActive(panelToShow == creditsPanel);
    }
}
