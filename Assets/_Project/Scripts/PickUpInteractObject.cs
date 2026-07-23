using UnityEngine;

[CreateAssetMenu(menuName = "GMTK Jam/Interactions/Pick Up", fileName = "PickUpInteraction")]
public sealed class PickUpInteractObject : InteractObject
{
    [SerializeField] private string itemId;

    public override InteractionType Type => InteractionType.PickUp;
    public string ItemId => itemId;

    public override void Interact(ObjectController controller)
    {
    }
}
