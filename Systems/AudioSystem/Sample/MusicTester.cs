using JG.Audio;
using UnityEngine;


namespace JG.Samples
{
    public class MusicTester : MonoBehaviour
    {
        [SerializeField] private PlaylistSO newPlaylistToLoad;

        bool paused = false;
        void Start()
        {
            // Example: After some delay, switch to a new playlist
            Invoke(nameof(SwitchToNewPlaylist), 5f);
        }

        private void SwitchToNewPlaylist()
        {
            // Raise event to change the playlist
            EventBus<ChangePlaylistEvent>.Raise(new ChangePlaylistEvent() { NewPlaylist = newPlaylistToLoad });
        }

        public void Update()
        {
            // Example: Play a specific track
            if (Input.GetKeyDown(KeyCode.Space))
            {
                paused = !paused;
                EventBus<PauseMusicEvent>.Raise(new PauseMusicEvent() { Paused = paused, FadeDuration = .5f });
            }

            if (Input.GetKeyDown(KeyCode.Q))
            {
                EventBus<ChangeMusicPitchEvent>.Raise(new ChangeMusicPitchEvent() { TargetPitch = 0.4f, FadeDuration = .5f });
            }
            if (Input.GetKeyDown(KeyCode.E))
            {
                EventBus<ChangeMusicPitchEvent>.Raise(new ChangeMusicPitchEvent() { TargetPitch = 1f, FadeDuration = .5f });
            }
        }
    }
}