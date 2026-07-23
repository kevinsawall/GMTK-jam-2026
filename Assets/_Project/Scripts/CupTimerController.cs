using System.Collections.Generic;
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
    private const float CutsceneDurationSeconds = 4f;
    private const float PlayerResetTimeSeconds = 2f;
    private const float CutsceneFadeOutSeconds = 1f;

    [SerializeField] private CupTimerMode timerMode = CupTimerMode.Sec;
    [SerializeField, Min(1f)] private float DurationSeconds = 60f;
    [SerializeField, TextArea(2, 5)] private List<string> playerStartPhrases = new();
    [SerializeField, TextArea(2, 5)] private List<string> playerEndPhrases = new();

    public static CupTimerController Instance { get; private set; }
    public bool IsCutscenePlaying { get; private set; }
    public bool IsRestartSequencePlaying => hasExpired;

    private TMP_Text timerText;
    private CanvasGroup canvasGroup;
    private GameObject cutsceneObject;
    private CanvasGroup cutsceneCanvasGroup;
    private float remainingSeconds;
    private bool hasExpired;
    private int nextStartPhraseIndex;
    private int nextEndPhraseIndex;

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
        StartCutsceneController.Finished += ShowNextStartPhrase;
    }

    private void OnDestroy()
    {
        DialogueManager.DialogueStarted -= OnDialogueStarted;
        StartCutsceneController.Finished -= ShowNextStartPhrase;
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        bool gameplayHasStarted = !StartCutsceneController.IsPlaying;
        bool isModalUiVisible = IsModalUiVisible();
        bool canCountDown = gameplayHasStarted && !isModalUiVisible && !IsCutscenePlaying;
        SetVisibility(canCountDown && !hasExpired);
        if (!canCountDown || hasExpired || timerMode != CupTimerMode.Sec) return;

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
        StartCameraShake();
        StartCoroutine(RunTimeoutSequence());
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
        if (cutsceneObject != null)
        {
            cutsceneObject.SetActive(true);
            cutsceneCanvasGroup = cutsceneObject.GetComponent<CanvasGroup>();
            if (cutsceneCanvasGroup == null) cutsceneCanvasGroup = cutsceneObject.AddComponent<CanvasGroup>();
            cutsceneCanvasGroup.alpha = 1f;
        }

        yield return new WaitForSecondsRealtime(PlayerResetTimeSeconds);
        ResetPlayerToStartPosition();
        StopCameraShake();

        yield return new WaitForSecondsRealtime(CutsceneDurationSeconds - PlayerResetTimeSeconds - CutsceneFadeOutSeconds);

        if (cutsceneCanvasGroup != null)
        {
            LeanTween.alphaCanvas(cutsceneCanvasGroup, 0f, CutsceneFadeOutSeconds).setIgnoreTimeScale(true);
        }

        yield return new WaitForSecondsRealtime(CutsceneFadeOutSeconds);

        if (cutsceneObject != null) cutsceneObject.SetActive(false);

        remainingSeconds = DurationSeconds;
        hasExpired = false;
        IsCutscenePlaying = false;
        UpdateTimerText();
        ShowNextStartPhrase();
    }

    private System.Collections.IEnumerator RunTimeoutSequence()
    {
        while (DialogueManager.Instance != null && DialogueManager.Instance.IsOpen)
        {
            yield return null;
        }

        yield return new WaitForSecondsRealtime(1.5f);

        if (ShowNextEndPhrase())
        {
            while (DialogueManager.Instance != null && DialogueManager.Instance.IsOpen)
            {
                yield return null;
            }
        }

        yield return PlayCutsceneAndRestart();
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

    private static void ResetPlayerToStartPosition()
    {
        foreach (CharacterManager character in Object.FindObjectsByType<CharacterManager>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (character.Type != CharacterManager.CharacterType.Player) continue;

            character.ResetToStartPosition();
            return;
        }

        Debug.LogWarning("No player CharacterManager was found for the pseudo restart.");
    }

    private static void StartCameraShake()
    {
        PlayerCameraFollow cameraFollow = Object.FindFirstObjectByType<PlayerCameraFollow>(FindObjectsInactive.Exclude);
        cameraFollow?.StartHorizontalShake();
    }

    private static void StopCameraShake()
    {
        PlayerCameraFollow cameraFollow = Object.FindFirstObjectByType<PlayerCameraFollow>(FindObjectsInactive.Exclude);
        cameraFollow?.StopHorizontalShakeAndResumeFollow();
    }

    private void ShowNextStartPhrase()
    {
        if (playerStartPhrases == null || playerStartPhrases.Count == 0) return;

        for (int attempts = 0; attempts < playerStartPhrases.Count; attempts++)
        {
            string phrase = playerStartPhrases[nextStartPhraseIndex];
            nextStartPhraseIndex = (nextStartPhraseIndex + 1) % playerStartPhrases.Count;
            if (string.IsNullOrWhiteSpace(phrase)) continue;

            DialogueManager manager = DialogueManager.Instance ??
                Object.FindFirstObjectByType<DialogueManager>(FindObjectsInactive.Include);
            manager?.ShowPlayerPhrase(phrase);
            return;
        }
    }

    private bool ShowNextEndPhrase()
    {
        if (playerEndPhrases == null || playerEndPhrases.Count == 0) return false;

        for (int attempts = 0; attempts < playerEndPhrases.Count; attempts++)
        {
            string phrase = playerEndPhrases[nextEndPhraseIndex];
            nextEndPhraseIndex = (nextEndPhraseIndex + 1) % playerEndPhrases.Count;
            if (string.IsNullOrWhiteSpace(phrase)) continue;

            DialogueManager manager = DialogueManager.Instance ??
                Object.FindFirstObjectByType<DialogueManager>(FindObjectsInactive.Include);
            if (manager == null) return false;

            manager.ShowPlayerPhrase(phrase);
            return true;
        }

        return false;
    }
}
