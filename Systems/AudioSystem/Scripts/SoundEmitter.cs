using JG.Util.Extensions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace JG.Audio
{
    [RequireComponent(typeof(AudioSource))]
    public class SoundEmitter : MonoBehaviour
    {
        public SoundData Data { get; private set; }
        public SoundMixerGroup MixerGroupType { get; private set; } = SoundMixerGroup.Effects;
        public LinkedListNode<SoundEmitter> Node { get; set; }

        AudioSource audioSource;
        Coroutine playingCoroutine;
        float baseVolume = 1f;

        void Awake()
        {
            audioSource = gameObject.GetOrAdd<AudioSource>();
        }

        public void Initialize(SoundData data)
        {
            Data = data;
            baseVolume = data != null ? data.volume : 1f;
            MixerGroupType = data != null ? data.mixerGroupType : SoundMixerGroup.Effects;

            audioSource.clip = data.clip;
            var outputGroup = data.mixerGroup;
            if (outputGroup == null && SoundManager.Instance != null)
            {
                outputGroup = SoundManager.Instance.ResolveMixerGroup(MixerGroupType);
            }
            audioSource.outputAudioMixerGroup = outputGroup;
            audioSource.loop = data.loop;
            audioSource.playOnAwake = data.playOnAwake;

            audioSource.mute = data.mute;
            audioSource.bypassEffects = data.bypassEffects;
            audioSource.bypassListenerEffects = data.bypassListenerEffects;
            audioSource.bypassReverbZones = data.bypassReverbZones;

            audioSource.priority = data.priority;
            ApplyVolumeMultiplier(
                SoundManager.TryGetInstance()?.GetVolumeMultiplier(MixerGroupType) ?? 1f);
            audioSource.pitch = data.pitch;
            audioSource.panStereo = data.panStereo;
            audioSource.spatialBlend = data.spatialBlend;
            audioSource.reverbZoneMix = data.reverbZoneMix;
            audioSource.dopplerLevel = data.dopplerLevel;
            audioSource.spread = data.spread;

            audioSource.minDistance = data.minDistance;
            audioSource.maxDistance = data.maxDistance;

            audioSource.ignoreListenerVolume = data.ignoreListenerVolume;
            audioSource.ignoreListenerPause = data.ignoreListenerPause;

            audioSource.rolloffMode = data.rolloffMode;
        }

        public void Play()
        {
            if (playingCoroutine != null)
            {
                StopCoroutine(playingCoroutine);
            }

            audioSource.Play();
            playingCoroutine = StartCoroutine(WaitForSoundToEnd());
        }

        IEnumerator WaitForSoundToEnd()
        {
            yield return new WaitWhile(() => audioSource.isPlaying);
            Stop();
        }

        public void Stop()
        {
            if (playingCoroutine != null)
            {
                StopCoroutine(playingCoroutine);
                playingCoroutine = null;
            }

            audioSource.Stop();
            SoundManager.Instance.ReturnToPool(this);
        }

        public void WithRandomPitch(Vector2 pitchRange)
        {
            audioSource.pitch += Random.Range(pitchRange.x, pitchRange.y);
        }

        public void ApplyVolumeMultiplier(float multiplier)
        {
            audioSource.volume = baseVolume * Mathf.Max(0f, multiplier);
        }
    }
}
