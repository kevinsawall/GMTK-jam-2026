using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>Stores the player's unique items and renders one icon per item in the InventoryPanel.</summary>
public sealed class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [SerializeField] private Transform itemContainer;
    [SerializeField] private GameObject itemViewPrefab;
    [SerializeField] private ItemNotification itemNotification;
    [Header("Item Tooltip")]
    [SerializeField] private RectTransform itemTooltip;
    [SerializeField] private TextMeshProUGUI itemTooltipText;
    [SerializeField] private Vector2 tooltipCursorOffset = new(16f, -16f);

    private readonly HashSet<ItemData> items = new();
    private readonly Dictionary<ItemData, GameObject> itemViews = new();
    private readonly Queue<ItemData> pendingNotifications = new();
    private ItemData hoveredItem;
    private Canvas tooltipCanvas;

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
        HideItemTooltip(null);
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
        UpdateTooltipPosition();

        if (pendingNotifications.Count == 0 || itemNotification == null || itemNotification.IsVisible) return;

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

        InventoryItemTooltip tooltip = itemView.GetComponent<InventoryItemTooltip>();
        if (tooltip == null) tooltip = itemView.AddComponent<InventoryItemTooltip>();
        tooltip.Initialize(this, item);

        return itemView;
    }

    public void ShowItemTooltip(ItemData item)
    {
        if (item == null || !EnsureTooltip()) return;

        hoveredItem = item;
        itemTooltipText.text = item.displayName;
        itemTooltip.gameObject.SetActive(true);
        UpdateTooltipPosition();
    }

    public void HideItemTooltip(ItemData item)
    {
        if (item != null && hoveredItem != item) return;

        hoveredItem = null;
        if (itemTooltip != null) itemTooltip.gameObject.SetActive(false);
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

    private bool EnsureTooltip()
    {
        if (itemTooltip == null && itemTooltipText != null)
        {
            itemTooltip = itemTooltipText.transform.parent as RectTransform;
        }

        if (itemTooltip == null || itemTooltipText == null)
        {
            tooltipCanvas = GetComponentInParent<Canvas>()?.rootCanvas;
            if (tooltipCanvas == null) return false;

            GameObject tooltipObject = new("Item Tooltip", typeof(RectTransform));
            tooltipObject.transform.SetParent(tooltipCanvas.rootCanvas.transform, false);
            tooltipObject.transform.SetAsLastSibling();
            itemTooltip = tooltipObject.GetComponent<RectTransform>();

            GameObject textObject = new("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(itemTooltip, false);
            itemTooltipText = textObject.GetComponent<TextMeshProUGUI>();
            if (TMP_Settings.defaultFontAsset != null) itemTooltipText.font = TMP_Settings.defaultFontAsset;
            itemTooltipText.fontSize = 24f;
            itemTooltipText.color = Color.white;
            itemTooltipText.alignment = TextAlignmentOptions.Left;
            itemTooltipText.raycastTarget = false;

            itemTooltip.anchorMin = Vector2.zero;
            itemTooltip.anchorMax = Vector2.zero;
            itemTooltip.pivot = new Vector2(0f, 1f);
            itemTooltip.sizeDelta = new Vector2(300f, 50f);

            RectTransform textTransform = itemTooltipText.rectTransform;
            textTransform.anchorMin = Vector2.zero;
            textTransform.anchorMax = Vector2.one;
            textTransform.offsetMin = Vector2.zero;
            textTransform.offsetMax = Vector2.zero;
        }

        if (tooltipCanvas == null) tooltipCanvas = itemTooltip.GetComponentInParent<Canvas>()?.rootCanvas;
        return tooltipCanvas != null;
    }

    private void UpdateTooltipPosition()
    {
        if (hoveredItem == null || itemTooltip == null || !itemTooltip.gameObject.activeSelf || !EnsureTooltip()) return;

        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        Camera eventCamera = tooltipCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : tooltipCanvas.worldCamera;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)tooltipCanvas.transform,
                mouse.position.ReadValue(),
                eventCamera,
                out Vector2 localPosition))
        {
            itemTooltip.anchoredPosition = localPosition + tooltipCursorOffset;
        }
    }
}
