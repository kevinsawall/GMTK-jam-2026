using UnityEngine;

public sealed class ObjectController : MonoBehaviour, IInteractable
{
    [SerializeField] private InteractObject interactObject;

    public InteractObject InteractObject => interactObject;

    public void Interact()
    {
        if (interactObject != null)
        {
            interactObject.Interact(this);
        }
    }
}
