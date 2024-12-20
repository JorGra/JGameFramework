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

        private MusicController musicController;
        private IMusicCommand currentCommand;

        private PlaylistSO currentPlaylist;
        private int currentTrackIndex;

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

            musicController = gameObject.AddComponent<MusicController>();
            musicController.Init(audioMixerGroup);

            SetPlaylist(defaultPlaylist);
            musicController.PlayClip(currentPlaylist.Tracks[currentTrackIndex], true, startFadeInDuration);

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
        }

        private void Update()
        {
            // Check if current track finished playing and move to next track if so
            if (!musicController.IsPlaying && currentPlaylist != null && currentPlaylist.Tracks.Length > 0)
            {
                PlayNextTrack();
            }
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
    }
}
