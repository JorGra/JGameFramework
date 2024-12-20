using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

namespace JG.Audio
{
    public class MusicController : MonoBehaviour
    {
        [SerializeField] private AudioMixerGroup audioMixerGroup;

        private AudioSource sourceA;
        private AudioSource sourceB;

        private bool isSourceAActive = true;
        private float masterVolume = 1f;
        private float masterPitch = 1f;
        private float internalVolumeA = 0f;
        private float internalVolumeB = 0f;

        private Coroutine masterVolumeCoroutine;
        private Coroutine masterPitchCoroutine;
        private Coroutine volumeACoroutine;
        private Coroutine volumeBCoroutine;

        public AudioClip CurrentClip
        {
            get
            {
                var activeSource = isSourceAActive ? sourceA : sourceB;
                return activeSource.clip;
            }
        }

        public bool IsPlaying
        {
            get
            {
                var activeSource = isSourceAActive ? sourceA : sourceB;
                return activeSource.isPlaying;
            }
        }

        public void Init(AudioMixerGroup mixerGroup)
        {
            audioMixerGroup = mixerGroup;
            sourceA = gameObject.AddComponent<AudioSource>();
            sourceB = gameObject.AddComponent<AudioSource>();

            sourceA.outputAudioMixerGroup = audioMixerGroup;
            sourceB.outputAudioMixerGroup = audioMixerGroup;

            sourceA.loop = false;
            sourceB.loop = false;
            sourceA.playOnAwake = false;
            sourceB.playOnAwake = false;
        }

        private void Update()
        {
            sourceA.volume = internalVolumeA * masterVolume;
            sourceB.volume = internalVolumeB * masterVolume;
            sourceA.pitch = masterPitch;
            sourceB.pitch = masterPitch;
        }

        public void PlayClip(AudioClip clip, bool fadeIn = false, float fadeDuration = 2f)
        {
            // Use SourceA by default for starting
            isSourceAActive = true;
            sourceA.clip = clip;
            sourceA.Play();
            internalVolumeA = fadeIn ? 0f : 1f;
            internalVolumeB = 0f;

            if (fadeIn)
            {
                FadeInternalVolumeA(internalVolumeA, 1f, fadeDuration);
            }
        }

        public void CrossfadeToClip(AudioClip newClip, float duration)
        {
            var activeSource = isSourceAActive ? sourceA : sourceB;
            var inactiveSource = isSourceAActive ? sourceB : sourceA;

            inactiveSource.clip = newClip;
            inactiveSource.Play();

            // Fade out active and in inactive
            if (isSourceAActive)
            {
                FadeInternalVolumeA(internalVolumeA, 0f, duration);
                FadeInternalVolumeB(internalVolumeB, 1f, duration);
            }
            else
            {
                FadeInternalVolumeB(internalVolumeB, 0f, duration);
                FadeInternalVolumeA(internalVolumeA, 1f, duration);
            }

            isSourceAActive = !isSourceAActive;
        }

        public void PauseActiveSource()
        {
            var activeSource = isSourceAActive ? sourceA : sourceB;
            activeSource.Pause();
        }

        public void UnPauseActiveSource()
        {
            var activeSource = isSourceAActive ? sourceA : sourceB;
            activeSource.UnPause();
        }

        public void StopActive(float fadeDuration = 0f)
        {
            var activeSource = isSourceAActive ? sourceA : sourceB;
            if (fadeDuration > 0f)
            {
                // Fade out then stop
                StartCoroutine(FadeValueRoutine(() => masterVolume, v => masterVolume = v, 0f, fadeDuration,
                    () => { activeSource.Stop(); masterVolume = 1f; }));
            }
            else
            {
                activeSource.Stop();
            }
        }

        public void SetMasterVolumeInstant(float vol)
        {
            masterVolume = vol;
        }

        public void FadeMasterVolume(float target, float duration)
        {
            if (masterVolumeCoroutine != null) StopCoroutine(masterVolumeCoroutine);
            masterVolumeCoroutine = StartCoroutine(FadeValueRoutine(() => masterVolume, v => masterVolume = v, target, duration));
        }

        public void FadeMasterPitch(float target, float duration)
        {
            if (masterPitchCoroutine != null) StopCoroutine(masterPitchCoroutine);
            masterPitchCoroutine = StartCoroutine(FadeValueRoutine(() => masterPitch, v => masterPitch = v, target, duration));
        }

        private void FadeInternalVolumeA(float start, float end, float duration)
        {
            if (volumeACoroutine != null) StopCoroutine(volumeACoroutine);
            volumeACoroutine = StartCoroutine(FadeValueRoutine(() => internalVolumeA, v => internalVolumeA = v, end, duration));
        }

        private void FadeInternalVolumeB(float start, float end, float duration)
        {
            if (volumeBCoroutine != null) StopCoroutine(volumeBCoroutine);
            volumeBCoroutine = StartCoroutine(FadeValueRoutine(() => internalVolumeB, v => internalVolumeB = v, end, duration));
        }

        private IEnumerator FadeValueRoutine(System.Func<float> getter, System.Action<float> setter, float end, float duration, System.Action onComplete = null)
        {
            float start = getter();
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                setter(Mathf.Lerp(start, end, elapsed / duration));
                yield return null;
            }
            setter(end);
            onComplete?.Invoke();
        }
    }
}
