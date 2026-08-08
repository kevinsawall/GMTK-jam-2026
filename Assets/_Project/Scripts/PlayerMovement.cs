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
    [Header("Animation")]
    [SerializeField] private RuntimeAnimatorController animationController;
    [Header("Pseudo Restart Start Pose")]
    [SerializeField] private Vector3 restartPosition;
    [SerializeField] private Vector3 restartRotationEuler;

    private readonly List<GridCell> pointPath = new();
    private Rigidbody body;
    private Animator animator;
    private bool controlsAnimation;
    private Vector2 moveInput;
    private IInteractable pendingInteraction;
    private Transform pendingInteractionTarget;
    private ItemData pendingItemDelivery;
    private int nextPathCell;
    private Vector3 lastPointPosition;
    private float blockedPointMovementTime;
    private bool isMovingToPoint;

    private static readonly int IsWalking = Animator.StringToHash("IsWalking");

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        animator = GetComponentInChildren<Animator>();

        if (GetComponent<CharacterManager>()?.Type == CharacterManager.CharacterType.Player &&
            animator != null && animationController != null)
        {
            // Physics movement owns the character transform; root motion would compete
            // with Rigidbody.MovePosition between physics steps and cause visible jitter.
            animator.applyRootMotion = false;
            animator.runtimeAnimatorController = animationController;
            if (animator.GetComponent<FootstepAnimationEvents>() == null)
            {
                animator.gameObject.AddComponent<FootstepAnimationEvents>();
            }
            controlsAnimation = true;
        }
    }

    private void OnDisable() => SetWalking(false);

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
            SetWalking(false);
            return;
        }

        if (moveInput.sqrMagnitude > 0f) CancelPointMovement();
        else if (MoveToPoint())
        {
            SetWalking(isMovingToPoint);
            return;
        }

        bool isWalking = movementStyle == MovementStyle.Tank
            ? MoveTank()
            : MoveDirectional();
        SetWalking(isWalking);
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
            CharacterManager character = hit.collider.GetComponentInParent<CharacterManager>();
            if (character != null && character.Type == CharacterManager.CharacterType.Npc)
            {
                AudioManager.Instance?.PlaySfx(SfxId.ClickOnCharacter);
            }

            if (SetReachableDestination(hit.point, interactable.InteractionDistance))
            {
                pendingInteraction = interactable;
                pendingInteractionTarget = hit.collider.transform;
                pendingItemDelivery = null;
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
        pendingItemDelivery = null;
        SetReachableDestination(hit.point);
    }

    /// <summary>Walks the player into interaction range, then gives the item to the target.</summary>
    public bool TryApproachAndDeliverItem(ItemData item, Collider targetCollider, Vector3 dropPoint)
    {
        if (item == null || targetCollider == null || gridManager == null || IsMovementBlocked())
        {
            return false;
        }

        IInteractable interactable = GetInteractable(targetCollider);
        if (interactable == null || !SetReachableDestination(dropPoint, interactable.InteractionDistance))
        {
            return false;
        }

        pendingInteraction = interactable;
        pendingInteractionTarget = targetCollider.transform;
        pendingItemDelivery = item;
        return true;
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
        isMovingToPoint = false;
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
            isMovingToPoint = nextPathCell < pointPath.Count;
            return true;
        }

        Quaternion targetRotation = Quaternion.LookRotation(GetPointMovementFacingDirection(pathDirection), Vector3.up);
        body.MoveRotation(Quaternion.RotateTowards(body.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime));
        body.MovePosition(Vector3.MoveTowards(body.position, targetPosition, moveSpeed * Time.fixedDeltaTime));
        isMovingToPoint = true;
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
        pendingItemDelivery = null;
        pointPath.Clear();
        nextPathCell = 0;
        blockedPointMovementTime = 0f;
    }

    private void CompletePointMovement()
    {
        IInteractable interaction = pendingInteraction;
        ItemData itemToDeliver = pendingItemDelivery;
        CancelPointMovement();
        body.angularVelocity = Vector3.zero;
        if (interaction == null) return;

        if (itemToDeliver != null)
        {
            bool isCorrectItem = interaction switch
            {
                ObjectController objectController => objectController.IsCorrectDroppedItem(itemToDeliver),
                CharacterManager characterManager => characterManager.IsCorrectDroppedItem(itemToDeliver),
                _ => false
            };

            bool wasItemReceived = interaction.TryReceiveItem(itemToDeliver);
            if (isCorrectItem && wasItemReceived)
            {
                AudioManager.Instance?.PlaySfx(SfxId.ItemUseGiveItem);
            }
            else if (!isCorrectItem)
            {
                AudioManager.Instance?.PlaySfx(SfxId.WrongItemSound);
            }
        }
        else interaction.Interact();
    }

    private bool MoveTank()
    {
        body.MoveRotation(body.rotation * Quaternion.Euler(0f, moveInput.x * turnSpeed * Time.fixedDeltaTime, 0f));
        body.MovePosition(body.position + transform.forward * (moveInput.y * moveSpeed * Time.fixedDeltaTime));
        return !Mathf.Approximately(moveInput.y, 0f);
    }

    private bool MoveDirectional()
    {
        if (moveInput.sqrMagnitude <= 0f) return false;
        Vector3 targetDirection = new(moveInput.x, 0f, moveInput.y);
        Quaternion targetRotation = Quaternion.LookRotation(targetDirection, Vector3.up);
        body.MoveRotation(Quaternion.RotateTowards(body.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime));
        body.MovePosition(body.position + targetDirection * (moveSpeed * Time.fixedDeltaTime));
        return true;
    }

    private void SetWalking(bool isWalking)
    {
        if (controlsAnimation && animator != null)
        {
            animator.SetBool(IsWalking, isWalking);
        }
    }

    /// <summary>Called by the Walking clip's animation events when a foot contacts the ground.</summary>
    public void PlayFootstep()
    {
        AudioManager.Instance?.PlayRandomSfx(SfxId.Footstep1, SfxId.Footstep2);
    }

    private static bool IsMovementBlocked()
    {
        if (PauseMenuController.IsPaused) return true;
        if (GameManager.IsEndGameSequencePlaying) return true;
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsOpen) return true;
        if (CutsceneController.IsStartGamePlaying) return true;
        if (CupTimerController.Instance != null && CupTimerController.Instance.IsRestartSequencePlaying) return true;
        if (CupTimerController.Instance != null && CupTimerController.Instance.IsCutscenePlaying) return true;
        return false;
    }
}
