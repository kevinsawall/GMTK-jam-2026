using UnityEngine;

[CreateAssetMenu(menuName = "GMTK Jam/Interactions/Inspect", fileName = "InspectInteraction")]
public sealed class InspectInteractObject : InteractObject
{
    [SerializeField, TextArea] private string description;

    public override InteractionType Type => InteractionType.Inspect;
    public string Description => description;

    public override void Interact(ObjectController controller)
    {
    }
}
