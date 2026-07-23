using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Shows a newly acquired item and closes when the player clicks it.</summary>
public sealed class ItemNotification : MonoBehaviour, IPointerClickHandler
{
    public static ItemNotification Instance { get; private set; }

    [SerializeField] private TMP_Text notificationText;

    public bool IsVisible => gameObject.activeSelf;

    private void Awake()
    {
        Instance = this;
        if (notificationText == null) notificationText = GetComponentInChildren<TMP_Text>(true);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Show(ItemData item)
    {
        if (item == null) return;

        gameObject.SetActive(true);
        notificationText.text = $"You got {GetArticle(item.displayName)} {item.displayName}.";
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Hide();
    }

    public void Hide() => gameObject.SetActive(false);

    private static string GetArticle(string itemName)
    {
        if (string.IsNullOrWhiteSpace(itemName)) return "an item";

        char firstLetter = char.ToLowerInvariant(itemName.TrimStart()[0]);
        return firstLetter is 'a' or 'e' or 'i' or 'o' or 'u' ? "an" : "a";
    }
}
