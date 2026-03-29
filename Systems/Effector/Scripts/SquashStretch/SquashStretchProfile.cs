using UnityEngine;

/// <summary>
/// Data asset defining a squash/stretch animation.
/// Curves are scale multipliers (1.0 = no change) evaluated over normalized time.
/// </summary>
[CreateAssetMenu(menuName = "JGameFramework/Effects/Squash Stretch Profile")]
public class SquashStretchProfile : ScriptableObject
{
    [Tooltip("Duration of the squash/stretch animation in seconds.")]
    public float duration = 0.15f;

    [Tooltip("Scale multiplier for X axis over normalized time (0..1). 1.0 = identity.")]
    public AnimationCurve scaleX = AnimationCurve.Constant(0f, 1f, 1f);

    [Tooltip("Scale multiplier for Y axis over normalized time (0..1). 1.0 = identity.")]
    public AnimationCurve scaleY = AnimationCurve.Constant(0f, 1f, 1f);

    [Tooltip("How to handle re-triggers while already playing.")]
    public RetriggerMode retriggerMode = RetriggerMode.Restart;
}

public enum RetriggerMode
{
    /// <summary>Reset timer and restart the animation from the beginning.</summary>
    Restart,
    /// <summary>Ignore the trigger if an animation is already playing.</summary>
    Ignore
}
