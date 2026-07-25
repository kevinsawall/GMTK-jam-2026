using UnityEngine;

/// <summary>
/// Moves a camera rig with the player and gives it a subtle, irregular train sway.
/// The child camera remains focused on the assigned player.
/// </summary>
[DefaultExecutionOrder(100)]
public sealed class CameraFollowPlayer : MonoBehaviour
{
    [System.Serializable]
    private sealed class ShakeProfile
    {
        public Vector3 positionAmplitude;
        public Vector3 rotationAmplitude;
        [Min(0.01f)] public float frequency = 16f;
    }

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform mainCamera;

    [Header("Follow")]
    [SerializeField] private Vector3 followOffset = new(-3.05f, 7.58f, -4.37f);
    [SerializeField] private Vector3 lookAtOffset = new(-0.36f, -1.16f, -0.61f);
    [SerializeField, Min(0.01f)] private float followSmoothTime = 0.12f;

    [Header("Train Sway")]
    [SerializeField] private bool trainSwayEnabled = true;
    [SerializeField] private Vector3 positionSway = new(0.04f, 0.025f, 0.02f);
    [SerializeField] private Vector3 rotationSway = new(0.25f, 0.2f, 0.4f);
    [SerializeField, Min(0.1f)] private float minimumSwayDuration = 0.7f;
    [SerializeField, Min(0.1f)] private float maximumSwayDuration = 1.6f;

    [Header("Event Shake")]
    [SerializeField] private ShakeProfile pseudoResetShake = new()
    {
        positionAmplitude = new Vector3(0.14f, 0.08f, 0.06f),
        rotationAmplitude = new Vector3(1.2f, 0.8f, 1.6f),
        frequency = 20f
    };
    [SerializeField] private ShakeProfile endGameShake = new()
    {
        positionAmplitude = new Vector3(0.035f, 0.02f, 0.015f),
        rotationAmplitude = new Vector3(0.3f, 0.2f, 0.45f),
        frequency = 12f
    };

    private Vector3 followPosition;
    private Vector3 followVelocity;
    private Vector3 currentPositionSway;
    private Vector3 currentRotationSway;
    private Vector3 positionSwayFrom;
    private Vector3 positionSwayTo;
    private Vector3 rotationSwayFrom;
    private Vector3 rotationSwayTo;
    private bool wasTrainSwayEnabled;
    private ShakeProfile activeShakeProfile;
    private float eventShakeStartedAt;

    private void Start()
    {
        if (player == null || mainCamera == null)
        {
            enabled = false;
            return;
        }

        followPosition = player.position + followOffset;
        transform.position = followPosition;
        wasTrainSwayEnabled = trainSwayEnabled;
        if (trainSwayEnabled) StartNextSway();
    }

    private void OnDisable()
    {
        LeanTween.cancel(gameObject);
        wasTrainSwayEnabled = false;
    }

    private void LateUpdate()
    {
        if (player == null || mainCamera == null) return;

        if (trainSwayEnabled != wasTrainSwayEnabled)
        {
            SetTrainSwayEnabled();
        }

        followPosition = Vector3.SmoothDamp(
            followPosition,
            player.position + followOffset,
            ref followVelocity,
            followSmoothTime);
        GetEventShake(out Vector3 eventPositionShake, out Vector3 eventRotationShake);
        transform.SetPositionAndRotation(
            followPosition + currentPositionSway + eventPositionShake,
            Quaternion.Euler(currentRotationSway + eventRotationShake));

        Vector3 lookDirection = player.position + lookAtOffset - mainCamera.position;
        if (lookDirection.sqrMagnitude > 0.0001f)
        {
            mainCamera.rotation = Quaternion.LookRotation(lookDirection, transform.up);
        }
    }

    private void StartNextSway()
    {
        if (!trainSwayEnabled || !isActiveAndEnabled) return;

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

    private void SetTrainSwayEnabled()
    {
        LeanTween.cancel(gameObject);
        currentPositionSway = Vector3.zero;
        currentRotationSway = Vector3.zero;
        wasTrainSwayEnabled = trainSwayEnabled;

        if (trainSwayEnabled) StartNextSway();
    }

    /// <summary>Starts the stronger shake used while the pseudo-reset countdown is active.</summary>
    public void StartPseudoResetShake() => StartEventShake(pseudoResetShake);

    /// <summary>Starts the gentler shake used before the end-game cutscene.</summary>
    public void StartEndGameShake() => StartEventShake(endGameShake);

    public void StopEventShake()
    {
        activeShakeProfile = null;
    }

    private void StartEventShake(ShakeProfile profile)
    {
        if (profile == null) return;

        activeShakeProfile = profile;
        eventShakeStartedAt = Time.unscaledTime;
    }

    private void GetEventShake(out Vector3 positionShake, out Vector3 rotationShake)
    {
        if (activeShakeProfile == null)
        {
            positionShake = Vector3.zero;
            rotationShake = Vector3.zero;
            return;
        }

        float time = (Time.unscaledTime - eventShakeStartedAt) * activeShakeProfile.frequency;
        positionShake = Vector3.Scale(new Vector3(
            Mathf.Sin(time * 1.17f),
            Mathf.Sin(time * 1.41f),
            Mathf.Sin(time * 0.83f)), activeShakeProfile.positionAmplitude);
        rotationShake = Vector3.Scale(new Vector3(
            Mathf.Sin(time * 0.91f),
            Mathf.Sin(time * 1.29f),
            Mathf.Sin(time * 1.53f)), activeShakeProfile.rotationAmplitude);
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
