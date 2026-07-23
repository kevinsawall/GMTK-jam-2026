using System;
using UnityEngine;

/// <summary>Plays the opening overlay before gameplay becomes interactive.</summary>
public sealed class StartCutsceneController : MonoBehaviour
{
    private const string StartCutsceneObjectName = "StartCutscentObject";
    private const float FadeDurationSeconds = 2f;

    private static StartCutsceneController instance;
    private CanvasGroup canvasGroup;
    private bool isPlaying;

    public static bool IsPlaying => instance != null && instance.isPlaying;
    public static event Action Finished;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void PlayForGameplayScene()
    {
        foreach (Transform transform in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (transform.name != StartCutsceneObjectName || !transform.gameObject.scene.IsValid()) continue;

            StartCutsceneController controller = transform.GetComponent<StartCutsceneController>();
            if (controller == null) controller = transform.gameObject.AddComponent<StartCutsceneController>();
            transform.gameObject.SetActive(true);
            return;
        }
    }

    private void Awake()
    {
        instance = this;
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        isPlaying = true;
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        LeanTween.alphaCanvas(canvasGroup, 0f, FadeDurationSeconds)
            .setIgnoreTimeScale(true)
            .setOnComplete(Finish);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private void Finish()
    {
        isPlaying = false;
        Finished?.Invoke();
        gameObject.SetActive(false);
    }
}
