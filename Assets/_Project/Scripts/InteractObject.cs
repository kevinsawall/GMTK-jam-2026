using UnityEngine;

public enum InteractionType
{
    Inspect,
    PickUp,
    Talk
}

public abstract class InteractObject : ScriptableObject
{
    [SerializeField] private string prompt = "Interact";

    public abstract InteractionType Type { get; }
    public string Prompt => prompt;

    // Each interaction type supplies its behaviour when its supporting game system exists.
    public abstract void Interact(ObjectController controller);

    /// <summary>
    /// Handles a drag-and-drop inventory item when this interaction supports it.
    /// </summary>
    public virtual bool TryReceiveItem(ItemData item, ObjectController controller) => false;
}
