using UnityEngine;

/// <summary>
/// Receives animation events on the Animator GameObject and relays them to the player.
/// The Animator is part of the imported character model, rather than the player root.
/// </summary>
public sealed class FootstepAnimationEvents : MonoBehaviour
{
    private PlayerMovement playerMovement;

    private void Awake()
    {
        playerMovement = GetComponentInParent<PlayerMovement>();
    }

    public void PlayFootstep()
    {
        playerMovement?.PlayFootstep();
    }
}
