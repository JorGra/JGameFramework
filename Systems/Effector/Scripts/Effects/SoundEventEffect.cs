using JG.Audio;
using System.Collections;
using UnityEngine;

/// <summary>
/// Fires a sound event to a custom event system or manager.
/// Optionally, can directly play an AudioSource if you prefer local audio playback.
/// </summary>
public class SoundEventEffect : EffectBase
{
    [Header("Sound Settings")]
    [SerializeField] SoundData soundData;
    [SerializeField] bool randomPitch = false;
    [SerializeField] Vector2 randomPitchRange = new Vector2(-0.05f, 0.05f);

    protected override IEnumerator PlayEffectLogic()
    {
        var soundEvent = randomPitch
            ? new PlaySoundEvent(soundData, transform.position, randomPitchRange)
            : new PlaySoundEvent(soundData, transform.position);

        EventBus<PlaySoundEvent>.Raise(soundEvent);
        yield return null;
    }
}
