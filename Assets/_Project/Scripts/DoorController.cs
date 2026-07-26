using UnityEngine;

public enum DoorHingeSide
{
    Left,
    Right
}

/// <summary>Opens a hinged door when its configured story flag is set.</summary>
public sealed class DoorController : MonoBehaviour
{
    [SerializeField] private string openOnFlag = "open-door";
    [SerializeField] private Transform door;
    [Header("Hinge Pivot")]
    [SerializeField] private bool createHingePivot;
    [SerializeField] private DoorHingeSide hingeSide = DoorHingeSide.Left;
    [SerializeField] private Vector3 openRotationOffset = new(0f, 90f, 0f);
    [SerializeField, Min(0f)] private float openDuration = 0.45f;
    [SerializeField] private LeanTweenType easeType = LeanTweenType.easeInOutQuad;

    private Vector3 closedLocalEulerAngles;
    private bool isOpen;
    private Transform rotationPivot;

    private void Awake()
    {
        if (door == null) door = transform;
        rotationPivot = createHingePivot ? CreateHingePivot() : door;
        closedLocalEulerAngles = rotationPivot.localEulerAngles;
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
        if (isOpen || rotationPivot == null) return;

        isOpen = true;
        LeanTween.cancel(rotationPivot.gameObject);
        LeanTween.rotateLocal(rotationPivot.gameObject, closedLocalEulerAngles + openRotationOffset, openDuration)
            .setEase(easeType);
    }

    private void OpenImmediately()
    {
        if (rotationPivot == null) return;

        isOpen = true;
        rotationPivot.localEulerAngles = closedLocalEulerAngles + openRotationOffset;
    }

    private Transform CreateHingePivot()
    {
        if (door == null || door.parent == null) return door;

        if (!TryGetLocalBounds(out Bounds localBounds)) return door;

        Vector3 hingeLocalPosition = localBounds.center;
        hingeLocalPosition.x = hingeSide == DoorHingeSide.Left ? localBounds.min.x : localBounds.max.x;

        GameObject hingeObject = new($"{door.name} Hinge");
        Transform hinge = hingeObject.transform;
        hinge.SetParent(door.parent, true);
        hinge.SetPositionAndRotation(door.TransformPoint(hingeLocalPosition), door.rotation);
        door.SetParent(hinge, true);
        return hinge;
    }

    private bool TryGetLocalBounds(out Bounds localBounds)
    {
        Renderer[] renderers = door.GetComponentsInChildren<Renderer>();
        bool hasBounds = false;
        localBounds = default;

        foreach (Renderer renderer in renderers)
        {
            Bounds worldBounds = renderer.bounds;
            Vector3 worldMin = worldBounds.min;
            Vector3 worldMax = worldBounds.max;

            for (int x = 0; x <= 1; x++)
            for (int y = 0; y <= 1; y++)
            for (int z = 0; z <= 1; z++)
            {
                Vector3 localPoint = door.InverseTransformPoint(new Vector3(
                    x == 0 ? worldMin.x : worldMax.x,
                    y == 0 ? worldMin.y : worldMax.y,
                    z == 0 ? worldMin.z : worldMax.z));

                if (hasBounds) localBounds.Encapsulate(localPoint);
                else
                {
                    localBounds = new Bounds(localPoint, Vector3.zero);
                    hasBounds = true;
                }
            }
        }

        return hasBounds;
    }
}
