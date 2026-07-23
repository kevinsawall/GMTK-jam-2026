using UnityEngine;
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

    private Rigidbody body;
    private Vector2 moveInput;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            moveInput = Vector2.zero;
            return;
        }

        moveInput = new Vector2(
            (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f) -
            (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1f : 0f),
            (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1f : 0f) -
            (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1f : 0f));

        moveInput = Vector2.ClampMagnitude(moveInput, 1f);
    }

    private void FixedUpdate()
    {
        if (movementStyle == MovementStyle.Tank)
        {
            MoveTank();
            return;
        }

        MoveDirectional();
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
