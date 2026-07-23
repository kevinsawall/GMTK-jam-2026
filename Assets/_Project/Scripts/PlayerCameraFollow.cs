using UnityEngine;

[RequireComponent(typeof(Camera))]
[DefaultExecutionOrder(100)]
public sealed class PlayerCameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 followOffset = new(-6f, 8f, -6f);
    [SerializeField] private Vector3 lookAtOffset = new(0f, 0.75f, 0f);
    [SerializeField, Min(0.01f)] private float positionSmoothTime = 0.2f;
    [SerializeField, Min(0f)] private float rotationSmoothSpeed = 10f;
    [SerializeField] private bool lockVerticalFollow = true;

    private Vector3 followVelocity;
    private float targetGroundHeight;

    private void Start()
    {
        if (target != null)
        {
            targetGroundHeight = target.position.y;
        }

        SnapToTarget();
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 focusPoint = GetFocusPoint();
        Vector3 targetPosition = focusPoint + followOffset;
        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref followVelocity,
            positionSmoothTime);

        Quaternion targetRotation = Quaternion.LookRotation(
            focusPoint + lookAtOffset - transform.position,
            Vector3.up);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSmoothSpeed * Time.deltaTime);
    }

    private void SnapToTarget()
    {
        if (target == null)
        {
            return;
        }

        Vector3 focusPoint = GetFocusPoint();
        transform.position = focusPoint + followOffset;
        transform.LookAt(focusPoint + lookAtOffset, Vector3.up);
    }

    private Vector3 GetFocusPoint()
    {
        Vector3 focusPoint = target.position;
        if (lockVerticalFollow)
        {
            focusPoint.y = targetGroundHeight;
        }

        return focusPoint;
    }
}
