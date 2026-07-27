using UnityEngine;

/// <summary>Applies a restrained, looping opacity pulse to a UI panel.</summary>
[DisallowMultipleComponent]
public sealed class SubtleCanvasFade : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float minimumAlpha = 0.92f;
    [SerializeField, Min(0.1f)] private float halfCycleDuration = 1.5f;

    private CanvasGroup canvasGroup;
    private int fadeTweenId = -1;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        if (canvasGroup == null) return;

        if (fadeTweenId >= 0) LeanTween.cancel(fadeTweenId);
        canvasGroup.alpha = 1f;
        fadeTweenId = LeanTween.alphaCanvas(canvasGroup, minimumAlpha, halfCycleDuration)
            .setEaseInOutSine()
            .setLoopPingPong()
            .id;
    }

    private void OnDisable()
    {
        if (fadeTweenId >= 0) LeanTween.cancel(fadeTweenId);
        fadeTweenId = -1;
    }
}
