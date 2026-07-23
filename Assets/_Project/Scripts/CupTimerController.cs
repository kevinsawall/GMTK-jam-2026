using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum CupTimerMode
{
    Sec,
    Natural
}

/// <summary>Runs the gameplay countdown and hides its UI while modal dialogue is visible.</summary>
public sealed class CupTimerController : MonoBehaviour
{
    private const string TimerObjectName = "Cup timer";
    

    [SerializeField] private CupTimerMode timerMode = CupTimerMode.Sec;
    [SerializeField] private float DurationSeconds = 60f;
    private TMP_Text timerText;
    private CanvasGroup canvasGroup;
    private float remainingSeconds;
    private bool hasExpired;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateForCupTimer()
    {
        foreach (Transform transform in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (transform.name != TimerObjectName || !transform.gameObject.scene.IsValid()) continue;
            if (transform.GetComponent<CupTimerController>() == null) transform.gameObject.AddComponent<CupTimerController>();
            return;
        }
    }

    private void Awake()
    {
        timerText = GetComponentInChildren<TMP_Text>(true);
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        remainingSeconds = DurationSeconds;
        UpdateTimerText();
        DialogueManager.DialogueStarted += OnDialogueStarted;
    }

    private void OnDestroy() => DialogueManager.DialogueStarted -= OnDialogueStarted;

    private void Update()
    {
        SetVisibility(!IsModalUiVisible());
        if (hasExpired || timerMode != CupTimerMode.Sec) return;

        ConsumeCount(Time.deltaTime);
    }

    private void OnDialogueStarted(DialogueState dialogueState)
    {
        if (timerMode != CupTimerMode.Natural || dialogueState is DialogueState.Summary or DialogueState.Completed) return;

        ConsumeCount(1f);
    }

    private void ConsumeCount(float amount)
    {
        if (hasExpired) return;

        remainingSeconds = Mathf.Max(0f, remainingSeconds - amount);
        UpdateTimerText();
        if (remainingSeconds > 0f) return;

        hasExpired = true;
        SceneLoader.Load(SceneManager.GetActiveScene().name);
    }

    private void SetVisibility(bool isVisible)
    {
        canvasGroup.alpha = isVisible ? 1f : 0f;
        canvasGroup.interactable = isVisible;
        canvasGroup.blocksRaycasts = isVisible;
    }

    private void UpdateTimerText()
    {
        if (timerText != null) timerText.text = Mathf.CeilToInt(remainingSeconds).ToString();
    }

    private static bool IsModalUiVisible()
    {
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsOpen) return true;

        ItemNotification notification = Object.FindFirstObjectByType<ItemNotification>(FindObjectsInactive.Include);
        return notification != null && notification.IsVisible;
    }
}
