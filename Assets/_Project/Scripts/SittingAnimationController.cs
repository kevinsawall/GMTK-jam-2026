using System.Collections.Generic;
using UnityEngine;

/// <summary>Assigns a character-specific sitting clip through the shared sitting controller.</summary>
public sealed class SittingAnimationController : MonoBehaviour
{
    [SerializeField] private RuntimeAnimatorController sittingController;
    [SerializeField] private AnimationClip sittingClip;

    private void Awake()
    {
        if (GetComponent<CharacterManager>()?.Type == CharacterManager.CharacterType.Player) return;

        Animator animator = GetComponentInChildren<Animator>(true);
        if (animator == null || sittingController == null) return;

        if (sittingClip == null)
        {
            animator.runtimeAnimatorController = sittingController;
            return;
        }

        AnimatorOverrideController overrideController = new(sittingController);
        List<KeyValuePair<AnimationClip, AnimationClip>> clips = new();
        overrideController.GetOverrides(clips);
        for (int i = 0; i < clips.Count; i++)
        {
            if (clips[i].Key.name == "Sitting")
            {
                clips[i] = new KeyValuePair<AnimationClip, AnimationClip>(clips[i].Key, sittingClip);
                break;
            }
        }

        overrideController.ApplyOverrides(clips);
        animator.runtimeAnimatorController = overrideController;
    }
}
