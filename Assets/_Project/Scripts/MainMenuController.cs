using UnityEngine;

public sealed class MainMenuController : MonoBehaviour
{
    [SerializeField] private GameObject mainPanel;
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
        if (mainPanel != null) mainPanel.SetActive(panelToShow == null);
        if (optionsPanel != null) optionsPanel.SetActive(panelToShow == optionsPanel);
        if (creditsPanel != null) creditsPanel.SetActive(panelToShow == creditsPanel);
    }
}
