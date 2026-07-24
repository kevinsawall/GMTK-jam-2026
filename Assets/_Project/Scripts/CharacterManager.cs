using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public sealed class CharacterManager : MonoBehaviour, IInteractable
{
    public enum CharacterType
    {
        Npc,
        Player
    }

    [SerializeField] private CharacterType characterType = CharacterType.Player;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private InteractObject interactObject;
    [SerializeField, Min(1)] private int interactionDistance = 1;
    [Header("NPC Hover Outline")]
    [SerializeField] private Material hoverOutlineMaterial;

    public CharacterType Type => characterType;
    public bool HasInteraction => interactObject != null;
    public InteractObject InteractObject => interactObject;
    public int InteractionDistance => interactionDistance;

    private Renderer[] outlineRenderers;
    private Material[][] originalMaterials;
    private bool isHovered;
    private bool isHoverOutlineVisible;

    private void Awake()
    {
        ApplyCharacterType();
        CacheOutlineRenderers();
    }

    private void OnEnable()
    {
        if (outlineRenderers == null) return;

        DialogueManager.DialogueStarted += RefreshHoverOutline;
        DialogueManager.DialogueClosed += RefreshHoverOutline;
        DialogueManager.PlayerDialogueStarted += RefreshHoverOutline;
        DialogueManager.PlayerDialogueClosed += RefreshHoverOutline;
        ItemNotification.AnyVisibilityChanged += RefreshHoverOutline;
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
        if (outlineRenderers == null) return;

        DialogueManager.DialogueStarted -= RefreshHoverOutline;
        DialogueManager.DialogueClosed -= RefreshHoverOutline;
        DialogueManager.PlayerDialogueStarted -= RefreshHoverOutline;
        DialogueManager.PlayerDialogueClosed -= RefreshHoverOutline;
        ItemNotification.AnyVisibilityChanged -= RefreshHoverOutline;
        PauseMenuController.PauseStateChanged -= RefreshHoverOutline;
        CutsceneController.StartGameStateChanged -= RefreshHoverOutline;
        SetHoverOutlineVisible(false);
    }

    private void OnValidate()
    {
        if (playerMovement == null)
        {
            playerMovement = GetComponent<PlayerMovement>();
        }

        ApplyCharacterType();
    }

    private void ApplyCharacterType()
    {
        if (playerMovement != null)
        {
            playerMovement.enabled = characterType == CharacterType.Player;
        }
    }

    private void CacheOutlineRenderers()
    {
        if (characterType != CharacterType.Npc || hoverOutlineMaterial == null) return;

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
                                ItemNotification.IsAnyVisible ||
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
        if (interactObject != null)
        {
            interactObject.Interact(null);
        }
    }

    public bool TryReceiveItem(ItemData item)
    {
        return interactObject is TalkInteractObject talkInteraction &&
               talkInteraction.TryReceiveItem(item);
    }

    /// <summary>Resets this character only when it is the scene player.</summary>
    public void ResetToStartPosition()
    {
        if (characterType != CharacterType.Player) return;

        if (playerMovement == null)
        {
            playerMovement = GetComponent<PlayerMovement>();
        }

        playerMovement?.ResetToStartPosition();
    }
}
