using UnityEngine;

public sealed class ObjectController : MonoBehaviour, IInteractable
{
    [SerializeField] private InteractObject interactObject;

    public InteractObject InteractObject => interactObject;
    public bool HasInteraction => interactObject != null;

    public void Interact()
    {
        if (interactObject != null)
        {
            interactObject.Interact(this);
        }
    }
}
