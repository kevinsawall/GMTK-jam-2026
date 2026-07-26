using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum CupTimerMode
{
    Sec,
    Natural
}

/// <summary>Runs the gameplay countdown and pauses it while dialogue is visible.</summary>
public sealed class CupTimerController : MonoBehaviour
{
    private const string TimerObjectName = "Cup timer";
    private const string CutsceneObjectName = "CutsceneObject";
    private const float CutsceneDurationSeconds = 6f;
    private const float PlayerResetTimeSeconds = 2f;
    private const float CutsceneFadeOutSeconds = 1f;

    [SerializeField] private CupTimerMode timerMode = CupTimerMode.Sec;
    [SerializeField, Min(1f)] private float DurationSeconds = 60f;
    [Header("Player Phrases")]
    [SerializeField, TextArea(2, 5)] private List<string> playerFirstGameStartPhrases = new();
    [SerializeField, TextArea(2, 5)] private List<string> playerStartPhrases = new();
    [SerializeField, TextArea(2, 5)] private List<string> playerEndPhrases = new();
    [Header("Natural Mode Display")]
    [SerializeField] private List<Sprite> naturalStageSprites = new();
    [SerializeField] private Sprite naturalEmptySprite;
    [SerializeField, Min(0f)] private float naturalSpriteStepDuration = 0.1f;

    public static CupTimerController Instance { get; private set; }
    public bool IsCutscenePlaying { get; private set; }
    public bool IsRestartSequencePlaying => hasExpired;

    private TMP_Text timerText;
    private Image timerImage;
    private CanvasGroup canvasGroup;
    private GameObject cutsceneObject;
    private CanvasGroup cutsceneCanvasGroup;
    private float remainingSeconds;
    private bool hasExpired;
    private bool hasSetTimerVisibility;
    private bool isTimerVisible;
    private int nextStartPhraseIndex;
    private int nextEndPhraseIndex;
    private Coroutine timeoutSequence;
    private Coroutine naturalCountAnimation;

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
        timerImage = GetComponent<Image>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        remainingSeconds = DurationSeconds;
        UpdateTimerDisplay();
        DialogueManager.NaturalCounterActionPerformed += OnNaturalCounterActionPerformed;
        CutsceneController.StartGameFinished += ShowFirstGameStartPhrases;
    }

    private void OnDestroy()
    {
        DialogueManager.NaturalCounterActionPerformed -= OnNaturalCounterActionPerformed;
        CutsceneController.StartGameFinished -= ShowFirstGameStartPhrases;
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        bool gameplayHasStarted = !CutsceneController.IsStartGamePlaying && !GameManager.IsEndGameSequencePlaying;
        bool isModalUiVisible = IsModalUiVisible();
        bool canCountDown = gameplayHasStarted && !isModalUiVisible && !IsCutscenePlaying;
        SetVisibility(gameplayHasStarted && !IsCutscenePlaying);
        if (!canCountDown || hasExpired || timerMode != CupTimerMode.Sec) return;

        ConsumeCount(Time.deltaTime);
    }

    private void OnNaturalCounterActionPerformed()
    {
        if (timerMode != CupTimerMode.Natural || hasExpired || naturalCountAnimation != null) return;

        int stageCount = Mathf.CeilToInt(remainingSeconds);
        if (stageCount <= 0) return;

        remainingSeconds = Mathf.Max(0f, remainingSeconds - 1f);
        naturalCountAnimation = StartCoroutine(AnimateNaturalCountDown(stageCount));
    }

    private void ConsumeCount(float amount)
    {
        if (hasExpired || GameManager.IsEndGameSequencePlaying) return;

        remainingSeconds = Mathf.Max(0f, remainingSeconds - amount);
        UpdateTimerDisplay();
        if (remainingSeconds > 0f) return;

        hasExpired = true;
        timeoutSequence = StartCoroutine(RunTimeoutSequence());
    }

    /// <summary>Stops the cup-timer timeout flow so a higher-priority ending can take over.</summary>
    public void CancelForEndGame()
    {
        if (timeoutSequence != null) StopCoroutine(timeoutSequence);
        timeoutSequence = null;
        hasExpired = false;
        IsCutscenePlaying = false;
        StopCameraShake();

        if (cutsceneObject != null) cutsceneObject.SetActive(false);
        SetVisibility(false);
    }

    private void SetVisibility(bool isVisible)
    {
        if (hasSetTimerVisibility && isTimerVisible == isVisible) return;

        hasSetTimerVisibility = true;
        isTimerVisible = isVisible;
        canvasGroup.alpha = isVisible ? 1f : 0f;
        canvasGroup.interactable = isVisible;
        canvasGroup.blocksRaycasts = isVisible;
    }

    private void UpdateTimerDisplay()
    {
        if (timerMode == CupTimerMode.Natural && HasNaturalStageSprites())
        {
            if (timerText != null) timerText.gameObject.SetActive(false);
            SetNaturalStageSprite(Mathf.CeilToInt(remainingSeconds), 4);
            return;
        }

        if (timerText != null)
        {
            timerText.gameObject.SetActive(true);
            timerText.text = Mathf.CeilToInt(remainingSeconds).ToString();
        }
    }

    private System.Collections.IEnumerator AnimateNaturalCountDown(int stageCount)
    {
        // Each stage drains from its full image (for example, 3_4) to 3_1
        // before the next full stage image (2_4) appears.
        for (int frame = 4; frame >= 1; frame--)
        {
            SetNaturalStageSprite(stageCount, frame);
            yield return new WaitForSecondsRealtime(naturalSpriteStepDuration);
        }

        if (remainingSeconds > 0f)
        {
            SetNaturalStageSprite(Mathf.CeilToInt(remainingSeconds), 4);
        }
        else
        {
            timerImage.sprite = naturalEmptySprite;
            hasExpired = true;
            timeoutSequence = StartCoroutine(RunTimeoutSequence());
        }

        naturalCountAnimation = null;
    }

    private bool HasNaturalStageSprites()
    {
        return timerImage != null && naturalStageSprites != null && naturalStageSprites.Count >= 12;
    }

    private void SetNaturalStageSprite(int stageCount, int frame)
    {
        if (!HasNaturalStageSprites()) return;

        int spriteIndex = (stageCount - 1) * 4 + (frame - 1);
        if (spriteIndex < 0 || spriteIndex >= naturalStageSprites.Count) return;

        timerImage.sprite = naturalStageSprites[spriteIndex];
    }

    private bool IsModalUiVisible()
    {
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsOpen) return true;

        return false;
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
        AudioManager.Instance?.PlayLoopingSfx(SfxId.TrainOnTheRun);
        UpdateTimerDisplay();
        ShowNextStartPhrase();
    }

    private System.Collections.IEnumerator RunTimeoutSequence()
    {
        while (DialogueManager.Instance != null && DialogueManager.Instance.IsOpen)
        {
            yield return null;
        }

        // Let the closed dialogue panel finish its frame before the timeout UI starts.
        yield return null;

        StartCameraShake();
        yield return new WaitForSecondsRealtime(1.5f);

        if (ShowNextEndPhrase())
        {
            while (DialogueManager.Instance != null && DialogueManager.Instance.IsOpen)
            {
                yield return null;
            }

            AudioManager.Instance?.PlaySfx(SfxId.TrainDestructionShort);
        }

        yield return PlayCutsceneAndRestart();
        timeoutSequence = null;
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
        CameraFollowPlayer cameraFollow = Object.FindFirstObjectByType<CameraFollowPlayer>(FindObjectsInactive.Exclude);
        cameraFollow?.StartPseudoResetShake();
    }

    private static void StopCameraShake()
    {
        CameraFollowPlayer cameraFollow = Object.FindFirstObjectByType<CameraFollowPlayer>(FindObjectsInactive.Exclude);
        cameraFollow?.StopEventShake();
    }

    private void ShowFirstGameStartPhrases()
    {
        DialogueManager manager = DialogueManager.Instance ??
            Object.FindFirstObjectByType<DialogueManager>(FindObjectsInactive.Include);
        manager?.ShowPlayerPhrases(playerFirstGameStartPhrases);
    }

    private void ShowNextStartPhrase()
    {
        if (!TryGetNextPhrase(playerStartPhrases, ref nextStartPhraseIndex, out string phrase)) return;

        DialogueManager manager = DialogueManager.Instance ??
            Object.FindFirstObjectByType<DialogueManager>(FindObjectsInactive.Include);
        manager?.ShowPlayerPhrase(phrase);
    }

    private bool ShowNextEndPhrase()
    {
        if (!TryGetNextPhrase(playerEndPhrases, ref nextEndPhraseIndex, out string phrase)) return false;

        DialogueManager manager = DialogueManager.Instance ??
            Object.FindFirstObjectByType<DialogueManager>(FindObjectsInactive.Include);
        if (manager == null) return false;

        manager.ShowPlayerPhrase(phrase);
        return true;
    }

    private static bool TryGetNextPhrase(List<string> phrases, ref int nextPhraseIndex, out string phrase)
    {
        phrase = null;
        if (phrases == null || phrases.Count == 0) return false;

        for (int attempt = 0; attempt < phrases.Count; attempt++)
        {
            nextPhraseIndex %= phrases.Count;
            string candidate = phrases[nextPhraseIndex++];
            if (string.IsNullOrWhiteSpace(candidate)) continue;

            phrase = candidate;
            return true;
        }

        return false;
    }
}
