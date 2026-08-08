using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Adds shared hover and click audio to Unity UI buttons.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public sealed class UIButtonAudioFeedback : MonoBehaviour, IPointerEnterHandler
{
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        if (button == null) button = GetComponent<Button>();
        button.onClick.AddListener(PlayClick);
    }

    private void OnDisable()
    {
        if (button != null) button.onClick.RemoveListener(PlayClick);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!button.interactable || IsContinueButton()) return;

        AudioManager.Instance?.PlaySfx(SfxId.HoverOverButton);
    }

    private void PlayClick()
    {
        AudioManager.Instance?.PlaySfx(SfxId.ClickOnContinueAndMenuButtons);
    }

    private bool IsContinueButton() => gameObject.name == "ContinueButton";
}

/// <summary>Installs UI audio feedback for all buttons in every loaded game scene.</summary>
public sealed class UIButtonAudioFeedbackInstaller : MonoBehaviour
{
    private static UIButtonAudioFeedbackInstaller instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        if (instance != null) return;

        GameObject installerObject = new GameObject(nameof(UIButtonAudioFeedbackInstaller));
        instance = installerObject.AddComponent<UIButtonAudioFeedbackInstaller>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += HandleSceneLoaded;
        InstallForLoadedButtons();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        if (instance == this) instance = null;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        InstallForLoadedButtons();
    }

    private static void InstallForLoadedButtons()
    {
        foreach (Button button in Resources.FindObjectsOfTypeAll<Button>())
        {
            if (!button.gameObject.scene.IsValid() ||
                button.GetComponent<UIButtonAudioFeedback>() != null)
            {
                continue;
            }

            button.gameObject.AddComponent<UIButtonAudioFeedback>();
        }
    }
}
