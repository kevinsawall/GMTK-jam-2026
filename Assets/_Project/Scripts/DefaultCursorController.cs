using UnityEngine;

/// <summary>Keeps the game's default cursor active in the editor and standalone builds.</summary>
[DisallowMultipleComponent]
public sealed class DefaultCursorController : MonoBehaviour
{
    [SerializeField] private Texture2D defaultCursor;
    [SerializeField] private Vector2 hotspot = new(2f, 1f);
    [SerializeField] private Texture2D inspectCursor;
    [SerializeField] private Vector2 inspectHotspot = new(24f, 24f);
    [SerializeField] private Texture2D talkingCursor;
    [SerializeField] private Vector2 talkingHotspot = new(22f, 16f);

    private static DefaultCursorController instance;
    private Texture2D activeCursor;
    private ObjectController hoveredObject;
    private CharacterManager hoveredNpc;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        ApplyCursor();
    }

    private void OnEnable()
    {
        DialogueManager.DialogueStarted += RefreshHoverCursor;
        DialogueManager.DialogueClosed += RefreshHoverCursor;
        DialogueManager.PlayerDialogueStarted += RefreshHoverCursor;
        DialogueManager.PlayerDialogueClosed += RefreshHoverCursor;
        CutsceneController.CutsceneStateChanged += RefreshHoverCursor;
        InventoryItemDrag.DragStateChanged += RefreshHoverCursor;
        ApplyCursor();
    }

    private void OnDisable()
    {
        DialogueManager.DialogueStarted -= RefreshHoverCursor;
        DialogueManager.DialogueClosed -= RefreshHoverCursor;
        DialogueManager.PlayerDialogueStarted -= RefreshHoverCursor;
        DialogueManager.PlayerDialogueClosed -= RefreshHoverCursor;
        CutsceneController.CutsceneStateChanged -= RefreshHoverCursor;
        InventoryItemDrag.DragStateChanged -= RefreshHoverCursor;
    }

    public static void SetHoveredObject(ObjectController objectController)
    {
        if (instance == null) return;

        instance.hoveredObject = objectController;
        instance.RefreshHoverCursor();
    }

    public static void ClearHoveredObject(ObjectController objectController)
    {
        if (instance == null || instance.hoveredObject != objectController) return;

        instance.hoveredObject = null;
        instance.RefreshHoverCursor();
    }

    public static void SetHoveredNpc(CharacterManager character)
    {
        if (instance == null) return;

        instance.hoveredNpc = character;
        instance.RefreshHoverCursor();
    }

    public static void ClearHoveredNpc(CharacterManager character)
    {
        if (instance == null || instance.hoveredNpc != character) return;

        instance.hoveredNpc = null;
        instance.RefreshHoverCursor();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) return;

        activeCursor = null;
        RefreshHoverCursor();
    }

    private void RefreshHoverCursor()
    {
        if (InventoryItemDrag.IsDragging || DialogueManager.Instance?.IsOpen == true ||
            CutsceneController.IsAnyCutscenePlaying)
        {
            ApplyCursor(defaultCursor);
            return;
        }

        if (talkingCursor != null && hoveredNpc != null && hoveredNpc.isActiveAndEnabled &&
            hoveredNpc.Type == CharacterManager.CharacterType.Npc && hoveredNpc.HasInteraction)
        {
            ApplyCursor(talkingCursor);
            return;
        }

        if (inspectCursor != null && hoveredObject != null && hoveredObject.isActiveAndEnabled)
        {
            ApplyCursor(inspectCursor);
            return;
        }

        ApplyCursor(defaultCursor);
    }

    private void RefreshHoverCursor(NpcDialogueSO _) => RefreshHoverCursor();

    private void RefreshHoverCursor(NpcDialogueSO _, DialogueState __) => RefreshHoverCursor();

    private void RefreshHoverCursor(bool _) => RefreshHoverCursor();

    private void ApplyCursor()
    {
        ApplyCursor(defaultCursor);
    }

    private void ApplyCursor(Texture2D cursor)
    {
        if (cursor == null || activeCursor == cursor) return;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Cursor.SetCursor(cursor, GetHotspot(cursor), CursorMode.Auto);
        activeCursor = cursor;
    }

    private Vector2 GetHotspot(Texture2D cursor)
    {
        if (cursor == inspectCursor) return inspectHotspot;
        return cursor == talkingCursor ? talkingHotspot : hotspot;
    }
}
