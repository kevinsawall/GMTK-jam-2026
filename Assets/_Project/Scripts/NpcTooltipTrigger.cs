using System.Collections;
using UnityEngine;

public enum NpcTooltipDisplayMode
{
    Never,
    Always,
    AfterFirstDialogue,
    AfterFirstPlayerDialogue
}

public enum NpcTooltipHideCondition
{
    None
}

/// <summary>
/// Controls an NPC's world-space tooltip from dialogue lifecycle events.
/// </summary>
[RequireComponent(typeof(Canvas))]
public sealed class NpcTooltipTrigger : MonoBehaviour
{
    [SerializeField] private NpcTooltipDisplayMode displayMode = NpcTooltipDisplayMode.AfterFirstDialogue;
    [SerializeField] private NpcTooltipHideCondition hideCondition = NpcTooltipHideCondition.None;
    [SerializeField, Min(0f)] private float showDelay = 0.5f;
    [SerializeField] private Canvas tooltipCanvas;
    [SerializeField] private CharacterManager characterManager;

    private NpcDialogueSO dialogue;
    private bool hasFinishedFirstDialogue;
    private bool hasFinishedFirstPlayerDialogue;
    private Coroutine delayedShow;

    private void Awake()
    {
        tooltipCanvas ??= GetComponent<Canvas>();
        characterManager ??= GetComponentInParent<CharacterManager>();
        dialogue = (characterManager?.InteractObject as TalkInteractObject)?.Dialogue;

        RefreshVisibility();
    }

    private void OnEnable()
    {
        DialogueManager.DialogueStarted += HandleDialogueStarted;
        DialogueManager.DialogueClosed += HandleDialogueClosed;
        DialogueManager.PlayerDialogueStarted += HandlePlayerDialogueStarted;
        DialogueManager.PlayerDialogueClosed += HandlePlayerDialogueClosed;
    }

    private void OnDisable()
    {
        DialogueManager.DialogueStarted -= HandleDialogueStarted;
        DialogueManager.DialogueClosed -= HandleDialogueClosed;
        DialogueManager.PlayerDialogueStarted -= HandlePlayerDialogueStarted;
        DialogueManager.PlayerDialogueClosed -= HandlePlayerDialogueClosed;
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

    private void RefreshVisibility(bool delayed = false)
    {
        bool shouldShow = displayMode == NpcTooltipDisplayMode.Always ||
                          (displayMode == NpcTooltipDisplayMode.AfterFirstDialogue && hasFinishedFirstDialogue) ||
                          (displayMode == NpcTooltipDisplayMode.AfterFirstPlayerDialogue && hasFinishedFirstPlayerDialogue);

        if (!shouldShow)
        {
            HideImmediately();
            return;
        }

        if (!delayed || showDelay <= 0f)
        {
            SetVisible(true);
            return;
        }

        if (delayedShow == null)
        {
            delayedShow = StartCoroutine(ShowAfterDelay());
        }
    }

    private IEnumerator ShowAfterDelay()
    {
        yield return new WaitForSeconds(showDelay);
        delayedShow = null;
        SetVisible(true);
    }

    private void HideImmediately()
    {
        if (delayedShow != null)
        {
            StopCoroutine(delayedShow);
            delayedShow = null;
        }

        SetVisible(false);
    }

    private void SetVisible(bool isVisible)
    {
        if (tooltipCanvas != null)
        {
            tooltipCanvas.enabled = isVisible;
        }
    }
}
