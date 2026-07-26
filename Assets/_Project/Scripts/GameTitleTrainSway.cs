using UnityEngine;

/// <summary>Gives the main-menu title a slow, irregular sway inspired by a moving train.</summary>
[RequireComponent(typeof(RectTransform))]
public sealed class GameTitleTrainSway : MonoBehaviour
{
    [SerializeField] private Vector2 movementRange = new(12f, 8f);
    [SerializeField, Min(0f)] private float rotationRange = 1.25f;
    [SerializeField] private Vector2 swayDurationRange = new(2.5f, 4.5f);

    private RectTransform rectTransform;
    private Vector2 restingPosition;
    private float restingRotation;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        restingPosition = rectTransform.anchoredPosition;
        restingRotation = rectTransform.localEulerAngles.z;
    }

    private void OnEnable()
    {
        StartNextSway();
    }

    private void OnDisable()
    {
        LeanTween.cancel(gameObject);
        ResetToRestingPose();
    }

    private void StartNextSway()
    {
        if (!isActiveAndEnabled || rectTransform == null) return;

        Vector2 startingPosition = rectTransform.anchoredPosition;
        float startingRotation = NormalizeAngle(rectTransform.localEulerAngles.z);
        Vector2 targetPosition = restingPosition + new Vector2(
            Random.Range(-movementRange.x, movementRange.x),
            Random.Range(-movementRange.y, movementRange.y));
        float targetRotation = restingRotation + Random.Range(-rotationRange, rotationRange);
        float duration = Random.Range(
            Mathf.Min(swayDurationRange.x, swayDurationRange.y),
            Mathf.Max(swayDurationRange.x, swayDurationRange.y));

        LeanTween.value(gameObject, 0f, 1f, duration)
            .setEaseInOutSine()
            .setIgnoreTimeScale(true)
            .setOnUpdate(value =>
            {
                rectTransform.anchoredPosition = Vector2.Lerp(startingPosition, targetPosition, value);
                rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(startingRotation, targetRotation, value));
            })
            .setOnComplete(StartNextSway);
    }

    private void ResetToRestingPose()
    {
        if (rectTransform == null) return;

        rectTransform.anchoredPosition = restingPosition;
        rectTransform.localRotation = Quaternion.Euler(0f, 0f, restingRotation);
    }

    private static float NormalizeAngle(float angle)
    {
        return angle > 180f ? angle - 360f : angle;
    }
}
