using System.Collections.Generic;
using UnityEngine;

public enum DialogueState
{
    FirstTalk,
    Summary,
    WaitingForItem,
    ItemGiven,
    FinalTalk,
    Completed
}

public enum DialogueActionType
{
    None,
    SetFlag,
    AddClue,
    RemoveItem,
    AdvanceNpcState,
    AdvanceStoryStage
}

[CreateAssetMenu(menuName = "Game/Dialogue/NPC Dialogue", fileName = "NpcDialogue")]
public sealed class NpcDialogueSO : ScriptableObject
{
    public string npcId;
    public string npcDisplayName;
    public Sprite portrait;
    public List<DialogueEntry> entries = new();
}

[System.Serializable]
public sealed class DialogueEntry
{
    public string entryId;

    [Header("When this dialogue can play")]
    public DialogueState requiredState;
    public string requiredFlag;
    public ItemData requiredItem;

    [Header("Dialogue")]
    [TextArea(2, 5)] public string[] lines;
    public List<DialogueAction> actions = new();
}

[System.Serializable]
public sealed class DialogueAction
{
    public DialogueActionType type;
    [Tooltip("Flag, clue, or story-stage id depending on the action type.")]
    public string stringValue;
    public ItemData item;
    public DialogueState nextNpcState;
}
