using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class GameManager : MonoBehaviour
{
    private const float StartDestructionDelaySeconds = 7f;

    public static GameManager Instance { get; private set; }
    public static bool IsEndGameSequencePlaying => Instance != null && Instance.isEndGameSequencePlaying;

    [SerializeField] private List<CutsceneController> cutscenes = new();
    [Header("End Game")]
    [SerializeField] private string endGameFlag = "end-game";
    [SerializeField, Min(0f)] private float endGameShakeLeadSeconds = 1f;
    [SerializeField, Min(0f)] private float endSceneTransitionLeadSeconds = 1f;
    [SerializeField] private string endScreenSceneName = "03_EndScreen";

    private bool isEndGameSequencePlaying;
    private bool isEndSceneLoadScheduled;

    private void Awake()
    {
        Instance = this;

        DialogueManager.FlagSet += HandleFlagSet;
        CutsceneController.StartGameFinished += HandleStartGameFinished;
    }

    private void OnDestroy()
    {
        DialogueManager.FlagSet -= HandleFlagSet;
        CutsceneController.StartGameFinished -= HandleStartGameFinished;
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        AudioManager.Instance?.PlayLoopingSfx(SfxId.TrainOnTheRun);

        if (PlayCutscene(CutsceneType.StartGame))
        {
            StartCoroutine(PlayStartDestructionAfterDelay());
        }
    }

    public bool PlayCutscene(CutsceneType cutsceneType)
    {
        foreach (CutsceneController cutscene in cutscenes)
        {
            if (cutscene == null || cutscene.Type != cutsceneType) continue;

            cutscene.gameObject.SetActive(true);
            return true;
        }

        Debug.LogWarning($"No {cutsceneType} cutscene is assigned to the GameManager.", this);
        return false;
    }

    private void HandleFlagSet(string flag)
    {
        if (flag != endGameFlag || isEndGameSequencePlaying) return;

        isEndGameSequencePlaying = true;
        CupTimerController.Instance?.CancelForEndGame();
        StartCoroutine(RunEndGameSequence());
    }

    private void HandleStartGameFinished()
    {
        AudioManager.Instance?.PlayMusic(MusicId.Gameplay);
    }

    private IEnumerator PlayStartDestructionAfterDelay()
    {
        yield return new WaitForSecondsRealtime(StartDestructionDelaySeconds);
        AudioManager.Instance?.PauseLoopingSfxForOneShot(SfxId.TrainOnTheRun, SfxId.TrainDestruction);
    }

    private IEnumerator RunEndGameSequence()
    {
        while (DialogueManager.Instance != null && DialogueManager.Instance.IsOpen)
        {
            yield return null;
        }

        CameraFollowPlayer cameraFollow = Object.FindFirstObjectByType<CameraFollowPlayer>(FindObjectsInactive.Exclude);
        cameraFollow?.StartEndGameShake();
        yield return new WaitForSecondsRealtime(endGameShakeLeadSeconds);
        cameraFollow?.StopEventShake();

        CutsceneController endCutscene = FindCutscene(CutsceneType.EndGame);
        if (endCutscene == null)
        {
            isEndGameSequencePlaying = false;
            Debug.LogWarning("No EndGame cutscene is assigned to the GameManager.", this);
            yield break;
        }

        endCutscene.gameObject.SetActive(true);
        float transitionTime = Mathf.Max(0f, endCutscene.TotalDurationSeconds - endSceneTransitionLeadSeconds);
        yield return new WaitForSecondsRealtime(transitionTime);

        if (isEndSceneLoadScheduled) yield break;

        isEndSceneLoadScheduled = true;
        SceneManager.LoadScene(endScreenSceneName);
    }

    private CutsceneController FindCutscene(CutsceneType cutsceneType)
    {
        foreach (CutsceneController cutscene in cutscenes)
        {
            if (cutscene != null && cutscene.Type == cutsceneType) return cutscene;
        }

        return null;
    }
}
