using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;
using UnityEngine;

/// <summary>
/// Configures the gameplay Cinemachine camera and preserves the public shake API used by gameplay.
/// Camera positioning and aiming are performed by CinemachineFollow and
/// CinemachineRotationComposer; this component never moves a camera transform directly.
/// </summary>
[RequireComponent(typeof(CinemachineCamera))]
[RequireComponent(typeof(CinemachineFollow))]
[RequireComponent(typeof(CinemachineRotationComposer))]
[RequireComponent(typeof(CinemachineCameraShake))]
public sealed class CameraFollowPlayer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform mainCamera;

    [Header("PS1 Follow")]
    [SerializeField] private Vector3 followOffset = new(-3.05f, 7.58f, -4.37f);
    [SerializeField] private Vector3 lookAtOffset = new(-0.36f, 1.3f, -0.61f);
    [SerializeField, Min(0f)] private float followSmoothTime = 0.12f;

    [Header("PS1 Camera Sway")]
    [SerializeField] private bool trainSwayEnabled = true;
    [SerializeField] private Vector3 positionSway = new(0.04f, 0.025f, 0.02f);
    [SerializeField] private Vector3 rotationSway = new(0.25f, 0.2f, 0.4f);
    [SerializeField, Min(0.1f)] private float minimumSwayDuration = 0.7f;
    [SerializeField, Min(0.1f)] private float maximumSwayDuration = 1.6f;

    [Header("Event Shake")]
    [SerializeField] private CinemachineCameraShake.ShakeProfile pseudoResetShake = new()
    {
        positionAmplitude = new Vector3(0.14f, 0.08f, 0.06f),
        rotationAmplitude = new Vector3(1.2f, 0.8f, 1.6f),
        frequency = 20f
    };
    [SerializeField] private CinemachineCameraShake.ShakeProfile endGameShake = new()
    {
        positionAmplitude = new Vector3(0.035f, 0.02f, 0.015f),
        rotationAmplitude = new Vector3(0.3f, 0.2f, 0.45f),
        frequency = 12f
    };

    private CinemachineCamera virtualCamera;
    private CinemachineFollow follow;
    private CinemachineRotationComposer composer;
    private CinemachineCameraShake shake;

    private void Awake()
    {
        virtualCamera = GetComponent<CinemachineCamera>();
        follow = GetComponent<CinemachineFollow>();
        composer = GetComponent<CinemachineRotationComposer>();
        shake = GetComponent<CinemachineCameraShake>();
        ApplySettings();

        if (mainCamera != null && mainCamera.GetComponent<CinemachineBrain>() == null)
        {
            Debug.LogWarning("The gameplay Main Camera needs a CinemachineBrain component.", this);
        }
    }

    private void OnValidate()
    {
        if (virtualCamera == null) virtualCamera = GetComponent<CinemachineCamera>();
        if (follow == null) follow = GetComponent<CinemachineFollow>();
        if (composer == null) composer = GetComponent<CinemachineRotationComposer>();
        if (shake == null) shake = GetComponent<CinemachineCameraShake>();
        ApplySettings();
    }

    /// <summary>Starts the stronger shake used while the pseudo-reset countdown is active.</summary>
    public void StartPseudoResetShake() => shake?.StartShake(pseudoResetShake);

    /// <summary>Starts the gentler shake used before the end-game cutscene.</summary>
    public void StartEndGameShake() => shake?.StartShake(endGameShake);

    public void StopEventShake() => shake?.StopShake();

    private void ApplySettings()
    {
        if (virtualCamera == null || follow == null || composer == null || shake == null) return;

        virtualCamera.Follow = player;
        virtualCamera.LookAt = player;

        follow.FollowOffset = followOffset;
        TrackerSettings tracker = follow.TrackerSettings;
        tracker.BindingMode = BindingMode.WorldSpace;
        tracker.PositionDamping = Vector3.one * followSmoothTime;
        follow.TrackerSettings = tracker;

        composer.TargetOffset = lookAtOffset;
        composer.Damping = Vector2.one * followSmoothTime;

        shake.ConfigureTrainSway(
            trainSwayEnabled,
            positionSway,
            rotationSway,
            minimumSwayDuration,
            maximumSwayDuration);
    }
}
