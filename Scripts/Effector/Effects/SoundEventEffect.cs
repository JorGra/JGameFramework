using System.Collections;
using UnityEngine;

/// <summary>
/// Fires a sound event to a custom event system or manager.
/// Optionally, can directly play an AudioSource if you prefer local audio playback.
/// </summary>
public class SoundEventEffect : EffectBase
{
    [Header("Sound Settings")]
    [Tooltip("Name/ID of the sound event to trigger, or an AudioClip reference.")]
    public string soundEventName = "ExplosionSound";

    [Tooltip("Optional AudioSource to directly play a clip instead of sending an event.")]
    public AudioSource audioSource;

    [Tooltip("Optional AudioClip to play on the AudioSource.")]
    public AudioClip clip;

    protected override IEnumerator PlayEffectLogic()
    {
        // Option 1: Fire an event to your custom event bus or audio manager
        // (Make sure you have a system that listens for this event.)
        // EventBus.Publish(new SoundPlayEvent(soundEventName));
        Debug.Log($"[SoundEventEffect]: Event triggered -> {soundEventName}");

        // Option 2: Directly play a clip on an AudioSource
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }

        // We won't wait here, but you could do:
        // yield return new WaitForSeconds(clip.length);
        // if you wanted the effect to block until sound finishes
        yield return null;
    }
}
