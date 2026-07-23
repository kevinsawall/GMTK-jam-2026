using UnityEngine;

[CreateAssetMenu(menuName = "GMTK Jam/Interactions/Pick Up", fileName = "PickUpInteraction")]
public sealed class PickUpInteractObject : InteractObject
{
    [SerializeField] private string itemId;
    [SerializeField] private ItemData item;

    public override InteractionType Type => InteractionType.PickUp;
    public string ItemId => itemId;

    public override void Interact(ObjectController controller)
    {
        DialogueManager manager = DialogueManager.Instance ??
            Object.FindFirstObjectByType<DialogueManager>(FindObjectsInactive.Include);
        manager?.GiveItem(item);
    }
}
