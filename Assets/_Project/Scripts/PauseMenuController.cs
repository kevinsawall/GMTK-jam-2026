using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class PauseMenuController : MonoBehaviour
{
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private string mainMenuSceneName = "01_MainMenu";
    [SerializeField] private string endScreenSceneName = "03_EndScreen";

    private void Awake()
    {
        BindOptionsBackButton();
        SetPaused(false);
    }

    public void PauseGame()
    {
        if (StartCutsceneController.IsPlaying || CupTimerController.Instance?.IsRestartSequencePlaying == true) return;
        SetPaused(true);
    }

    public void ResumeGame()
    {
        SetPaused(false);
    }

    public void TogglePause()
    {
        if (StartCutsceneController.IsPlaying || CupTimerController.Instance?.IsRestartSequencePlaying == true) return;

        if (optionsPanel.activeSelf)
        {
            CloseOptions();
            return;
        }

        SetPaused(!pauseMenuPanel.activeSelf);
    }

    public void ShowOptions()
    {
        pauseMenuPanel.SetActive(false);
        optionsPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void CloseOptions()
    {
        SetPaused(true);
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneLoader.Load(SceneManager.GetActiveScene().name);
    }

    public void ReturnToMainMenu()
    {
        SetPaused(false);
        SceneLoader.Load(mainMenuSceneName);
    }

    public void EndGame()
    {
        Time.timeScale = 1f;
        SceneLoader.Load(endScreenSceneName);
    }

    private void SetPaused(bool isPaused)
    {
        pauseMenuPanel.SetActive(isPaused);
        optionsPanel.SetActive(false);
        Time.timeScale = isPaused ? 0f : 1f;
    }

    private void BindOptionsBackButton()
    {
        Button fallbackButton = null;

        foreach (Button button in optionsPanel.GetComponentsInChildren<Button>(true))
        {
            if (fallbackButton == null)
            {
                fallbackButton = button;
            }

            if (button.name == "OptionsBackButton")
            {
                button.onClick.AddListener(CloseOptions);
                return;
            }
        }

        if (fallbackButton != null)
        {
            fallbackButton.onClick.AddListener(CloseOptions);
            return;
        }

        Debug.LogWarning("No Back button was found in the Options panel.", optionsPanel);
    }
}
