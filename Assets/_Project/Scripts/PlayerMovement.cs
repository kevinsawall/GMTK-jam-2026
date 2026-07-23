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
        if (!movementPlane.Raycast(ray, out RaycastHit hit, camera.farClipPlane) ||
            !NavMesh.SamplePosition(hit.point, out NavMeshHit navMeshHit, 1f, navMeshAgent.areaMask))
        {
            return;
        }

        navMeshAgent.SetDestination(navMeshHit.position);
    }

    private bool MoveToPoint()
    {
        if (!pointMovement || navMeshAgent == null || !navMeshAgent.isOnNavMesh || !navMeshAgent.hasPath)
        {
            return false;
        }

        Vector3 pathVelocity = Vector3.ProjectOnPlane(navMeshAgent.desiredVelocity, Vector3.up);
        if (pathVelocity.sqrMagnitude <= 0.0001f)
        {
            return false;
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
