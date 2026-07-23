using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Owns the current conversation and the small amount of runtime story state that
/// dialogue conditions need. Place this component on the DialoguePanel object.
/// </summary>
public sealed class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }
    public static event Action NaturalCounterActionPerformed;

    [SerializeField] private TMP_Text speakerNameText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private Button continueButton;
    [SerializeField] private string playerDisplayName = "Player";
    [SerializeField, Min(1f)] private float charactersPerSecond = 45f;

    private readonly Dictionary<string, DialogueState> npcStates = new();
    private readonly HashSet<string> flags = new();
    private readonly HashSet<string> clues = new();

    private NpcDialogueSO currentDialogue;
    private DialogueEntry currentEntry;
    private int currentLineIndex;
    private Coroutine typewriter;
    private bool isTyping;

    public bool IsOpen => currentEntry != null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        FindPanelReferences();
        continueButton?.onClick.AddListener(Continue);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (!IsOpen) return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame))
        {
            Continue();
        }
    }

    public void StartDialogue(NpcDialogueSO dialogue)
    {
        if (dialogue == null) return;

        DialogueEntry entry = FindMatchingEntry(dialogue);
        if (entry == null)
        {
            Debug.LogWarning($"No dialogue entry matches '{dialogue.npcId}' in state {GetNpcState(dialogue.npcId)}.", dialogue);
            return;
        }

        StartDialogueEntry(dialogue, entry);
        if (entry.requiredState == DialogueState.FirstTalk) NaturalCounterActionPerformed?.Invoke();
    }

    public bool StartItemDropDialogue(NpcDialogueSO dialogue, ItemData droppedItem)
    {
        if (dialogue == null || droppedItem == null || IsOpen ||
            GetNpcState(dialogue.npcId) != DialogueState.WaitingForItem)
        {
            return false;
        }

        bool isCorrectItem = droppedItem == dialogue.expectedDroppedItem;
        DialogueEntry entry = isCorrectItem
            ? dialogue.correctItemDropDialogue
            : dialogue.incorrectItemDropDialogue;
        if (entry == null)
        {
            return false;
        }

        StartDialogueEntry(dialogue, entry);
        if (isCorrectItem) NaturalCounterActionPerformed?.Invoke();
        return true;
    }

    public void ShowPlayerPhrase(string phrase)
    {
        if (string.IsNullOrWhiteSpace(phrase)) return;

        currentDialogue = null;
        currentEntry = new DialogueEntry
        {
            lines = new List<DialogueLine>
            {
                new() { speaker = DialogueSpeaker.Player, text = phrase }
            }
        };
        currentLineIndex = 0;
        gameObject.SetActive(true);
        ShowCurrentLine();
    }

    public void Continue()
    {
        if (!IsOpen) return;

        if (isTyping)
        {
            FinishTyping();
            return;
        }

        currentLineIndex++;
        if (currentEntry.lines != null && currentLineIndex < currentEntry.lines.Count)
        {
            ShowCurrentLine();
            return;
        }

        ApplyActions(currentDialogue, currentEntry);
        CloseDialogue();
    }

    public DialogueState GetNpcState(string npcId)
    {
        return !string.IsNullOrWhiteSpace(npcId) && npcStates.TryGetValue(npcId, out DialogueState state)
            ? state
            : DialogueState.FirstTalk;
    }

    public void SetNpcState(string npcId, DialogueState state)
    {
        if (!string.IsNullOrWhiteSpace(npcId)) npcStates[npcId] = state;
    }

    public bool HasFlag(string flag) => string.IsNullOrWhiteSpace(flag) || flags.Contains(flag);
    public void SetFlag(string flag) { if (!string.IsNullOrWhiteSpace(flag)) flags.Add(flag); }
    public bool HasClue(string clueId) => !string.IsNullOrWhiteSpace(clueId) && clues.Contains(clueId);
    public bool HasItem(ItemData item) => item == null || GetInventoryManager()?.HasItem(item) == true;
    public void GiveItem(ItemData item) => GetInventoryManager()?.AddItem(item);
    public void RemoveItem(ItemData item) => GetInventoryManager()?.RemoveItem(item);

    private void FindPanelReferences()
    {
        TMP_Text[] textFields = GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text textField in textFields)
        {
            if (speakerNameText == null && textField.name.Contains("Name")) speakerNameText = textField;
            if (dialogueText == null && textField.name.Contains("TextText")) dialogueText = textField;
        }

        if (continueButton == null) continueButton = GetComponentInChildren<Button>(true);
    }

    private DialogueEntry FindMatchingEntry(NpcDialogueSO dialogue)
    {
        DialogueState state = GetNpcState(dialogue.npcId);
        if (dialogue.entries == null) return null;

        foreach (DialogueEntry entry in dialogue.entries)
        {
            if (entry == null)
            {
                continue;
            }

            if (state == DialogueState.WaitingForItem && entry.requiredItem != null)
            {
                continue;
            }

            if (entry.requiredState == state && HasFlag(entry.requiredFlag) &&
                HasItem(entry.requiredItem))
            {
                return entry;
            }
        }

        return null;
    }

    private void ShowCurrentLine()
    {
        if (currentEntry.lines == null || currentEntry.lines.Count == 0)
        {
            Continue();
            return;
        }

        if (typewriter != null) StopCoroutine(typewriter);
        DialogueLine line = currentEntry.lines[currentLineIndex];
        speakerNameText.text = line != null && line.speaker == DialogueSpeaker.Player
            ? playerDisplayName
            : currentDialogue != null ? currentDialogue.npcDisplayName : string.Empty;
        typewriter = StartCoroutine(TypeLine(line?.text ?? string.Empty));
    }

    private void StartDialogueEntry(NpcDialogueSO dialogue, DialogueEntry entry)
    {
        currentDialogue = dialogue;
        currentEntry = entry;
        currentLineIndex = 0;
        gameObject.SetActive(true);
        speakerNameText.text = dialogue.npcDisplayName;
        ShowCurrentLine();
    }

    private IEnumerator TypeLine(string line)
    {
        isTyping = true;
        dialogueText.text = string.Empty;
        float delay = 1f / charactersPerSecond;
        for (int i = 0; i < line.Length; i++)
        {
            dialogueText.text += line[i];
            yield return new WaitForSeconds(delay);
        }

        isTyping = false;
        typewriter = null;
    }

    private void FinishTyping()
    {
        if (typewriter != null) StopCoroutine(typewriter);
        dialogueText.text = currentEntry.lines[currentLineIndex]?.text ?? string.Empty;
        isTyping = false;
        typewriter = null;
    }

    private void ApplyActions(NpcDialogueSO dialogue, DialogueEntry entry)
    {
        if (entry.actions == null) return;

        foreach (DialogueAction action in entry.actions)
        {
            if (action == null) continue;

            switch (action.type)
            {
                case DialogueActionType.SetFlag:
                    SetFlag(action.stringValue);
                    break;
                case DialogueActionType.AddClue:
                    if (!string.IsNullOrWhiteSpace(action.stringValue)) clues.Add(action.stringValue);
                    break;
                case DialogueActionType.RemoveItem:
                    RemoveItem(action.item);
                    break;
                case DialogueActionType.AddItem:
                    GiveItem(action.item);
                    break;
                case DialogueActionType.AdvanceNpcState:
                    SetNpcState(dialogue.npcId, action.nextNpcState);
                    break;
                case DialogueActionType.AdvanceStoryStage:
                    SetFlag(action.stringValue);
                    break;
            }
        }
    }

    private void CloseDialogue()
    {
        if (typewriter != null) StopCoroutine(typewriter);
        typewriter = null;
        isTyping = false;
        currentDialogue = null;
        currentEntry = null;
        gameObject.SetActive(false);
    }

    private static InventoryManager GetInventoryManager()
    {
        return InventoryManager.Instance ??
            UnityEngine.Object.FindFirstObjectByType<InventoryManager>(FindObjectsInactive.Include);
    }
}
