using JG.Tools; // For PersistentSingleton if you use this pattern
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

namespace JG.Audio
{

    public struct ChangePlaylistEvent : IEvent
    {
        public PlaylistSO NewPlaylist;
    }

    public struct NextTrackEvent : IEvent { }
    public struct PreviousTrackEvent : IEvent { }
    public struct PauseMusicEvent : IEvent
    {
        public float FadeDuration;
    }
    public struct ChangeMusicVolumeEvent : IEvent
    {
        public float TargetVolume; // e.g. >1.0 to intensify, <1.0 to deintensify
        public float Duration;
    }
    public struct ChangeMusicPitchEvent : IEvent
    {
        public float TargetPitch; // e.g. <1.0 to muffle
        public float Duration;
    }
    public struct ResetMusicSettingsEvent : IEvent
    {
        public float Duration;
    }

    public class BackgroundMusicManager : PersistentSingleton<BackgroundMusicManager>
    {
        [SerializeField] private PlaylistSO defaultPlaylist;
        [SerializeField] private AudioMixerGroup audioMixerGroup;
        [SerializeField] private float startFadeInDuration = 2f;

        private AudioSource sourceA;
        private AudioSource sourceB;

        private bool isSourceAActive = true;
        private PlaylistSO currentPlaylist;
        private int currentTrackIndex;
        private float crossfadeDuration = 2f;

        // For displaying in editor
        public PlaylistSO CurrentPlaylist => currentPlaylist;
        public string CurrentTrackName
        {
            get
            {
                if (currentPlaylist == null || currentPlaylist.Tracks.Length == 0 || currentTrackIndex < 0) return "None";
                return currentPlaylist.Tracks[currentTrackIndex] != null ? currentPlaylist.Tracks[currentTrackIndex].name : "None";
            }
        }

        // Master volume and pitch adjustments
        private float masterVolume = 1f;
        private float masterPitch = 1f;
        private Coroutine volumeAdjustCoroutine;
        private Coroutine pitchAdjustCoroutine;

        // Internal track volumes for crossfading
        private float internalVolumeA = 0f;
        private float internalVolumeB = 0f;

        EventBinding<ChangePlaylistEvent> changePlaylistBinding;
        EventBinding<NextTrackEvent> nextTrackBinding;
        EventBinding<PreviousTrackEvent> previousTrackBinding;
        EventBinding<PauseMusicEvent> pauseMusicBinding;
        EventBinding<ChangeMusicVolumeEvent> changeVolumeBinding;
        EventBinding<ChangeMusicPitchEvent> changePitchBinding;
        EventBinding<ResetMusicSettingsEvent> resetSettingsBinding;

        protected override void Awake()
        {
            base.Awake();
            DontDestroyOnLoad(gameObject);

            // Setup AudioSources
            sourceA = gameObject.AddComponent<AudioSource>();
            sourceB = gameObject.AddComponent<AudioSource>();

            sourceA.outputAudioMixerGroup = audioMixerGroup;
            sourceB.outputAudioMixerGroup = audioMixerGroup;

            sourceA.loop = false;
            sourceB.loop = false;
            sourceA.playOnAwake = false;
            sourceB.playOnAwake = false;

            // Register events
            changePlaylistBinding = new EventBinding<ChangePlaylistEvent>(OnChangePlaylist);
            EventBus<ChangePlaylistEvent>.Register(changePlaylistBinding);

            nextTrackBinding = new EventBinding<NextTrackEvent>(OnNextTrack);
            EventBus<NextTrackEvent>.Register(nextTrackBinding);

            previousTrackBinding = new EventBinding<PreviousTrackEvent>(OnPreviousTrack);
            EventBus<PreviousTrackEvent>.Register(previousTrackBinding);

            pauseMusicBinding = new EventBinding<PauseMusicEvent>(OnPauseMusic);
            EventBus<PauseMusicEvent>.Register(pauseMusicBinding);

            changeVolumeBinding = new EventBinding<ChangeMusicVolumeEvent>(OnChangeMusicVolume);
            EventBus<ChangeMusicVolumeEvent>.Register(changeVolumeBinding);

            changePitchBinding = new EventBinding<ChangeMusicPitchEvent>(OnChangeMusicPitch);
            EventBus<ChangeMusicPitchEvent>.Register(changePitchBinding);

            resetSettingsBinding = new EventBinding<ResetMusicSettingsEvent>(OnResetMusicSettings);
            EventBus<ResetMusicSettingsEvent>.Register(resetSettingsBinding);

            // Start with default playlist
            SetPlaylist(defaultPlaylist);
            StartPlayback(true);
        }

        private void OnDestroy()
        {
            EventBus<ChangePlaylistEvent>.Deregister(changePlaylistBinding);
            EventBus<NextTrackEvent>.Deregister(nextTrackBinding);
            EventBus<PreviousTrackEvent>.Deregister(previousTrackBinding);
            EventBus<PauseMusicEvent>.Deregister(pauseMusicBinding);
            EventBus<ChangeMusicVolumeEvent>.Deregister(changeVolumeBinding);
            EventBus<ChangeMusicPitchEvent>.Deregister(changePitchBinding);
            EventBus<ResetMusicSettingsEvent>.Deregister(resetSettingsBinding);
        }

        private void Update()
        {
            // Update source volumes and pitch
            sourceA.volume = internalVolumeA * masterVolume;
            sourceB.volume = internalVolumeB * masterVolume;

            sourceA.pitch = masterPitch;
            sourceB.pitch = masterPitch;

            // Check track end
            AudioSource activeSource = isSourceAActive ? sourceA : sourceB;
            if (!activeSource.isPlaying && currentPlaylist != null && currentPlaylist.Tracks.Length > 0)
            {
                PlayNextTrack();
            }
        }

        // ---------- Event Handlers ----------
        private void OnChangePlaylist(ChangePlaylistEvent e)
        {
            PlaylistSO newPlaylist = e.NewPlaylist != null ? e.NewPlaylist : defaultPlaylist;
            SwitchToPlaylist(newPlaylist);
        }

        private void OnNextTrack(NextTrackEvent e)
        {
            ForceNextTrack();
        }

        private void OnPreviousTrack(PreviousTrackEvent e)
        {
            ForcePreviousTrack();
        }

        private void OnPauseMusic(PauseMusicEvent e)
        {
            PauseCurrentSong(e.FadeDuration);
        }

        private void OnChangeMusicVolume(ChangeMusicVolumeEvent e)
        {
            AdjustMasterVolume(e.TargetVolume, e.Duration);
        }

        private void OnChangeMusicPitch(ChangeMusicPitchEvent e)
        {
            AdjustMasterPitch(e.TargetPitch, e.Duration);
        }

        private void OnResetMusicSettings(ResetMusicSettingsEvent e)
        {
            ResetMasterSettings(e.Duration);
        }

        // ---------- Core Methods ----------
        private void SetPlaylist(PlaylistSO playlist)
        {
            currentPlaylist = playlist != null ? playlist : defaultPlaylist;
            crossfadeDuration = currentPlaylist != null ? currentPlaylist.CrossfadeDuration : 2f;
            currentTrackIndex = -1;
        }

        private void StartPlayback(bool fadeIn)
        {
            if (currentPlaylist == null || currentPlaylist.Tracks.Length == 0) return;
            currentTrackIndex = GetNextTrackIndex();
            AudioClip clip = currentPlaylist.Tracks[currentTrackIndex];
            sourceA.clip = clip;
            sourceA.Play();
            internalVolumeA = fadeIn ? 0f : 1f;
            internalVolumeB = 0f;

            if (fadeIn)
            {
                StartCoroutine(FadeInternalVolume(val => internalVolumeA = val, internalVolumeA, 1f, startFadeInDuration));
            }

            isSourceAActive = true;
        }

        private void PlayNextTrack()
        {
            CrossfadeToNewTrack();
        }

        private void SwitchToPlaylist(PlaylistSO newPlaylist)
        {
            if (newPlaylist == currentPlaylist) return;
            SetPlaylist(newPlaylist);
            CrossfadeToNewTrack();
        }

        private void CrossfadeToNewTrack()
        {
            if (currentPlaylist == null || currentPlaylist.Tracks.Length == 0) return;
            AudioSource inactiveSource = isSourceAActive ? sourceB : sourceA;
            AudioSource activeSource = isSourceAActive ? sourceA : sourceB;

            currentTrackIndex = GetNextTrackIndex();
            AudioClip newClip = currentPlaylist.Tracks[currentTrackIndex];
            inactiveSource.clip = newClip;
            inactiveSource.Play();

            if (isSourceAActive)
            {
                StartCoroutine(FadeInternalVolume(val => internalVolumeA = val, internalVolumeA, 0f, crossfadeDuration));
                StartCoroutine(FadeInternalVolume(val => internalVolumeB = val, internalVolumeB, 1f, crossfadeDuration));
            }
            else
            {
                StartCoroutine(FadeInternalVolume(val => internalVolumeB = val, internalVolumeB, 0f, crossfadeDuration));
                StartCoroutine(FadeInternalVolume(val => internalVolumeA = val, internalVolumeA, 1f, crossfadeDuration));
            }

            isSourceAActive = !isSourceAActive;
        }

        private int GetNextTrackIndex()
        {
            if (currentPlaylist == null || currentPlaylist.Tracks.Length == 0) return -1;

            if (currentPlaylist.Shuffle)
            {
                return Random.Range(0, currentPlaylist.Tracks.Length);
            }
            else
            {
                int nextIndex = currentTrackIndex + 1;
                if (nextIndex >= currentPlaylist.Tracks.Length)
                    nextIndex = 0;
                return nextIndex;
            }
        }

        // ---------- Additional Public Methods For Testing ----------
        public void ForceNextTrack()
        {
            // Force next track immediately
            CrossfadeToNewTrack();
        }

        public void ForcePreviousTrack()
        {
            if (currentPlaylist == null || currentPlaylist.Tracks.Length == 0) return;
            int trackCount = currentPlaylist.Tracks.Length;
            currentTrackIndex = (currentTrackIndex - 2 + trackCount) % trackCount;
            CrossfadeToNewTrack();
        }

        public void PauseCurrentSong(float fadeDuration)
        {
            StartCoroutine(PauseRoutine(fadeDuration));
        }

        public void AdjustMasterVolume(float targetVolume, float duration)
        {
            if (volumeAdjustCoroutine != null) StopCoroutine(volumeAdjustCoroutine);
            volumeAdjustCoroutine = StartCoroutine(FadeMasterVolumeRoutine(masterVolume, targetVolume, duration));
        }

        public void AdjustMasterPitch(float targetPitch, float duration)
        {
            if (pitchAdjustCoroutine != null) StopCoroutine(pitchAdjustCoroutine);
            pitchAdjustCoroutine = StartCoroutine(FadeMasterPitchRoutine(masterPitch, targetPitch, duration));
        }

        public void ResetMasterSettings(float duration)
        {
            AdjustMasterVolume(1f, duration);
            AdjustMasterPitch(1f, duration);
        }


        // ---------- Coroutines ----------
        private IEnumerator FadeInternalVolume(System.Action<float> setter, float start, float end, float duration)
        {
            float elapsed = 0f;
            float initial = start;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float val = Mathf.Lerp(initial, end, t);
                setter(val);
                yield return null;
            }
            setter(end);
        }

        private IEnumerator PauseRoutine(float fadeDuration)
        {
            yield return StartCoroutine(FadeMasterVolumeRoutine(masterVolume, 0f, fadeDuration));
            AudioSource activeSource = isSourceAActive ? sourceA : sourceB;
            activeSource.Pause();
        }

        private IEnumerator FadeMasterVolumeRoutine(float start, float end, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                masterVolume = Mathf.Lerp(start, end, elapsed / duration);
                yield return null;
            }
            masterVolume = end;
        }

        private IEnumerator FadeMasterPitchRoutine(float start, float end, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                masterPitch = Mathf.Lerp(start, end, elapsed / duration);
                yield return null;
            }
            masterPitch = end;
        }
    }
}
