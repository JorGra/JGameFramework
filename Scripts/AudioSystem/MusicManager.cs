using JG.Tools;
using JG.Util.Extensions;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;


namespace JG.Audio
{
    [RequireComponent(typeof(MusicManager))]
    public class MusicManager : PersistentSingleton<MonoBehaviour>
    {
        const float crossFadeTime = 1.0f;
        float fading;
        [SerializeField] AudioSource current;
        [SerializeField] AudioSource previous;
        readonly Queue<AudioClip> playlist = new();

        [SerializeField] List<AudioClip> initialPlaylist;
        [SerializeField] AudioMixerGroup musicMixerGroup;

        [SerializeField] bool loopPlaylist = true;

        void Start()
        {
            FillPlayList();
        }

        public void AddToPlaylist(AudioClip clip)
        {
            playlist.Enqueue(clip);
            if (current == null && previous == null)
            {
                PlayNextTrack();
            }
        }

        public void Clear() => playlist.Clear();

        public void PlayNextTrack()
        {
            bool gotTrack = playlist.TryDequeue(out AudioClip nextTrack);
            if (gotTrack)
            {
                Play(nextTrack);
            }

            // If we're looping and ended up with no track this round
            if (loopPlaylist && playlist.Count == 0)
            {
                FillPlayList();
                // After refilling the playlist, try to play another track right away
                if (!gotTrack && playlist.TryDequeue(out nextTrack))
                {
                    Play(nextTrack);
                }
            }
        }

        private void FillPlayList()
        {
            foreach (var clip in initialPlaylist)
            {
                AddToPlaylist(clip);
            }
        }

        public void Play(AudioClip clip)
        {
            if (current && current.clip == clip) return;

            if (previous)
            {
                //Destroy(previous);
                //previous = null;
            }

            previous = current;

            current = gameObject.GetOrAdd<AudioSource>();
            current.clip = clip;
            current.outputAudioMixerGroup = musicMixerGroup; // Set mixer group
            current.loop = false; // For playlist functionality, we want tracks to play once
            current.volume = 0;
            current.bypassListenerEffects = true;
            current.Play();

            fading = 0.001f;
        }

        void Update()
        {
            HandleCrossFade();

            if (current && !current.isPlaying && playlist.Count > 0)
            {
                PlayNextTrack();
            }
        }

        void HandleCrossFade()
        {
            if (fading <= 0f) return;

            fading += Time.deltaTime;

            float fraction = Mathf.Clamp01(fading / crossFadeTime);

            // Logarithmic fade
            float logFraction = fraction.ToLogarithmicFraction();

            if (previous) previous.volume = 1.0f - logFraction;
            if (current) current.volume = logFraction;

            if (fraction >= 1)
            {
                fading = 0.0f;
                if (previous)
                {
                    //Destroy(previous);
                    //previous = null;
                }
            }
        }
    }
}