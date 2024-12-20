using UnityEngine;

namespace JG.Audio
{

    public class PlaylistController : MonoBehaviour
    {
        [SerializeField] private PlaylistSO scenePlaylist;

        // This script will raise an event when the scene is loaded or activated.
        // For simplicity, let's assume we raise it in OnEnable or Start.
        // You can also tie this into a "SceneLoadedEvent" from your event system.
        private void Start()
        {
            if (scenePlaylist != null)
            {
                EventBus<ChangePlaylistEvent>.Raise(new ChangePlaylistEvent()
                {
                    NewPlaylist = scenePlaylist
                });
            }
            else
            {
                // If no playlist is assigned, raise an event with no playlist to use the default
                EventBus<ChangePlaylistEvent>.Raise(new ChangePlaylistEvent()
                {
                    NewPlaylist = null
                });
            }
        }
    }

}