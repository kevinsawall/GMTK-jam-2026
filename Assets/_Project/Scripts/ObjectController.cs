using UnityEngine;

public sealed class ObjectController : MonoBehaviour, IInteractable
{
    [SerializeField] private InteractObject interactObject;
    [SerializeField, Min(1)] private int interactionDistance = 1;

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
}
