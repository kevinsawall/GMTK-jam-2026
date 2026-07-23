using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SceneLoader : MonoBehaviour
{
    private const string LoadingSceneName = "04_LoadingScreen";

    private static SceneLoader instance;

    private bool isLoading;

    public static void Load(string targetSceneName)
    {
        if (string.IsNullOrWhiteSpace(targetSceneName))
        {
            Debug.LogError("A target scene name is required.");
            return;
        }

        EnsureInstance().BeginLoad(targetSceneName);
    }

    private static SceneLoader EnsureInstance()
    {
        if (instance != null)
        {
            return instance;
        }

        GameObject loaderObject = new GameObject("SceneLoader");
        instance = loaderObject.AddComponent<SceneLoader>();
        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void BeginLoad(string targetSceneName)
    {
        if (isLoading)
        {
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(targetSceneName))
        {
            Debug.LogError($"Scene '{targetSceneName}' is not available in Build Settings.", this);
            return;
        }

        StartCoroutine(LoadRoutine(targetSceneName));
    }

    private IEnumerator LoadRoutine(string targetSceneName)
    {
        isLoading = true;
        Time.timeScale = 1f;

        if (SceneManager.GetActiveScene().name != LoadingSceneName)
        {
            yield return SceneManager.LoadSceneAsync(LoadingSceneName, LoadSceneMode.Single);
            yield return null;
        }

        yield return SceneManager.LoadSceneAsync(targetSceneName, LoadSceneMode.Single);
        isLoading = false;
    }
}
