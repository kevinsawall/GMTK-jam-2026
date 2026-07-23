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

    public CharacterType Type => characterType;
    public bool HasInteraction => interactObject != null;
    public int InteractionDistance => interactionDistance;

    private void Awake()
    {
        ApplyCharacterType();
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

    public void Interact()
    {
        if (interactObject != null)
        {
            interactObject.Interact(null);
        }
    }
}
