using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public sealed class CharacterManager : MonoBehaviour
{
    public enum CharacterType
    {
        Npc,
        Player
    }

    [SerializeField] private CharacterType characterType = CharacterType.Player;
    [SerializeField] private PlayerMovement playerMovement;

    public CharacterType Type => characterType;

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
}
