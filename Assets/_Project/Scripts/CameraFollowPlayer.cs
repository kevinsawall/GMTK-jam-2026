using UnityEngine;

/// <summary>
/// Moves a camera rig with the player and gives it a subtle, irregular train sway.
/// The child camera remains focused on the assigned player.
/// </summary>
[DefaultExecutionOrder(100)]
public sealed class CameraFollowPlayer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform mainCamera;

    [Header("Follow")]
    [SerializeField] private Vector3 followOffset = new(-3.05f, 7.58f, -4.37f);
    [SerializeField] private Vector3 lookAtOffset = new(-0.36f, -1.16f, -0.61f);
    [SerializeField, Min(0.01f)] private float followSmoothTime = 0.12f;

    [Header("Train Sway")]
    [SerializeField] private Vector3 positionSway = new(0.04f, 0.025f, 0.02f);
    [SerializeField] private Vector3 rotationSway = new(0.25f, 0.2f, 0.4f);
    [SerializeField, Min(0.1f)] private float minimumSwayDuration = 0.7f;
    [SerializeField, Min(0.1f)] private float maximumSwayDuration = 1.6f;

    private Vector3 followPosition;
    private Vector3 followVelocity;
    private Vector3 currentPositionSway;
    private Vector3 currentRotationSway;
    private Vector3 positionSwayFrom;
    private Vector3 positionSwayTo;
    private Vector3 rotationSwayFrom;
    private Vector3 rotationSwayTo;

    private void Start()
    {
        if (player == null || mainCamera == null)
        {
            enabled = false;
            return;
        }

        followPosition = player.position + followOffset;
        transform.position = followPosition;
        StartNextSway();
    }

    private void OnDisable()
    {
        LeanTween.cancel(gameObject);
    }

    private void LateUpdate()
    {
        if (player == null || mainCamera == null) return;

        followPosition = Vector3.SmoothDamp(
            followPosition,
            player.position + followOffset,
            ref followVelocity,
            followSmoothTime);
        transform.SetPositionAndRotation(
            followPosition + currentPositionSway,
            Quaternion.Euler(currentRotationSway));

        Vector3 lookDirection = player.position + lookAtOffset - mainCamera.position;
        if (lookDirection.sqrMagnitude > 0.0001f)
        {
            mainCamera.rotation = Quaternion.LookRotation(lookDirection, transform.up);
        }
    }

    private void StartNextSway()
    {
        positionSwayFrom = currentPositionSway;
        rotationSwayFrom = currentRotationSway;
        positionSwayTo = RandomSway(positionSway);
        rotationSwayTo = RandomSway(rotationSway);

        float duration = Random.Range(
            Mathf.Min(minimumSwayDuration, maximumSwayDuration),
            Mathf.Max(minimumSwayDuration, maximumSwayDuration));
        LeanTween.value(gameObject, 0f, 1f, duration)
            .setEaseInOutSine()
            .setOnUpdate(UpdateSway)
            .setOnComplete(StartNextSway);
    }

    private void UpdateSway(float progress)
    {
        currentPositionSway = Vector3.LerpUnclamped(positionSwayFrom, positionSwayTo, progress);
        currentRotationSway = Vector3.LerpUnclamped(rotationSwayFrom, rotationSwayTo, progress);
    }

    private static Vector3 RandomSway(Vector3 maximumSway) => new(
        Random.Range(-maximumSway.x, maximumSway.x),
        Random.Range(-maximumSway.y, maximumSway.y),
        Random.Range(-maximumSway.z, maximumSway.z));
}
