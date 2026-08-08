using UnityEngine;

public sealed class ObjectController : MonoBehaviour, IInteractable
{
    [SerializeField] private InteractObject interactObject;
    [SerializeField, Min(1)] private int interactionDistance = 1;
    [Header("Interaction Collider")]
    [SerializeField] private bool fitBoxColliderToVisualBounds;
    [Header("Flag Trigger")]
    [SerializeField] private string deactivateOnFlag;
    [Header("Linked Interaction")]
    [SerializeField] private ObjectController linkedObjectController;
    [SerializeField] private string disableInteractionOnFlag;
    [SerializeField] private bool disableCollidersOnInteractionDisabled;
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
        FitBoxColliderToVisualBounds();
        CacheOutlineRenderers();
    }

    private void FitBoxColliderToVisualBounds()
    {
        if (!fitBoxColliderToVisualBounds || !TryGetComponent(out BoxCollider boxCollider)) return;

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds worldBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            worldBounds.Encapsulate(renderers[i].bounds);
        }

        Vector3 min = new(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        Vector3 max = new(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
        Vector3 worldMin = worldBounds.min;
        Vector3 worldMax = worldBounds.max;

        for (int x = 0; x <= 1; x++)
        for (int y = 0; y <= 1; y++)
        for (int z = 0; z <= 1; z++)
        {
            Vector3 localPoint = transform.InverseTransformPoint(new Vector3(
                x == 0 ? worldMin.x : worldMax.x,
                y == 0 ? worldMin.y : worldMax.y,
                z == 0 ? worldMin.z : worldMax.z));
            min = Vector3.Min(min, localPoint);
            max = Vector3.Max(max, localPoint);
        }

        boxCollider.center = (min + max) * 0.5f;
        boxCollider.size = max - min;
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
        DefaultCursorController.SetHoveredObject(this);
        RefreshHoverOutline();
    }

    private void OnMouseExit()
    {
        isHovered = false;
        DefaultCursorController.ClearHoveredObject(this);
        RefreshHoverOutline();
    }

    private void OnDisable()
    {
        DefaultCursorController.ClearHoveredObject(this);
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

        if (disableCollidersOnInteractionDisabled)
        {
            foreach (Collider interactionCollider in GetComponents<Collider>())
            {
                interactionCollider.enabled = false;
            }
        }

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
