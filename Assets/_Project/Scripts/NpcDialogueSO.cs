using System.Collections.Generic;
using UnityEngine;

public enum DialogueState
{
    FirstTalk = 0,
    Summary = 1,
    WaitingForItem = 2,
    // Keep this value so existing Completed dialogue assets remain valid.
    Completed = 5
}

public enum DialogueActionType
{
    None,
    SetFlag,
    AddClue,
    RemoveItem,
    AdvanceNpcState,
    AdvanceStoryStage,
    AddItem
}

public enum DialogueSpeaker
{
    Npc,
    Player
}

[CreateAssetMenu(menuName = "Game/Dialogue/NPC Dialogue", fileName = "NpcDialogue")]
public sealed class NpcDialogueSO : ScriptableObject
{
    public string npcId;
    public string npcDisplayName;
    public Sprite portrait;
    public List<DialogueEntry> entries = new();

    [Header("Item Drag and Drop")]
    public ItemData expectedDroppedItem;
    public DialogueEntry correctItemDropDialogue;
    public DialogueEntry incorrectItemDropDialogue;
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
    public List<DialogueLine> lines = new();
    public List<DialogueAction> actions = new();
}

[System.Serializable]
public sealed class DialogueLine
{
    public DialogueSpeaker speaker;
    [TextArea(2, 5)] public string text;
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
