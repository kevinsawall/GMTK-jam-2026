using UnityEngine;

/// <summary>Keeps the game's default cursor active in the editor and standalone builds.</summary>
[DisallowMultipleComponent]
public sealed class DefaultCursorController : MonoBehaviour
{
    [SerializeField] private Texture2D defaultCursor;
    [SerializeField] private Vector2 hotspot = new(2f, 1f);

    private static DefaultCursorController instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        ApplyCursor();
    }

    private void OnEnable() => ApplyCursor();

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus) ApplyCursor();
    }

    private void ApplyCursor()
    {
        if (defaultCursor == null) return;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Cursor.SetCursor(defaultCursor, hotspot, CursorMode.Auto);
    }
}
