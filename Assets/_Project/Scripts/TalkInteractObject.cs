using UnityEngine;

[CreateAssetMenu(menuName = "GMTK Jam/Interactions/Talk", fileName = "TalkInteraction")]
public sealed class TalkInteractObject : InteractObject
{
    [SerializeField] private NpcDialogueSO dialogue;

    public override InteractionType Type => InteractionType.Talk;
    public NpcDialogueSO Dialogue => dialogue;

    public override void Interact(ObjectController controller)
    {
        if (dialogue == null)
        {
            Debug.LogWarning("Talk interaction has no NPC dialogue assigned.", this);
            return;
        }

        DialogueManager manager = DialogueManager.Instance ??
            Object.FindFirstObjectByType<DialogueManager>(FindObjectsInactive.Include);
        if (manager == null)
        {
            Debug.LogWarning("No DialogueManager is present in the scene.", this);
            return;
        }

        manager.StartDialogue(dialogue);
    }
}
