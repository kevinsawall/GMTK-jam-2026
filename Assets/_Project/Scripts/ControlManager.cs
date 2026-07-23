using UnityEngine;
using UnityEngine.InputSystem;

public sealed class ControlManager : MonoBehaviour
{
    [SerializeField] private PauseMenuController pauseMenuController;

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            pauseMenuController.TogglePause();
        }
    }
}
