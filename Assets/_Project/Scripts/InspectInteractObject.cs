using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "GMTK Jam/Interactions/Inspect", fileName = "InspectInteraction")]
public sealed class InspectInteractObject : InteractObject
{
    [SerializeField, TextArea(2, 5)] private List<string> playerPhrases = new();

    public override InteractionType Type => InteractionType.Inspect;

    public override void Interact(ObjectController controller)
    {
        if (playerPhrases == null || playerPhrases.Count == 0)
        {
            Debug.LogWarning("Inspect interaction has no player phrases assigned.", this);
            return;
        }

        DialogueManager manager = DialogueManager.Instance ??
            Object.FindFirstObjectByType<DialogueManager>(FindObjectsInactive.Include);
        if (manager == null)
        {
            Debug.LogWarning("No DialogueManager is present in the scene.", this);
            return;
        }

        int phraseIndex = controller != null ? controller.GetNextInspectPhraseIndex(playerPhrases.Count) : 0;
        manager.ShowPlayerPhrase(playerPhrases[phraseIndex]);
    }
}
