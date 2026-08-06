using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Adds subtle PS1-style sway and gameplay-driven shake in Cinemachine's Noise stage.
/// This keeps shake inside the camera pipeline, so it is applied after follow and aim.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CinemachineCamera))]
public sealed class CinemachineCameraShake : CinemachineExtension
{
    [System.Serializable]
    public sealed class ShakeProfile
    {
        public Vector3 positionAmplitude;
        public Vector3 rotationAmplitude;
        [Min(0.01f)] public float frequency = 16f;
    }

    private Vector3 positionSway;
    private Vector3 rotationSway;
    private Vector3 currentPositionSway;
    private Vector3 currentRotationSway;
    private Vector3 positionSwayFrom;
    private Vector3 positionSwayTo;
    private Vector3 rotationSwayFrom;
    private Vector3 rotationSwayTo;
    private float minimumSwayDuration;
    private float maximumSwayDuration;
    private bool trainSwayEnabled;
    private ShakeProfile activeShakeProfile;
    private float eventShakeStartedAt;

    public void ConfigureTrainSway(
        bool enabled,
        Vector3 newPositionSway,
        Vector3 newRotationSway,
        float minDuration,
        float maxDuration)
    {
        bool settingsChanged = trainSwayEnabled != enabled
            || positionSway != newPositionSway
            || rotationSway != newRotationSway
            || !Mathf.Approximately(minimumSwayDuration, minDuration)
            || !Mathf.Approximately(maximumSwayDuration, maxDuration);

        trainSwayEnabled = enabled;
        positionSway = newPositionSway;
        rotationSway = newRotationSway;
        minimumSwayDuration = Mathf.Max(0.1f, minDuration);
        maximumSwayDuration = Mathf.Max(0.1f, maxDuration);

        if (settingsChanged) ResetSway();
    }

    public void StartShake(ShakeProfile profile)
    {
        activeShakeProfile = profile;
        eventShakeStartedAt = Time.unscaledTime;
    }

    public void StopShake() => activeShakeProfile = null;

    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase virtualCamera,
        CinemachineCore.Stage stage,
        ref CameraState state,
        float deltaTime)
    {
        if (stage != CinemachineCore.Stage.Noise) return;

        Vector3 eventPosition = Vector3.zero;
        Vector3 eventRotation = Vector3.zero;
        if (activeShakeProfile != null)
        {
            float time = (Time.unscaledTime - eventShakeStartedAt) * activeShakeProfile.frequency;
            eventPosition = Vector3.Scale(new Vector3(
                Mathf.Sin(time * 1.17f), Mathf.Sin(time * 1.41f), Mathf.Sin(time * 0.83f)),
                activeShakeProfile.positionAmplitude);
            eventRotation = Vector3.Scale(new Vector3(
                Mathf.Sin(time * 0.91f), Mathf.Sin(time * 1.29f), Mathf.Sin(time * 1.53f)),
                activeShakeProfile.rotationAmplitude);
        }

        state.PositionCorrection += currentPositionSway + eventPosition;
        state.OrientationCorrection *= Quaternion.Euler(currentRotationSway + eventRotation);
    }

    private void ResetSway()
    {
        LeanTween.cancel(gameObject);
        currentPositionSway = Vector3.zero;
        currentRotationSway = Vector3.zero;
        if (trainSwayEnabled && isActiveAndEnabled) StartNextSway();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        if (trainSwayEnabled) StartNextSway();
    }

    private void OnDisable()
    {
        LeanTween.cancel(gameObject);
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
