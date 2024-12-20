using JG.Audio;
using UnityEngine;

public class MusicTester : MonoBehaviour
{
    [SerializeField] private PlaylistSO newPlaylistToLoad;

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
}
