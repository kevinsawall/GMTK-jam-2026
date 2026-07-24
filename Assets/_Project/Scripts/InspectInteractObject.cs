using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "GMTK Jam/Interactions/Inspect", fileName = "InspectInteraction")]
public sealed class InspectInteractObject : InteractObject
{
    [Header("Click and Correct Drop Phrases")]
    [SerializeField, TextArea(2, 5)] private List<string> playerPhrases = new();
    [Header("Required Flag")]
    [Tooltip("When assigned, the regular phrases play only after this story flag has been set.")]
    [SerializeField] private string requiredFlag;
    [Tooltip("Phrases played while the required flag has not been set.")]
    [SerializeField, TextArea(2, 5)] private List<string> missingRequiredFlagPhrases = new();
    [Header("Item Drag and Drop")]
    [SerializeField] private ItemData expectedDroppedItem;
    [Tooltip("Optional story flag set after the expected item is dropped.")]
    [SerializeField] private string flagToSetOnCorrectItemDrop;
    [SerializeField, TextArea(2, 5)] private string incorrectItemDropPhrase;

    public override InteractionType Type => InteractionType.Inspect;

    public override void Interact(ObjectController controller)
    {
        ShowNextPlayerPhrase(controller);
    }

    public override bool TryReceiveItem(ItemData item, ObjectController controller)
    {
        if (expectedDroppedItem == null || item != expectedDroppedItem)
        {
            return ShowPlayerPhrase(incorrectItemDropPhrase);
        }

        bool wasAccepted = ShowNextPlayerPhrase(controller, flagToSetOnCorrectItemDrop);
        if (wasAccepted)
        {
            DialogueManager manager = DialogueManager.Instance ??
                Object.FindFirstObjectByType<DialogueManager>(FindObjectsInactive.Include);
            manager?.RemoveItem(item);
        }

        return wasAccepted;
    }

    private bool ShowNextPlayerPhrase(ObjectController controller, string flagToSet = null)
    {
        DialogueManager manager = DialogueManager.Instance ??
            Object.FindFirstObjectByType<DialogueManager>(FindObjectsInactive.Include);
        if (manager == null)
        {
            Debug.LogWarning("No DialogueManager is present in the scene.", this);
            return false;
        }

        if (!string.IsNullOrWhiteSpace(flagToSet))
        {
            manager.SetFlag(flagToSet);
        }

        List<string> phrases = playerPhrases;
        if (!string.IsNullOrWhiteSpace(requiredFlag) && !manager.HasFlag(requiredFlag))
        {
            phrases = missingRequiredFlagPhrases;
        }

        if (phrases == null || phrases.Count == 0)
        {
            Debug.LogWarning("Inspect interaction has no matching player phrases assigned.", this);
            return false;
        }

        int phraseIndex = controller != null ? controller.GetNextInspectPhraseIndex(phrases.Count) : 0;
        manager.ShowPlayerPhrase(phrases[phraseIndex]);
        return true;
    }

    private bool ShowPlayerPhrase(string phrase)
    {
        if (string.IsNullOrWhiteSpace(phrase))
        {
            return false;
        }

        DialogueManager manager = DialogueManager.Instance ??
            Object.FindFirstObjectByType<DialogueManager>(FindObjectsInactive.Include);
        if (manager == null)
        {
            Debug.LogWarning("No DialogueManager is present in the scene.", this);
            return false;
        }

        manager.ShowPlayerPhrase(phrase);
        return true;
    }
}
