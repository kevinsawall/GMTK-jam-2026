public interface IInteractable
{
    int InteractionDistance { get; }
    void Interact();
    bool TryReceiveItem(ItemData item);
}
