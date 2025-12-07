using JG.Tools;
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
        public bool Paused;
        public float FadeDuration;
    }
    public struct ChangeMusicVolumeEvent : IEvent
    {
        public float TargetVolume; // e.g. >1.0 to intensify, <1.0 to deintensify
        public float FadeDuration;
    }
    public struct ChangeMusicPitchEvent : IEvent
    {
        public float TargetPitch; // e.g. <1.0 to muffle
        public float FadeDuration;
    }
    public struct ResetMusicSettingsEvent : IEvent
    {
        public float FadeDuration;
    }

    public class BackgroundMusicManager : PersistentSingleton<BackgroundMusicManager>
    {
        [SerializeField] private PlaylistSO defaultPlaylist;
        [SerializeField] private AudioMixerGroup audioMixerGroup;
        [SerializeField] private float startFadeInDuration = 2f;
        [SerializeField] private float focusFadeDuration = 0.5f;

        private MusicController musicController;
        private IMusicCommand currentCommand;

        private PlaylistSO currentPlaylist;
        private int currentTrackIndex;
        private bool awaitingFocusResume;
        private bool focusPauseActive;

        EventSubscription<ChangePlaylistEvent> changePlaylistSubscription;
        EventSubscription<NextTrackEvent> nextTrackSubscription;
        EventSubscription<PreviousTrackEvent> previousTrackSubscription;
        EventSubscription<PauseMusicEvent> pauseMusicSubscription;
        EventSubscription<ChangeMusicVolumeEvent> changeVolumeSubscription;
        EventSubscription<ChangeMusicPitchEvent> changePitchSubscription;
        EventSubscription<ResetMusicSettingsEvent> resetSettingsSubscription;

        protected override void Awake()
        {
            base.Awake();
            DontDestroyOnLoad(gameObject);

            musicController = gameObject.AddComponent<MusicController>();
            musicController.Init(audioMixerGroup);

            SetPlaylist(defaultPlaylist);
            musicController.PlayClip(currentPlaylist.Tracks[currentTrackIndex], true, startFadeInDuration);

            // Register events
            changePlaylistSubscription = EventBus<ChangePlaylistEvent>.Subscribe(OnChangePlaylist, this);
            nextTrackSubscription = EventBus<NextTrackEvent>.Subscribe(OnNextTrack, this);
            previousTrackSubscription = EventBus<PreviousTrackEvent>.Subscribe(OnPreviousTrack, this);
            pauseMusicSubscription = EventBus<PauseMusicEvent>.Subscribe(OnPauseMusic, this);
            changeVolumeSubscription = EventBus<ChangeMusicVolumeEvent>.Subscribe(OnChangeMusicVolume, this);
            changePitchSubscription = EventBus<ChangeMusicPitchEvent>.Subscribe(OnChangeMusicPitch, this);
            resetSettingsSubscription = EventBus<ResetMusicSettingsEvent>.Subscribe(OnResetMusicSettings, this);
        }

        private void Update()
        {
            if (awaitingFocusResume)
            {
                if (focusPauseActive)
                {
                    // Wait until the focus pause/resume cycle completes before resuming normal updates
                    return;
                }

                if (musicController.IsPlaying)
                {
                    awaitingFocusResume = false;
                }
                else
                {
                    // Skip crossfades until audio resumes after focus loss
                    return;
                }
            }

            // Check if current track finished playing and move to next track if so
            if (!musicController.IsPlaying && currentPlaylist != null && currentPlaylist.Tracks.Length > 0)
            {
                PlayNextTrack();
            }
        }

        private void OnDestroy()
        {
            changePlaylistSubscription?.Dispose();
            nextTrackSubscription?.Dispose();
            previousTrackSubscription?.Dispose();
            pauseMusicSubscription?.Dispose();
            changeVolumeSubscription?.Dispose();
            changePitchSubscription?.Dispose();
            resetSettingsSubscription?.Dispose();

            changePlaylistSubscription = null;
            nextTrackSubscription = null;
            previousTrackSubscription = null;
            pauseMusicSubscription = null;
            changeVolumeSubscription = null;
            changePitchSubscription = null;
            resetSettingsSubscription = null;
        }

        private void SetPlaylist(PlaylistSO playlist)
        {
            currentPlaylist = playlist != null ? playlist : defaultPlaylist;
            currentTrackIndex = 0;
        }

        private void PlayNextTrack()
        {
            currentTrackIndex = (currentTrackIndex + 1) % currentPlaylist.Tracks.Length;
            musicController.CrossfadeToClip(currentPlaylist.Tracks[currentTrackIndex], 2f);
        }

        private void PlayPreviousTrack()
        {
            currentTrackIndex = (currentTrackIndex - 1 + currentPlaylist.Tracks.Length) % currentPlaylist.Tracks.Length;
            musicController.CrossfadeToClip(currentPlaylist.Tracks[currentTrackIndex], 2f);
        }

        private void OnChangePlaylist(ChangePlaylistEvent e)
        {
            CancelCurrentCommand();
            SetPlaylist(e.NewPlaylist);
            musicController.CrossfadeToClip(currentPlaylist.Tracks[currentTrackIndex], 2f);
        }

        private void OnNextTrack(NextTrackEvent e)
        {
            CancelCurrentCommand();
            PlayNextTrack();
        }

        private void OnPreviousTrack(PreviousTrackEvent e)
        {
            CancelCurrentCommand();
            PlayPreviousTrack();
        }

        private void OnPauseMusic(PauseMusicEvent e)
        {
            CancelCurrentCommand();
            if (e.Paused)
            {
                currentCommand = new PauseCommand(musicController, this, e.FadeDuration);
            }
            else
            {
                currentCommand = new ResumeCommand(musicController, this, e.FadeDuration);
            }
            currentCommand.Execute();
        }

        private void OnChangeMusicVolume(ChangeMusicVolumeEvent e)
        {
            CancelCurrentCommand();
            currentCommand = new ChangeVolumeCommand(musicController, this, e.TargetVolume, e.FadeDuration);
            currentCommand.Execute();
        }

        private void OnChangeMusicPitch(ChangeMusicPitchEvent e)
        {
            CancelCurrentCommand();
            currentCommand = new ChangePitchCommand(musicController, this, e.TargetPitch, e.FadeDuration);
            currentCommand.Execute();
        }

        private void OnResetMusicSettings(ResetMusicSettingsEvent e)
        {
            CancelCurrentCommand();
            // Reset to normal volume and pitch:
            musicController.FadeMasterVolume(1f, e.FadeDuration);
            musicController.FadeMasterPitch(1f, e.FadeDuration);
        }

        private void CancelCurrentCommand()
        {
            if (currentCommand != null && currentCommand.IsRunning)
            {
                currentCommand.Cancel();
            }
            currentCommand = null;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                ResumeAfterFocusReturn();
            }
            else
            {
                PauseForFocusLoss();
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                PauseForFocusLoss();
            }
            else
            {
                ResumeAfterFocusReturn();
            }
        }

        private void PauseForFocusLoss()
        {
            if (focusPauseActive || musicController == null)
            {
                awaitingFocusResume = true;
                return;
            }

            awaitingFocusResume = true;
            focusPauseActive = true;
            CancelCurrentCommand();
            currentCommand = new PauseCommand(musicController, this, focusFadeDuration);
            currentCommand.Execute();
        }

        private void ResumeAfterFocusReturn()
        {
            if (!focusPauseActive || musicController == null)
            {
                return;
            }

            CancelCurrentCommand();
            focusPauseActive = false;
            currentCommand = new ResumeCommand(musicController, this, focusFadeDuration);
            currentCommand.Execute();
        }
    }
}
