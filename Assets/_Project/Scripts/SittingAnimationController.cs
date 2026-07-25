using UnityEngine;

/// <summary>Assigns a looping sitting controller to the Animator on an attached character model.</summary>
public sealed class SittingAnimationController : MonoBehaviour
{
    [SerializeField] private RuntimeAnimatorController sittingController;

    private void Awake()
    {
        Animator animator = GetComponentInChildren<Animator>();
        if (animator != null && sittingController != null)
        {
            animator.runtimeAnimatorController = sittingController;
        }
    }
}
