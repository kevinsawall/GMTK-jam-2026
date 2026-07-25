using UnityEngine;

/// <summary>Opens a hinged door when its configured story flag is set.</summary>
public sealed class DoorController : MonoBehaviour
{
    [SerializeField] private string openOnFlag = "open-door";
    [SerializeField] private Transform door;
    [SerializeField] private Vector3 openRotationOffset = new(0f, 90f, 0f);
    [SerializeField, Min(0f)] private float openDuration = 0.45f;
    [SerializeField] private LeanTweenType easeType = LeanTweenType.easeInOutQuad;

    private Vector3 closedLocalEulerAngles;
    private bool isOpen;

    private void Awake()
    {
        if (door == null) door = transform;
        closedLocalEulerAngles = door.localEulerAngles;
    }

    private void OnEnable()
    {
        DialogueManager.FlagSet += HandleFlagSet;
        if (DialogueManager.Instance != null && DialogueManager.Instance.HasFlag(openOnFlag)) OpenImmediately();
    }

    private void OnDisable()
    {
        DialogueManager.FlagSet -= HandleFlagSet;
    }

    private void HandleFlagSet(string flag)
    {
        if (flag == openOnFlag) Open();
    }

    private void Open()
    {
        if (isOpen || door == null) return;

        isOpen = true;
        LeanTween.cancel(door.gameObject);
        LeanTween.rotateLocal(door.gameObject, closedLocalEulerAngles + openRotationOffset, openDuration)
            .setEase(easeType);
    }

    private void OpenImmediately()
    {
        if (door == null) return;

        isOpen = true;
        door.localEulerAngles = closedLocalEulerAngles + openRotationOffset;
    }
}
