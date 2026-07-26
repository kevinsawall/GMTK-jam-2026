using UnityEngine;

public sealed class ObjectController : MonoBehaviour, IInteractable
{
    [SerializeField] private InteractObject interactObject;
    [SerializeField, Min(1)] private int interactionDistance = 1;
    [Header("Flag Trigger")]
    [SerializeField] private string deactivateOnFlag;
    [Header("Linked Interaction")]
    [SerializeField] private ObjectController linkedObjectController;
    [SerializeField] private string disableInteractionOnFlag;
    [Header("Hover Outline")]
    [SerializeField] private Material hoverOutlineMaterial;

    private int nextInspectPhraseIndex;
    private Renderer[] outlineRenderers;
    private Material[][] originalMaterials;
    private bool isHovered;
    private bool isHoverOutlineVisible;
    private bool isInteractionDisabled;

    public InteractObject InteractObject => interactObject;
    public bool HasInteraction => interactObject != null && !isInteractionDisabled;
    public int InteractionDistance => interactionDistance;

    private void Awake()
    {
        CacheOutlineRenderers();
    }

    private void OnEnable()
    {
        DialogueManager.FlagSet += HandleFlagSet;
        if (ShouldDeactivateForFlag())
        {
            gameObject.SetActive(false);
            return;
        }

        if (ShouldDisableInteractionForFlag())
        {
            DisableInteraction();
        }

        if (outlineRenderers == null) return;

        DialogueManager.DialogueStarted += RefreshHoverOutline;
        DialogueManager.DialogueClosed += RefreshHoverOutline;
        DialogueManager.PlayerDialogueStarted += RefreshHoverOutline;
        DialogueManager.PlayerDialogueClosed += RefreshHoverOutline;
        PauseMenuController.PauseStateChanged += RefreshHoverOutline;
        CutsceneController.StartGameStateChanged += RefreshHoverOutline;
        RefreshHoverOutline();
    }

    private void OnMouseEnter()
    {
        isHovered = true;
        RefreshHoverOutline();
    }

    private void OnMouseExit()
    {
        isHovered = false;
        RefreshHoverOutline();
    }

    private void OnDisable()
    {
        DialogueManager.FlagSet -= HandleFlagSet;
        if (outlineRenderers == null) return;

        DialogueManager.DialogueStarted -= RefreshHoverOutline;
        DialogueManager.DialogueClosed -= RefreshHoverOutline;
        DialogueManager.PlayerDialogueStarted -= RefreshHoverOutline;
        DialogueManager.PlayerDialogueClosed -= RefreshHoverOutline;
        PauseMenuController.PauseStateChanged -= RefreshHoverOutline;
        CutsceneController.StartGameStateChanged -= RefreshHoverOutline;
        SetHoverOutlineVisible(false);
    }

    private void HandleFlagSet(string flag)
    {
        if (!string.IsNullOrWhiteSpace(deactivateOnFlag) && flag == deactivateOnFlag)
        {
            gameObject.SetActive(false);
            return;
        }

        if (!string.IsNullOrWhiteSpace(disableInteractionOnFlag) && flag == disableInteractionOnFlag)
        {
            DisableInteraction();
        }
    }

    private bool ShouldDeactivateForFlag()
    {
        return !string.IsNullOrWhiteSpace(deactivateOnFlag) &&
               DialogueManager.Instance != null &&
               DialogueManager.Instance.HasFlag(deactivateOnFlag);
    }

    private bool ShouldDisableInteractionForFlag()
    {
        return !string.IsNullOrWhiteSpace(disableInteractionOnFlag) &&
               DialogueManager.Instance != null &&
               DialogueManager.Instance.HasFlag(disableInteractionOnFlag);
    }

    private void CacheOutlineRenderers()
    {
        if (hoverOutlineMaterial == null) return;

        outlineRenderers = GetComponentsInChildren<Renderer>();
        originalMaterials = new Material[outlineRenderers.Length][];
        for (int i = 0; i < outlineRenderers.Length; i++)
        {
            originalMaterials[i] = outlineRenderers[i].sharedMaterials;
        }
    }

    private void RefreshHoverOutline()
    {
        bool isBlockedByModal = DialogueManager.Instance?.IsOpen == true ||
                                PauseMenuController.IsPaused ||
                                CutsceneController.IsStartGamePlaying;
        SetHoverOutlineVisible(isHovered && !isBlockedByModal);
    }

    private void RefreshHoverOutline(NpcDialogueSO _) => RefreshHoverOutline();

    private void RefreshHoverOutline(NpcDialogueSO _, DialogueState __) => RefreshHoverOutline();

    private void RefreshHoverOutline(bool _) => RefreshHoverOutline();

    private void SetHoverOutlineVisible(bool isVisible)
    {
        if (isHoverOutlineVisible == isVisible || outlineRenderers == null) return;

        isHoverOutlineVisible = isVisible;
        for (int i = 0; i < outlineRenderers.Length; i++)
        {
            Renderer renderer = outlineRenderers[i];
            if (renderer == null) continue;

            if (!isVisible)
            {
                renderer.sharedMaterials = originalMaterials[i];
                continue;
            }

            Material[] outlinedMaterials = new Material[originalMaterials[i].Length + 1];
            originalMaterials[i].CopyTo(outlinedMaterials, 0);
            outlinedMaterials[^1] = hoverOutlineMaterial;
            renderer.sharedMaterials = outlinedMaterials;
        }
    }

    public void Interact()
    {
        if (!isInteractionDisabled && interactObject != null)
        {
            interactObject.Interact(this);
        }
    }

    public bool TryReceiveItem(ItemData item)
    {
        return !isInteractionDisabled && interactObject != null && interactObject.TryReceiveItem(item, this);
    }

    /// <summary>Stops this object and its linked partner from receiving any further interactions.</summary>
    public void DisableInteraction()
    {
        if (isInteractionDisabled) return;

        isInteractionDisabled = true;
        isHovered = false;
        SetHoverOutlineVisible(false);
        linkedObjectController?.DisableInteraction();
    }

    /// <summary>Returns this object's next inspect phrase index and advances it, looping at the end.</summary>
    public int GetNextInspectPhraseIndex(int phraseCount)
    {
        if (phraseCount <= 0) return 0;

        int phraseIndex = nextInspectPhraseIndex % phraseCount;
        nextInspectPhraseIndex = (phraseIndex + 1) % phraseCount;
        return phraseIndex;
    }
}
