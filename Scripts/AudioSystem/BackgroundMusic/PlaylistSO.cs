using UnityEngine;
using UnityEngine.Audio;

namespace JG.Audio
{

    [CreateAssetMenu(fileName = "NewPlaylist", menuName = "Audio/Playlist")]
    public class PlaylistSO : ScriptableObject
    {
        public AudioClip[] Tracks;
        public bool Shuffle = false;
        public float CrossfadeDuration = 2f;
        public AudioMixerGroup MixerGroup;
    }

}