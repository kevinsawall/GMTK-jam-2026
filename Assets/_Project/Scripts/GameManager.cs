using System.Collections.Generic;
using UnityEngine;

public sealed class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private List<CutsceneController> cutscenes = new();

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        PlayCutscene(CutsceneType.StartGame);
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
}
