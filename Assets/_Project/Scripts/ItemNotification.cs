using TMPro;
using UnityEngine;

/// <summary>Shows a newly acquired item briefly, without interrupting gameplay.</summary>
public sealed class ItemNotification : MonoBehaviour
{
    [SerializeField] private TMP_Text notificationText;
    [SerializeField, Min(0f)] private float displayDurationSeconds = 5f;
    [SerializeField, Min(0f)] private float fadeInDuration = 0.2f;
    [SerializeField, Min(0f)] private float fadeOutDuration = 0.2f;

    public bool IsVisible => gameObject.activeSelf;
    private CanvasGroup canvasGroup;
    private Coroutine displayRoutine;

    private void Awake()
    {
        if (notificationText == null) notificationText = GetComponentInChildren<TMP_Text>(true);
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void Show(ItemData item)
    {
        if (item == null) return;

        gameObject.SetActive(true);
        notificationText.text = $"You got {GetArticle(item.displayName)} {item.displayName}.";
        if (displayRoutine != null) StopCoroutine(displayRoutine);
        LeanTween.cancel(gameObject);
        canvasGroup.alpha = 0f;
        displayRoutine = StartCoroutine(ShowAndHide());
    }

    public void Hide()
    {
        if (displayRoutine != null) StopCoroutine(displayRoutine);
        displayRoutine = null;
        LeanTween.cancel(gameObject);
        gameObject.SetActive(false);
    }

    private System.Collections.IEnumerator ShowAndHide()
    {
        LeanTween.alphaCanvas(canvasGroup, 1f, fadeInDuration).setIgnoreTimeScale(true);
        yield return new WaitForSecondsRealtime(fadeInDuration);
        yield return new WaitForSecondsRealtime(displayDurationSeconds);
        LeanTween.alphaCanvas(canvasGroup, 0f, fadeOutDuration).setIgnoreTimeScale(true);
        yield return new WaitForSecondsRealtime(fadeOutDuration);

        displayRoutine = null;
        gameObject.SetActive(false);
    }

    private static string GetArticle(string itemName)
    {
        if (string.IsNullOrWhiteSpace(itemName)) return "an item";

        char firstLetter = char.ToLowerInvariant(itemName.TrimStart()[0]);
        return firstLetter is 'a' or 'e' or 'i' or 'o' or 'u' ? "an" : "a";
    }
}
