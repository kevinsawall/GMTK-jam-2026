using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public sealed class PlayerMovement : MonoBehaviour
{
    public enum MovementStyle
    {
        Tank,
        Directional
    }

    [SerializeField] private MovementStyle movementStyle = MovementStyle.Directional;
    [SerializeField, Min(0f)] private float moveSpeed = 5f;
    [SerializeField, Min(0f)] private float turnSpeed = 540f;

    [SerializeField] private bool pointMovement = true;
    [SerializeField] private Collider movementPlane;
    [SerializeField] private NavMeshAgent navMeshAgent;

    private Rigidbody body;
    private Vector2 moveInput;
    private ObjectController pendingInteraction;
    private float pointStoppingDistance;
    private NavMeshPath navigationPath;
    private Vector3 lastNavigationPosition;
    private float blockedNavigationTime;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        navMeshAgent = navMeshAgent != null ? navMeshAgent : GetComponentInChildren<NavMeshAgent>();
        if (navMeshAgent == null)
        {
            return;
        }

        navMeshAgent.updatePosition = false;
        navMeshAgent.updateRotation = false;
        navMeshAgent.speed = moveSpeed;
        pointStoppingDistance = navMeshAgent.stoppingDistance;
        navigationPath = new NavMeshPath();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            moveInput = Vector2.zero;
        }
        else
        {
            moveInput = new Vector2(
                (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f) -
                (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1f : 0f),
                (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1f : 0f) -
                (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1f : 0f));

            moveInput = Vector2.ClampMagnitude(moveInput, 1f);
        }

        if (pointMovement)
        {
            TrySetPointDestination();
        }
    }

    private void FixedUpdate()
    {
        if (navMeshAgent != null && navMeshAgent.isOnNavMesh)
        {
            navMeshAgent.nextPosition = body.position + Vector3.down;
        }

        if (moveInput.sqrMagnitude > 0f)
        {
            CancelPointMovement();
        }
        else if (MoveToPoint())
        {
            return;
        }

        if (movementStyle == MovementStyle.Tank)
        {
            MoveTank();
            return;
        }

        MoveDirectional();
    }

    private void TrySetPointDestination()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame ||
            (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()))
        {
            return;
        }

        Camera camera = Camera.main;
        if (camera == null || movementPlane == null || navMeshAgent == null || !navMeshAgent.isOnNavMesh)
        {
            return;
        }

        Ray ray = camera.ScreenPointToRay(mouse.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit objectHit, camera.farClipPlane))
        {
            ObjectController objectController = objectHit.collider.GetComponentInParent<ObjectController>();
            if (objectController != null && objectController.HasInteraction)
            {
                BeginInteraction(objectController, objectHit.point);
                return;
            }
        }

        if (!movementPlane.Raycast(ray, out RaycastHit hit, camera.farClipPlane) ||
            !NavMesh.SamplePosition(hit.point, out NavMeshHit navMeshHit, 1f, navMeshAgent.areaMask))
        {
            return;
        }

        pendingInteraction = null;
        SetReachableDestination(navMeshHit.position, pointStoppingDistance);
    }

    private void BeginInteraction(ObjectController objectController, Vector3 clickedPoint)
    {
        float sampleDistance = Mathf.Max(1f, objectController.InteractionDistance);
        if (!NavMesh.SamplePosition(clickedPoint, out NavMeshHit navMeshHit, sampleDistance, navMeshAgent.areaMask))
        {
            return;
        }

        pendingInteraction = SetReachableDestination(navMeshHit.position, objectController.InteractionDistance)
            ? objectController
            : null;
    }

    private bool SetReachableDestination(Vector3 destination, float stoppingDistance)
    {
        if (!NavMesh.CalculatePath(navMeshAgent.nextPosition, destination, navMeshAgent.areaMask, navigationPath) ||
            navigationPath.status != NavMeshPathStatus.PathComplete)
        {
            CancelPointMovement();
            return false;
        }

        navMeshAgent.stoppingDistance = stoppingDistance;
        if (!navMeshAgent.SetDestination(destination))
        {
            CancelPointMovement();
            return false;
        }

        lastNavigationPosition = body.position;
        blockedNavigationTime = 0f;
        return true;
    }

    private bool MoveToPoint()
    {
        if (!pointMovement || navMeshAgent == null || !navMeshAgent.isOnNavMesh)
        {
            return false;
        }

        if (!navMeshAgent.pathPending && navMeshAgent.pathStatus != NavMeshPathStatus.PathComplete)
        {
            CancelPointMovement();
            return true;
        }

        if (pendingInteraction != null && !navMeshAgent.pathPending &&
            navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance + 0.01f)
        {
            ObjectController interaction = pendingInteraction;
            pendingInteraction = null;
            navMeshAgent.ResetPath();
            interaction.Interact();
            return true;
        }

        if (!navMeshAgent.hasPath)
        {
            return false;
        }

        Vector3 pathVelocity = Vector3.ProjectOnPlane(navMeshAgent.desiredVelocity, Vector3.up);
        if (pathVelocity.sqrMagnitude <= 0.0001f)
        {
            return false;
        }

        Vector3 movementSinceLastFrame = Vector3.ProjectOnPlane(body.position - lastNavigationPosition, Vector3.up);
        blockedNavigationTime = movementSinceLastFrame.sqrMagnitude <= 0.000001f
            ? blockedNavigationTime + Time.fixedDeltaTime
            : 0f;
        lastNavigationPosition = body.position;

        if (blockedNavigationTime >= 0.75f)
        {
            CancelPointMovement();
            return true;
        }

        Quaternion targetRotation = Quaternion.LookRotation(pathVelocity, Vector3.up);
        Quaternion nextRotation = Quaternion.RotateTowards(
            body.rotation,
            targetRotation,
            turnSpeed * Time.fixedDeltaTime);

        // This matches directional WASD movement: move immediately while turning smoothly.
        body.MoveRotation(nextRotation);
        body.MovePosition(body.position + pathVelocity * Time.fixedDeltaTime);
        return true;
    }

    private void CancelPointMovement()
    {
        pendingInteraction = null;
        if (navMeshAgent != null && navMeshAgent.isOnNavMesh && navMeshAgent.hasPath)
        {
            navMeshAgent.ResetPath();
        }
    }

    private void MoveTank()
    {
        float turnAmount = moveInput.x * turnSpeed * Time.fixedDeltaTime;
        body.MoveRotation(body.rotation * Quaternion.Euler(0f, turnAmount, 0f));

        Vector3 displacement = transform.forward * (moveInput.y * moveSpeed * Time.fixedDeltaTime);
        body.MovePosition(body.position + displacement);
    }

    private void MoveDirectional()
    {
        if (moveInput.sqrMagnitude <= 0f)
        {
            return;
        }

        Vector3 targetDirection = new Vector3(moveInput.x, 0f, moveInput.y);
        Quaternion targetRotation = Quaternion.LookRotation(targetDirection, Vector3.up);
        Quaternion nextRotation = Quaternion.RotateTowards(
            body.rotation,
            targetRotation,
            turnSpeed * Time.fixedDeltaTime);

        body.MoveRotation(nextRotation);

        // Travel toward the selected direction immediately, while the character turns to face it.
        body.MovePosition(body.position + targetDirection * (moveSpeed * Time.fixedDeltaTime));
    }
}
