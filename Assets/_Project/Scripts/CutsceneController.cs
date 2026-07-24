using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum CutsceneType
{
    StartGame,
    EndGame,
    Generic
}

/// <summary>Configurable full-screen cutscene with an optional title and fade-out.</summary>
public sealed class CutsceneController : MonoBehaviour
{
    [Header("Cutscene")]
    [SerializeField] private CutsceneType cutsceneType = CutsceneType.Generic;
    [SerializeField, Min(0.01f)] private float totalDurationSeconds = 5f;
    [SerializeField, Min(0f)] private float fadeOutSeconds = 1f;

    [Header("Title")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField, TextArea(2, 5)] private string titleTextOverride;
    [SerializeField, Min(0f)] private float titleAppearDelaySeconds = 1f;
    [SerializeField, Min(0f)] private float titleFadeInSeconds = 1f;
    [SerializeField, Min(0f)] private float titleVisibleSeconds = 3f;
    [SerializeField, Min(0f)] private float titleFadeOutSeconds;

    private static bool isStartGamePlaying;
    private CanvasGroup canvasGroup;
    private CanvasGroup titleCanvasGroup;
    private Image backgroundImage;
    private Color backgroundColor;
    private bool isPlaying;
    private bool hasFinished;

    public static bool IsStartGamePlaying => isStartGamePlaying;
    public static event Action StartGameFinished;
    public static event Action<bool> StartGameStateChanged;
    public bool IsPlaying => isPlaying;
    public CutsceneType Type => cutsceneType;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        backgroundImage = GetComponent<Image>();
        if (backgroundImage != null)
        {
            backgroundColor = backgroundImage.color;
            SetBackgroundAlpha(1f);
        }

        if (titleText == null) titleText = GetComponentInChildren<TMP_Text>(true);
        if (titleText != null)
        {
            if (!string.IsNullOrWhiteSpace(titleTextOverride)) titleText.text = titleTextOverride;
            titleCanvasGroup = titleText.GetComponent<CanvasGroup>();
            if (titleCanvasGroup == null) titleCanvasGroup = titleText.gameObject.AddComponent<CanvasGroup>();
            titleCanvasGroup.alpha = 0f;
        }

    }

    private void OnEnable()
    {
        hasFinished = false;
        isPlaying = true;
        if (cutsceneType == CutsceneType.StartGame)
        {
            isStartGamePlaying = true;
            StartGameStateChanged?.Invoke(true);
        }

        SetBackgroundAlpha(1f);
        if (titleCanvasGroup != null) titleCanvasGroup.alpha = 0f;
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        StartCoroutine(PlayCutscene());
    }

    private void OnDisable()
    {
        isPlaying = false;
        if (!hasFinished && cutsceneType == CutsceneType.StartGame)
        {
            isStartGamePlaying = false;
            StartGameStateChanged?.Invoke(false);
        }
    }

    private IEnumerator PlayCutscene()
    {
        float startedAt = Time.realtimeSinceStartup;
        if (titleCanvasGroup != null)
        {
            yield return new WaitForSecondsRealtime(titleAppearDelaySeconds);
            FadeTitleIn();
            StartCoroutine(HideTitleAfterDisplay());
        }

        float fadeStartTime = Mathf.Max(0f, totalDurationSeconds - fadeOutSeconds);
        float elapsedBeforeFade = Time.realtimeSinceStartup - startedAt;
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, fadeStartTime - elapsedBeforeFade));

        FadeBackgroundOut();
        float elapsedBeforeFinish = Time.realtimeSinceStartup - startedAt;
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, totalDurationSeconds - elapsedBeforeFinish));
        Finish();
    }

    private void FadeTitleIn()
    {
        if (titleFadeInSeconds <= 0f)
        {
            titleCanvasGroup.alpha = 1f;
            return;
        }

        LeanTween.alphaCanvas(titleCanvasGroup, 1f, titleFadeInSeconds).setIgnoreTimeScale(true);
    }

    private IEnumerator HideTitleAfterDisplay()
    {
        yield return new WaitForSecondsRealtime(titleFadeInSeconds + titleVisibleSeconds);
        if (titleCanvasGroup == null) yield break;

        if (titleFadeOutSeconds <= 0f)
        {
            titleCanvasGroup.alpha = 0f;
            yield break;
        }

        LeanTween.alphaCanvas(titleCanvasGroup, 0f, titleFadeOutSeconds).setIgnoreTimeScale(true);
    }

    private void FadeBackgroundOut()
    {
        if (backgroundImage == null || fadeOutSeconds <= 0f)
        {
            SetBackgroundAlpha(0f);
            return;
        }

        LeanTween.value(gameObject, SetBackgroundAlpha, 1f, 0f, fadeOutSeconds).setIgnoreTimeScale(true);
    }

    private void SetBackgroundAlpha(float alpha)
    {
        if (backgroundImage == null) return;

        backgroundColor.a = alpha;
        backgroundImage.color = backgroundColor;
    }

    private void Finish()
    {
        if (hasFinished) return;

        hasFinished = true;
        isPlaying = false;
        if (cutsceneType == CutsceneType.StartGame)
        {
            isStartGamePlaying = false;
            StartGameStateChanged?.Invoke(false);
            StartGameFinished?.Invoke();
        }

        gameObject.SetActive(false);
    }
}
