using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public sealed class HoverScaleController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField, Min(1f)] private float hoverScale = 1.08f;
    [SerializeField, Min(0f)] private float duration = 0.12f;

    private RectTransform rectTransform;
    private Vector3 defaultScale;
    private int tweenId = -1;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        defaultScale = rectTransform.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        TweenTo(defaultScale * hoverScale, LeanTweenType.easeOutBack);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TweenTo(defaultScale, LeanTweenType.easeOutQuad);
    }

    private void OnDisable()
    {
        CancelTween();

        if (rectTransform != null)
        {
            rectTransform.localScale = defaultScale;
        }
    }

    private void TweenTo(Vector3 targetScale, LeanTweenType ease)
    {
        CancelTween();
        tweenId = LeanTween.scale(rectTransform, targetScale, duration)
            .setEase(ease)
            .setIgnoreTimeScale(true)
            .id;
    }

    private void CancelTween()
    {
        if (tweenId >= 0)
        {
            LeanTween.cancel(tweenId);
            tweenId = -1;
        }
    }
}
