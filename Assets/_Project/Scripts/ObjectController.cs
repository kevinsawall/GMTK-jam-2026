using UnityEngine;

public sealed class ObjectController : MonoBehaviour, IInteractable
{
    [SerializeField] private InteractObject interactObject;
    [SerializeField, Min(0f)] private float interactionDistance = 1.5f;

    public InteractObject InteractObject => interactObject;
    public float InteractionDistance => interactionDistance;
    public bool HasInteraction => interactObject != null;

    public void Interact()
    {
        if (interactObject != null)
        {
            interactObject.Interact(this);
        }
    }
}
