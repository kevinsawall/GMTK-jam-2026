using UnityEngine;

[CreateAssetMenu(menuName = "GMTK Jam/Interactions/Talk", fileName = "TalkInteraction")]
public sealed class TalkInteractObject : InteractObject
{
    [SerializeField, TextArea] private string openingLine;

    public override InteractionType Type => InteractionType.Talk;
    public string OpeningLine => openingLine;

    public override void Interact(ObjectController controller)
    {
    }
}
