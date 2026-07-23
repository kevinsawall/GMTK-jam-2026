using UnityEngine;

public sealed class FrameRateController : MonoBehaviour
{
    public static FrameRateController Instance { get; private set; }

    [SerializeField, Min(1)] private int targetFrameRate = 60;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = targetFrameRate;
    }
}
