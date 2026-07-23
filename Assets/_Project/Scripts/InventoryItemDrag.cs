using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class InventoryItemDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform sourceRect;
    private Canvas rootCanvas;
    private Image sourceImage;
    private RawImage sourceRawImage;
    private RectTransform dragVisual;

    private void Awake()
    {
        sourceRect = (RectTransform)transform;
        sourceImage = GetComponentInChildren<Image>();
        sourceRawImage = GetComponentInChildren<RawImage>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null || (sourceImage == null && sourceRawImage == null))
        {
            return;
        }

        rootCanvas = canvas.rootCanvas;
        GameObject visual = new GameObject("Dragged Inventory Item", typeof(RectTransform), typeof(CanvasGroup));
        dragVisual = visual.GetComponent<RectTransform>();
        dragVisual.SetParent(rootCanvas.transform, false);
        dragVisual.SetAsLastSibling();
        dragVisual.anchorMin = new Vector2(0.5f, 0.5f);
        dragVisual.anchorMax = new Vector2(0.5f, 0.5f);
        dragVisual.sizeDelta = sourceRect.rect.size;

        if (sourceImage != null)
        {
            Image visualImage = visual.AddComponent<Image>();
            visualImage.sprite = sourceImage.sprite;
            visualImage.color = sourceImage.color;
            visualImage.preserveAspect = sourceImage.preserveAspect;
        }
        else
        {
            RawImage visualImage = visual.AddComponent<RawImage>();
            visualImage.texture = sourceRawImage.texture;
            visualImage.color = sourceRawImage.color;
            visualImage.uvRect = sourceRawImage.uvRect;
        }

        visual.GetComponent<CanvasGroup>().blocksRaycasts = false;

        UpdateDragPosition(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        UpdateDragPosition(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragVisual != null)
        {
            Destroy(dragVisual.gameObject);
            dragVisual = null;
        }
    }

    private void UpdateDragPosition(PointerEventData eventData)
    {
        if (dragVisual == null || rootCanvas == null)
        {
            return;
        }

        Camera eventCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : eventData.pressEventCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)rootCanvas.transform,
                eventData.position,
                eventCamera,
                out Vector2 localPosition))
        {
            dragVisual.anchoredPosition = localPosition;
        }
    }
}
