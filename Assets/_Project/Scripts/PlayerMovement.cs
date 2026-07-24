using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public sealed class PlayerMovement : MonoBehaviour
{
    public enum MovementStyle { Tank, Directional }

    [SerializeField] private MovementStyle movementStyle = MovementStyle.Directional;
    [SerializeField, Min(0f)] private float moveSpeed = 5f;
    [SerializeField, Min(0f)] private float turnSpeed = 540f;
    [SerializeField, Min(0f)] private float cornerTurnDistance = 0.75f;
    [SerializeField] private bool pointMovement = true;
    [SerializeField] private GridManager gridManager;
    [Header("Pseudo Restart Start Pose")]
    [SerializeField] private Vector3 restartPosition;
    [SerializeField] private Vector3 restartRotationEuler;

    private readonly List<GridCell> pointPath = new();
    private Rigidbody body;
    private Vector2 moveInput;
    private IInteractable pendingInteraction;
    private Transform pendingInteractionTarget;
    private int nextPathCell;
    private Vector3 lastPointPosition;
    private float blockedPointMovementTime;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
    }

    public void ResetToStartPosition()
    {
        moveInput = Vector2.zero;
        CancelPointMovement();
        body.position = restartPosition;
        body.rotation = Quaternion.Euler(restartRotationEuler);
        body.angularVelocity = Vector3.zero;
    }

    private void Update()
    {
        if (IsMovementBlocked())
        {
            moveInput = Vector2.zero;
            CancelPointMovement();
            return;
        }

        Keyboard keyboard = Keyboard.current;
        moveInput = keyboard == null ? Vector2.zero : Vector2.ClampMagnitude(new Vector2(
            (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f) -
            (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1f : 0f),
            (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1f : 0f) -
            (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1f : 0f)), 1f);

        if (pointMovement) TrySetPointDestination();
    }

    private void FixedUpdate()
    {
        if (IsMovementBlocked())
        {
            CancelPointMovement();
            return;
        }

        if (moveInput.sqrMagnitude > 0f) CancelPointMovement();
        else if (MoveToPoint()) return;

        if (movementStyle == MovementStyle.Tank) MoveTank();
        else MoveDirectional();
    }

    private void TrySetPointDestination()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame ||
            (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())) return;

        Camera camera = Camera.main;
        if (camera == null || gridManager == null) return;

        Ray ray = camera.ScreenPointToRay(mouse.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit, camera.farClipPlane))
        {
            return;
        }

        IInteractable interactable = GetInteractable(hit.collider);
        if (interactable != null)
        {
            if (SetReachableDestination(hit.point, interactable.InteractionDistance))
            {
                pendingInteraction = interactable;
                pendingInteractionTarget = hit.collider.transform;
            }
            else
            {
                pendingInteraction = null;
                pendingInteractionTarget = null;
            }

            return;
        }

        // A non-interactable collider consumes the click; do not move to ground behind it.
        if (!gridManager.IsGroundCollider(hit.collider) || !gridManager.TryGetCell(hit.point, out _)) return;

        pendingInteraction = null;
        pendingInteractionTarget = null;
        SetReachableDestination(hit.point);
    }

    private static IInteractable GetInteractable(Collider collider)
    {
        ObjectController objectController = collider.GetComponentInParent<ObjectController>();
        if (objectController != null && objectController.HasInteraction)
        {
            return objectController;
        }

        CharacterManager characterManager = collider.GetComponentInParent<CharacterManager>();
        return characterManager != null && characterManager.HasInteraction ? characterManager : null;
    }

    private bool SetReachableDestination(Vector3 destination, int interactionDistance = 0)
    {
        if (!gridManager.TryFindPath(body.position, destination, pointPath, interactionDistance))
        {
            CancelPointMovement();
            return false;
        }

        nextPathCell = pointPath.Count > 1 ? 1 : 0;
        lastPointPosition = body.position;
        blockedPointMovementTime = 0f;
        return true;
    }

    private bool MoveToPoint()
    {
        if (!pointMovement || pointPath.Count == 0) return false;
        if (nextPathCell >= pointPath.Count)
        {
            if (!FacePendingInteraction())
            {
                return true;
            }

            CompletePointMovement();
            return true;
        }

        Vector3 movementSinceLastStep = Vector3.ProjectOnPlane(body.position - lastPointPosition, Vector3.up);
        blockedPointMovementTime = movementSinceLastStep.sqrMagnitude <= 0.000001f
            ? blockedPointMovementTime + Time.fixedDeltaTime
            : 0f;
        lastPointPosition = body.position;

        // Do not keep turning in place if a collider prevents reaching the next cell.
        if (blockedPointMovementTime >= 0.25f)
        {
            CancelPointMovement();
            return true;
        }

        Vector3 cellCenter = pointPath[nextPathCell].WorldPosition;
        Vector3 targetPosition = new Vector3(cellCenter.x, body.position.y, cellCenter.z);
        Vector3 pathDirection = targetPosition - body.position;
        if (pathDirection.sqrMagnitude <= 0.0025f)
        {
            nextPathCell++;
            return true;
        }

        Quaternion targetRotation = Quaternion.LookRotation(GetPointMovementFacingDirection(pathDirection), Vector3.up);
        body.MoveRotation(Quaternion.RotateTowards(body.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime));
        body.MovePosition(Vector3.MoveTowards(body.position, targetPosition, moveSpeed * Time.fixedDeltaTime));
        return true;
    }

    private Vector3 GetPointMovementFacingDirection(Vector3 pathDirection)
    {
        Vector3 currentDirection = pathDirection.normalized;
        if (cornerTurnDistance <= 0f || nextPathCell + 1 >= pointPath.Count)
        {
            return currentDirection;
        }

        Vector3 nextCellCenter = pointPath[nextPathCell + 1].WorldPosition;
        Vector3 currentCellCenter = pointPath[nextPathCell].WorldPosition;
        Vector3 nextDirection = Vector3.ProjectOnPlane(nextCellCenter - currentCellCenter, Vector3.up).normalized;
        if (nextDirection.sqrMagnitude <= 0f)
        {
            return currentDirection;
        }

        float turnProgress = Mathf.Clamp01(1f - pathDirection.magnitude / cornerTurnDistance);
        return Vector3.Slerp(currentDirection, nextDirection, turnProgress).normalized;
    }

    private bool FacePendingInteraction()
    {
        if (pendingInteraction == null || pendingInteractionTarget == null)
        {
            return true;
        }

        Vector3 direction = Vector3.ProjectOnPlane(pendingInteractionTarget.position - body.position, Vector3.up);
        if (direction.sqrMagnitude <= 0.0001f)
        {
            return true;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        Quaternion nextRotation = Quaternion.RotateTowards(
            body.rotation,
            targetRotation,
            turnSpeed * Time.fixedDeltaTime);
        body.MoveRotation(nextRotation);
        return Quaternion.Angle(nextRotation, targetRotation) <= 0.1f;
    }

    private void CancelPointMovement()
    {
        pendingInteraction = null;
        pendingInteractionTarget = null;
        pointPath.Clear();
        nextPathCell = 0;
        blockedPointMovementTime = 0f;
    }

    private void CompletePointMovement()
    {
        IInteractable interaction = pendingInteraction;
        CancelPointMovement();
        body.angularVelocity = Vector3.zero;
        if (interaction != null) interaction.Interact();
    }

    private void MoveTank()
    {
        body.MoveRotation(body.rotation * Quaternion.Euler(0f, moveInput.x * turnSpeed * Time.fixedDeltaTime, 0f));
        body.MovePosition(body.position + transform.forward * (moveInput.y * moveSpeed * Time.fixedDeltaTime));
    }

    private void MoveDirectional()
    {
        if (moveInput.sqrMagnitude <= 0f) return;
        Vector3 targetDirection = new(moveInput.x, 0f, moveInput.y);
        Quaternion targetRotation = Quaternion.LookRotation(targetDirection, Vector3.up);
        body.MoveRotation(Quaternion.RotateTowards(body.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime));
        body.MovePosition(body.position + targetDirection * (moveSpeed * Time.fixedDeltaTime));
    }

    private static bool IsMovementBlocked()
    {
        if (PauseMenuController.IsPaused) return true;
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsOpen) return true;
        if (CutsceneController.IsStartGamePlaying) return true;
        if (CupTimerController.Instance != null && CupTimerController.Instance.IsRestartSequencePlaying) return true;
        if (CupTimerController.Instance != null && CupTimerController.Instance.IsCutscenePlaying) return true;
        return ItemNotification.Instance != null && ItemNotification.Instance.IsVisible;
    }
}
