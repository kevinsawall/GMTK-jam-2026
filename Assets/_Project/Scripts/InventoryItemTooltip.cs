using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Forwards inventory-item hover events to the shared inventory tooltip.</summary>
public sealed class InventoryItemTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private InventoryManager inventory;
    private ItemData item;

    public void Initialize(InventoryManager inventoryManager, ItemData itemData)
    {
        inventory = inventoryManager;
        item = itemData;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        inventory?.ShowItemTooltip(item);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        inventory?.HideItemTooltip(item);
    }

    private void OnDisable()
    {
        inventory?.HideItemTooltip(item);
    }
}
