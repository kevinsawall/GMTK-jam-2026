using UnityEngine;

[RequireComponent(typeof(Camera))]
[DefaultExecutionOrder(100)]
public sealed class PlayerCameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 followOffset = new(-6f, 8f, -6f);
    [SerializeField] private Vector3 lookAtOffset = new(0f, 0.75f, 0f);
    [SerializeField, Min(0.01f)] private float positionSmoothTime = 0.2f;
    [SerializeField, Min(0.01f)] private float rotationSmoothSpeed = 10f;
    [SerializeField] private bool lockVerticalFollow = true;
    [Header("Timeout Shake")]
    [SerializeField, Min(0f)] private float horizontalShakeDistance = 0.15f;
    [SerializeField, Min(0f)] private float horizontalShakeFrequency = 16f;

    private Vector3 followVelocity;
    private Vector3 followPosition;
    private float targetGroundHeight;
    private bool isShakingHorizontally;
    private float shakeStartedAt;
    private float shakeIntensityMultiplier = 1f;

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
        followPosition = Vector3.SmoothDamp(
            followPosition,
            targetPosition,
            ref followVelocity,
            positionSmoothTime);

        Quaternion targetRotation = Quaternion.LookRotation(
            focusPoint + lookAtOffset - followPosition,
            Vector3.up);
        float rotationBlend = 1f - Mathf.Exp(-rotationSmoothSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationBlend);
        transform.position = followPosition + GetShakeOffset();
    }

    public void StartHorizontalShake(float intensityMultiplier = 1f)
    {
        isShakingHorizontally = true;
        shakeStartedAt = Time.unscaledTime;
        shakeIntensityMultiplier = Mathf.Max(0f, intensityMultiplier);
    }

    public void StopHorizontalShakeAndResumeFollow()
    {
        isShakingHorizontally = false;
        shakeIntensityMultiplier = 1f;
        followVelocity = Vector3.zero;
    }

    private void SnapToTarget()
    {
        if (target == null)
        {
            return;
        }

        Vector3 focusPoint = GetFocusPoint();
        followPosition = focusPoint + followOffset;
        followVelocity = Vector3.zero;
        transform.position = followPosition;
        transform.rotation = Quaternion.LookRotation(focusPoint + lookAtOffset - followPosition, Vector3.up);
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

    private Vector3 GetShakeOffset()
    {
        if (!isShakingHorizontally) return Vector3.zero;

        float horizontalOffset = Mathf.Sin((Time.unscaledTime - shakeStartedAt) * horizontalShakeFrequency) *
                                 horizontalShakeDistance * shakeIntensityMultiplier;
        return Vector3.right * horizontalOffset;
    }
}
