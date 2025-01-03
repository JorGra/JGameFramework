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
    public bool CooldownReady()
    {
        return Time.time >= nextReadyTime;
    }

    /// <summary>
    /// Resets the cooldown using the originally set duration.
    /// </summary>
    public void ResetCooldown()
    {
        nextReadyTime = Time.time + cooldownDuration;
    }

    /// <summary>
    /// Resets the cooldown to a completely new duration.
    /// </summary>
    public void ResetCooldown(float newCooldownDuration)
    {
        cooldownDuration = newCooldownDuration;
        nextReadyTime = Time.time + newCooldownDuration;
    }

    /// <summary>
    /// Returns how much time is left before the cooldown finishes.
    /// </summary>
    public float CooldownRemaining()
    {
        return Mathf.Max(0f, nextReadyTime - Time.time);
    }
}
