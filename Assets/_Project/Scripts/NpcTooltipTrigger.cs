using System.Collections;
using UnityEngine;

public enum NpcTooltipDisplayMode
{
    Never,
    Always,
    AfterFirstDialogue,
    AfterFirstPlayerDialogue,
    AfterFlag
}

public enum NpcTooltipHideCondition
{
    None,
    EndGame
}

/// <summary>
/// Controls an NPC's world-space tooltip from dialogue lifecycle events.
/// </summary>
[RequireComponent(typeof(Canvas), typeof(CanvasGroup))]
public sealed class NpcTooltipTrigger : MonoBehaviour
{
    [SerializeField] private NpcTooltipDisplayMode displayMode = NpcTooltipDisplayMode.AfterFirstDialogue;
    [SerializeField] private string requiredFlag = "wake_conductor";
    [SerializeField] private NpcTooltipHideCondition hideCondition = NpcTooltipHideCondition.None;
    [SerializeField] private string endGameFlag = "end-game";
    [SerializeField, Min(0f)] private float showDelay = 0.5f;
    [SerializeField, Min(0f)] private float fadeInDuration = 0.25f;
    [SerializeField] private Canvas tooltipCanvas;
    [SerializeField] private CanvasGroup tooltipCanvasGroup;
    [SerializeField] private CharacterManager characterManager;

    private NpcDialogueSO dialogue;
    private bool hasFinishedFirstDialogue;
    private bool hasFinishedFirstPlayerDialogue;
    private bool isHiddenByCondition;
    private Coroutine delayedShow;

    private void Awake()
    {
        tooltipCanvas ??= GetComponent<Canvas>();
        tooltipCanvasGroup ??= GetComponent<CanvasGroup>();
        characterManager ??= GetComponentInParent<CharacterManager>();
        dialogue = (characterManager?.InteractObject as TalkInteractObject)?.Dialogue;

        RefreshVisibility();
    }

    private void Start()
    {
        RefreshVisibility();
    }

    private void OnEnable()
    {
        DialogueManager.DialogueStarted += HandleDialogueStarted;
        DialogueManager.DialogueClosed += HandleDialogueClosed;
        DialogueManager.PlayerDialogueStarted += HandlePlayerDialogueStarted;
        DialogueManager.PlayerDialogueClosed += HandlePlayerDialogueClosed;
        DialogueManager.FlagSet += HandleFlagSet;
    }

    private void OnDisable()
    {
        DialogueManager.DialogueStarted -= HandleDialogueStarted;
        DialogueManager.DialogueClosed -= HandleDialogueClosed;
        DialogueManager.PlayerDialogueStarted -= HandlePlayerDialogueStarted;
        DialogueManager.PlayerDialogueClosed -= HandlePlayerDialogueClosed;
        DialogueManager.FlagSet -= HandleFlagSet;
    }

    private void HandleDialogueStarted(NpcDialogueSO _)
    {
        HideImmediately();
    }

    private void HandleDialogueClosed(NpcDialogueSO closedDialogue, DialogueState _)
    {
        if (closedDialogue == dialogue)
        {
            hasFinishedFirstDialogue = true;
        }

        RefreshVisibility(delayed: true);
    }

    private void HandlePlayerDialogueStarted()
    {
        HideImmediately();
    }

    private void HandlePlayerDialogueClosed()
    {
        hasFinishedFirstPlayerDialogue = true;
        RefreshVisibility(delayed: true);
    }

    private void HandleFlagSet(string flag)
    {
        if (hideCondition == NpcTooltipHideCondition.EndGame && flag == endGameFlag)
        {
            isHiddenByCondition = true;
            HideImmediately();
            return;
        }

        if (displayMode == NpcTooltipDisplayMode.AfterFlag && flag == requiredFlag)
        {
            RefreshVisibility(delayed: true);
        }
    }

    private void RefreshVisibility(bool delayed = false)
    {
        if (ShouldBeHiddenByCondition())
        {
            isHiddenByCondition = true;
        }

        if (isHiddenByCondition)
        {
            HideImmediately();
            return;
        }

        bool shouldShow = displayMode == NpcTooltipDisplayMode.Always ||
                          (displayMode == NpcTooltipDisplayMode.AfterFirstDialogue && hasFinishedFirstDialogue) ||
                          (displayMode == NpcTooltipDisplayMode.AfterFirstPlayerDialogue && hasFinishedFirstPlayerDialogue) ||
                          (displayMode == NpcTooltipDisplayMode.AfterFlag &&
                           DialogueManager.Instance != null && DialogueManager.Instance.HasFlag(requiredFlag));

        if (!shouldShow)
        {
            HideImmediately();
            return;
        }

        if (!delayed || showDelay <= 0f)
        {
            ShowImmediately();
            return;
        }

        if (delayedShow == null)
        {
            delayedShow = StartCoroutine(ShowAfterDelay());
        }
    }

    private bool ShouldBeHiddenByCondition()
    {
        return hideCondition == NpcTooltipHideCondition.EndGame &&
               DialogueManager.Instance != null &&
               DialogueManager.Instance.HasFlag(endGameFlag);
    }

    private IEnumerator ShowAfterDelay()
    {
        yield return new WaitForSeconds(showDelay);
        delayedShow = null;
        FadeIn();
    }

    private void HideImmediately()
    {
        if (delayedShow != null)
        {
            StopCoroutine(delayedShow);
            delayedShow = null;
        }

        LeanTween.cancel(gameObject);
        if (tooltipCanvasGroup != null)
        {
            tooltipCanvasGroup.alpha = 0f;
        }

        SetVisible(false);
    }

    private void ShowImmediately()
    {
        LeanTween.cancel(gameObject);
        if (tooltipCanvasGroup != null)
        {
            tooltipCanvasGroup.alpha = 1f;
        }

        SetVisible(true);
    }

    private void FadeIn()
    {
        SetVisible(true);
        if (tooltipCanvasGroup == null || fadeInDuration <= 0f)
        {
            ShowImmediately();
            return;
        }

        tooltipCanvasGroup.alpha = 0f;
        LeanTween.alphaCanvas(tooltipCanvasGroup, 1f, fadeInDuration).setEaseOutQuad();
    }

    private void SetVisible(bool isVisible)
    {
        if (tooltipCanvas != null)
        {
            tooltipCanvas.enabled = isVisible;
        }
    }
}
