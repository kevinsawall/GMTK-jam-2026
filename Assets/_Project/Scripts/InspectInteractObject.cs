using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "GMTK Jam/Interactions/Inspect", fileName = "InspectInteraction")]
public sealed class InspectInteractObject : InteractObject
{
    [SerializeField, TextArea(2, 5)] private List<string> playerPhrases = new();
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

        return ShowNextPlayerPhrase(controller, flagToSetOnCorrectItemDrop);
    }

    private bool ShowNextPlayerPhrase(ObjectController controller, string flagToSet = null)
    {
        if (playerPhrases == null || playerPhrases.Count == 0)
        {
            Debug.LogWarning("Inspect interaction has no player phrases assigned.", this);
            return false;
        }

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

        int phraseIndex = controller != null ? controller.GetNextInspectPhraseIndex(playerPhrases.Count) : 0;
        manager.ShowPlayerPhrase(playerPhrases[phraseIndex]);
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
