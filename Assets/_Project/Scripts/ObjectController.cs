using UnityEngine;

public sealed class ObjectController : MonoBehaviour, IInteractable
{
    [SerializeField] private InteractObject interactObject;
    [SerializeField, Min(1)] private int interactionDistance = 1;

    private int nextInspectPhraseIndex;

    public InteractObject InteractObject => interactObject;
    public bool HasInteraction => interactObject != null;
    public int InteractionDistance => interactionDistance;

    public void Interact()
    {
        if (interactObject != null)
        {
            interactObject.Interact(this);
        }
    }

    public bool TryReceiveItem(ItemData item) => false;

    /// <summary>Returns this object's next inspect phrase index and advances it, looping at the end.</summary>
    public int GetNextInspectPhraseIndex(int phraseCount)
    {
        if (phraseCount <= 0) return 0;

        int phraseIndex = nextInspectPhraseIndex % phraseCount;
        nextInspectPhraseIndex = (phraseIndex + 1) % phraseCount;
        return phraseIndex;
    }
}
