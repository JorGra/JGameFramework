using UnityEngine;

[System.Serializable]
public class Cooldown
{
    [SerializeField] private float cooldownDuration;
    private float nextReadyTime;

    public Cooldown(float duration)
    {
        cooldownDuration = duration;
        nextReadyTime = 0f;
    }

    /// <summary>
    /// Returns true if the cooldown period has passed.
    /// </summary>
    public bool Ready()
    {
        return Time.time >= nextReadyTime;
    }

    /// <summary>
    /// Resets the cooldown using the originally set duration.
    /// </summary>
    public void Reset()
    {
        nextReadyTime = Time.time + cooldownDuration;
    }

    /// <summary>
    /// Resets the cooldown to a completely new duration.
    /// </summary>
    public void Reset(float newCooldownDuration)
    {
        cooldownDuration = newCooldownDuration;
        nextReadyTime = Time.time + newCooldownDuration;
    }

    /// <summary>
    /// Returns how much time is left before the cooldown finishes.
    /// </summary>
    public float RemainingTime()
    {
        return Mathf.Max(0f, nextReadyTime - Time.time);
    }
}
