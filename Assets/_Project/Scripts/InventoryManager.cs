using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Stores the player's unique items and renders one icon per item in the InventoryPanel.</summary>
public sealed class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [SerializeField] private Transform itemContainer;
    [SerializeField] private GameObject itemViewPrefab;
    [SerializeField] private ItemNotification itemNotification;

    private readonly HashSet<ItemData> items = new();
    private readonly Dictionary<ItemData, GameObject> itemViews = new();
    private readonly Queue<ItemData> pendingNotifications = new();

    public event Action<ItemData> ItemAdded;
    public event Action<ItemData> ItemRemoved;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (itemContainer == null) itemContainer = transform;
        FindItemNotification();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public bool HasItem(ItemData item) => item != null && items.Contains(item);

    public bool AddItem(ItemData item)
    {
        if (item == null || !items.Add(item)) return false;

        itemViews[item] = CreateItemView(item);
        pendingNotifications.Enqueue(item);
        ItemAdded?.Invoke(item);
        return true;
    }

    private void Update()
    {
        if (pendingNotifications.Count == 0 || itemNotification == null || itemNotification.IsVisible) return;
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsOpen) return;

        itemNotification.Show(pendingNotifications.Dequeue());
    }

    public bool RemoveItem(ItemData item)
    {
        if (item == null || !items.Remove(item)) return false;

        if (itemViews.Remove(item, out GameObject itemView)) Destroy(itemView);
        ItemRemoved?.Invoke(item);
        return true;
    }

    private GameObject CreateItemView(ItemData item)
    {
        GameObject itemView = itemViewPrefab != null
            ? Instantiate(itemViewPrefab, itemContainer)
            : CreateDefaultItemView(item);
        itemView.name = item.displayName;

        Image image = itemView.GetComponentInChildren<Image>();
        if (image != null)
        {
            image.sprite = item.icon;
            image.preserveAspect = true;
        }

        RawImage rawImage = itemView.GetComponentInChildren<RawImage>();
        if (rawImage != null) rawImage.texture = item.icon != null ? item.icon.texture : null;

        InventoryItemDrag drag = itemView.GetComponent<InventoryItemDrag>();
        if (drag == null) drag = itemView.AddComponent<InventoryItemDrag>();
        drag.Initialize(item);

        return itemView;
    }

    private GameObject CreateDefaultItemView(ItemData item)
    {
        GameObject itemView = new("Inventory Item", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        itemView.transform.SetParent(itemContainer, false);
        Image background = itemView.GetComponent<Image>();
        background.color = new Color(1f, 1f, 1f, 0.9f);
        LayoutElement layout = itemView.GetComponent<LayoutElement>();
        layout.preferredWidth = 100f;
        layout.preferredHeight = 100f;

        GameObject label = new("Item Name", typeof(RectTransform), typeof(TextMeshProUGUI));
        label.transform.SetParent(itemView.transform, false);
        RectTransform labelTransform = label.GetComponent<RectTransform>();
        labelTransform.anchorMin = Vector2.zero;
        labelTransform.anchorMax = Vector2.one;
        labelTransform.offsetMin = new Vector2(6f, 6f);
        labelTransform.offsetMax = new Vector2(-6f, -6f);
        TextMeshProUGUI labelText = label.GetComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null) labelText.font = TMP_Settings.defaultFontAsset;
        labelText.text = item.displayName;
        labelText.color = Color.black;
        labelText.fontSize = 18f;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.enableWordWrapping = true;
        return itemView;
    }

    private void FindItemNotification()
    {
        if (itemNotification != null) return;

        Transform notificationTransform = null;
        foreach (Transform transform in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (transform.name == "ItemNotification" && transform.gameObject.scene.IsValid())
            {
                notificationTransform = transform;
                break;
            }
        }

        if (notificationTransform == null) return;

        itemNotification = notificationTransform.GetComponent<ItemNotification>();
        if (itemNotification == null) itemNotification = notificationTransform.gameObject.AddComponent<ItemNotification>();
        itemNotification.Hide();
    }
}
