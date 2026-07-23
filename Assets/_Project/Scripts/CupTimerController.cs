using TMPro;
using UnityEngine;

public enum CupTimerMode
{
    Sec,
    Natural
}

/// <summary>Runs the gameplay countdown and hides its UI while modal dialogue is visible.</summary>
public sealed class CupTimerController : MonoBehaviour
{
    private const string TimerObjectName = "Cup timer";
    private const string CutsceneObjectName = "CutsceneObject";
    private const float CutsceneDurationSeconds = 5f;

    [SerializeField] private CupTimerMode timerMode = CupTimerMode.Sec;
    [SerializeField, Min(1f)] private float DurationSeconds = 60f;

    public static CupTimerController Instance { get; private set; }
    public bool IsCutscenePlaying { get; private set; }

    private TMP_Text timerText;
    private CanvasGroup canvasGroup;
    private GameObject cutsceneObject;
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
        Instance = this;
        timerText = GetComponentInChildren<TMP_Text>(true);
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        remainingSeconds = DurationSeconds;
        UpdateTimerText();
        DialogueManager.DialogueStarted += OnDialogueStarted;
    }

    private void OnDestroy()
    {
        DialogueManager.DialogueStarted -= OnDialogueStarted;
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        bool gameplayHasStarted = !StartCutsceneController.IsPlaying;
        SetVisibility(gameplayHasStarted && !IsModalUiVisible() && !IsCutscenePlaying);
        if (!gameplayHasStarted || hasExpired || timerMode != CupTimerMode.Sec) return;

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
        StartCoroutine(PlayCutsceneAndRestart());
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

    private System.Collections.IEnumerator PlayCutsceneAndRestart()
    {
        IsCutscenePlaying = true;
        cutsceneObject = FindCutsceneObject();
        if (cutsceneObject != null) cutsceneObject.SetActive(true);

        yield return new WaitForSecondsRealtime(CutsceneDurationSeconds);

        if (cutsceneObject != null) cutsceneObject.SetActive(false);

        PlayerMovement playerMovement = Object.FindFirstObjectByType<PlayerMovement>();
        playerMovement?.ResetToStartPosition();

        remainingSeconds = DurationSeconds;
        hasExpired = false;
        IsCutscenePlaying = false;
        UpdateTimerText();
    }

    private static GameObject FindCutsceneObject()
    {
        foreach (Transform transform in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (transform.name == CutsceneObjectName && transform.gameObject.scene.IsValid()) return transform.gameObject;
        }

        Debug.LogWarning("No CutsceneObject was found in the scene.");
        return null;
    }
}
