using JG.Tools;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace JG.Audio
{

    [DefaultExecutionOrder(-50)]
    public class SoundManager : PersistentSingleton<SoundManager>
    {
        IObjectPool<SoundEmitter> soundEmitterPool;
        readonly List<SoundEmitter> activeSoundEmitters = new List<SoundEmitter>();
        readonly Dictionary<AudioClip, ClipVoiceState> voiceStates = new Dictionary<AudioClip, ClipVoiceState>();
        float masterVolume = 1f;
        float musicVolume = 1f;
        float effectsVolume = 1f;
        float uiVolume = 1f;

        [SerializeField] SoundEmitter soundEmitterPrefab;
        [Header("Mixer Groups")]
        [SerializeField] AudioMixerGroup masterMixerGroup;
        [SerializeField] AudioMixerGroup musicMixerGroup;
        [SerializeField] AudioMixerGroup effectsMixerGroup;
        [SerializeField] AudioMixerGroup uiMixerGroup;
        [SerializeField] bool collecitonCheck = true;
        [SerializeField] int defaultCapacity = 10;
        [SerializeField] int maxPoolSize = 100;
        [Header("Voice Limiting")]
        [Tooltip("Max simultaneous instances of the same clip. Oldest is stopped when exceeded.")]
        [SerializeField] int maxVoicesPerClip = 8;
        [Tooltip("Tighter cap for sounds marked 'frequent'.")]
        [SerializeField] int frequentMaxVoices = 4;
        [Tooltip("Minimum seconds between two starts of the same 'frequent' clip; requests inside the window are dropped.")]
        [SerializeField] float frequentMinInterval = 0.05f;
        [Tooltip("Volume multiplier per already-playing instance of the same clip (1 = off). 0.8 ≈ -2 dB per extra instance.")]
        [Range(0f, 1f)][SerializeField] float stackedVolumeFalloff = 0.8f;

        EventSubscription<PlaySoundEvent> soundEventSubscription;

        sealed class ClipVoiceState
        {
            public readonly LinkedList<SoundEmitter> active = new LinkedList<SoundEmitter>();
            public float lastPlayTime = float.NegativeInfinity;
        }

        private void Start()
        {
            soundEventSubscription = EventBus<PlaySoundEvent>.Subscribe(OnPlaySoundEvent, this);
            InitializePool();
            ApplyVolumeToActiveEmitters();
        }

        private void OnDestroy()
        {
            soundEventSubscription?.Dispose();
            soundEventSubscription = null;
        }

        void OnPlaySoundEvent(PlaySoundEvent e)
        {
            var builder = CreateSound()
                .WithSoundData(e.SoundData)
                .WithPosition(e.Position);

            if (e.HasRandomPitchOverride)
            {
                builder.WithRadnomPitch(e.RandomPitchRange.x, e.RandomPitchRange.y);
            }

            builder.Play();
        }

        public SoundBuilder CreateSound() => new SoundBuilder(this);

        public bool TryAcquireVoice(SoundData data, out float volumeScale)
        {
            volumeScale = 1f;
            var clip = data?.clip;
            if (clip == null) return false;

            if (!voiceStates.TryGetValue(clip, out var state))
            {
                voiceStates.Add(clip, state = new ClipVoiceState());
            }

            float now = Time.unscaledTime;
            if (data.frequent && frequentMinInterval > 0f && now - state.lastPlayTime < frequentMinInterval)
            {
                return false;
            }

            int max = Mathf.Max(1, data.frequent ? frequentMaxVoices : maxVoicesPerClip);
            while (state.active.Count >= max)
            {
                var oldest = state.active.First;
                state.active.Remove(oldest);
                oldest.Value.Stop();
            }

            if (stackedVolumeFalloff < 1f && state.active.Count > 0)
            {
                volumeScale = Mathf.Pow(stackedVolumeFalloff, state.active.Count);
            }

            state.lastPlayTime = now;
            return true;
        }

        public void RegisterVoice(SoundEmitter emitter)
        {
            if (emitter == null || emitter.Node == null || emitter.Node.List != null) return;
            if (emitter.Data == null || emitter.Data.clip == null) return;
            if (!voiceStates.TryGetValue(emitter.Data.clip, out var state)) return;

            state.active.AddLast(emitter.Node);
        }

        public void OnEmitterStopped(SoundEmitter emitter)
        {
            var node = emitter?.Node;
            node?.List?.Remove(node);
        }

        public SoundEmitter Get()
        {
            return soundEmitterPool.Get();
        }

        public void ReturnToPool(SoundEmitter soundEmitter)
        {
            soundEmitterPool.Release(soundEmitter);
        }

        private void InitializePool()
        {
            soundEmitterPool = new ObjectPool<SoundEmitter>(
                CreateSoundEmitter,
                OnTakeFromPool,
                OnReturnFromPool,
                OnDestroyPoolObject,
                collecitonCheck,
                defaultCapacity,
                maxPoolSize);
        }

        private void OnDestroyPoolObject(SoundEmitter emitter)
        {
            if (emitter != null && emitter.gameObject != null)
                Destroy(emitter.gameObject);
        }

        private void OnReturnFromPool(SoundEmitter emitter)
        {
            OnEmitterStopped(emitter);
            emitter.gameObject.SetActive(false);
            activeSoundEmitters.Remove(emitter);
        }

        private void OnTakeFromPool(SoundEmitter emitter)
        {
            emitter.gameObject.SetActive(true);
            activeSoundEmitters.Add(emitter);
        }

        private SoundEmitter CreateSoundEmitter()
        {
            var soundEmitter = Instantiate(soundEmitterPrefab);
            soundEmitter.gameObject.SetActive(false);
            return soundEmitter;
        }

        public AudioMixerGroup ResolveMixerGroup(SoundMixerGroup mixerGroup)
        {
            return mixerGroup switch
            {
                SoundMixerGroup.Music when musicMixerGroup != null => musicMixerGroup,
                SoundMixerGroup.Effects when effectsMixerGroup != null => effectsMixerGroup,
                SoundMixerGroup.UI when uiMixerGroup != null => uiMixerGroup,
                _ => masterMixerGroup
            };
        }
        public float GetVolumeMultiplier(SoundMixerGroup mixerGroup)
        {
            float category = mixerGroup switch
            {
                SoundMixerGroup.Music => musicVolume,
                SoundMixerGroup.UI => uiVolume,
                _ => effectsVolume
            };

            return Mathf.Clamp01(masterVolume) * Mathf.Clamp01(category);
        }

        public void SetChannelVolumes(float master, float music, float effects, float ui)
        {
            masterVolume = Mathf.Clamp01(master);
            musicVolume = Mathf.Clamp01(music);
            effectsVolume = Mathf.Clamp01(effects);
            uiVolume = Mathf.Clamp01(ui);

            ApplyVolumeToActiveEmitters();
        }

        void ApplyVolumeToActiveEmitters()
        {
            float master = masterVolume;
            float music = musicVolume;
            float effects = effectsVolume;
            float ui = uiVolume;

            for (int i = 0; i < activeSoundEmitters.Count; i++)
            {
                var emitter = activeSoundEmitters[i];
                if (emitter == null) continue;

                float multiplier = master * (emitter.MixerGroupType switch
                {
                    SoundMixerGroup.Music => music,
                    SoundMixerGroup.UI => ui,
                    _ => effects
                });

                emitter.ApplyVolumeMultiplier(multiplier);
            }
        }

    }

    public struct PlaySoundEvent : IEvent
    {
        public SoundData SoundData { get; private set; }
        public Vector3 Position { get; private set; }
        public bool HasRandomPitchOverride { get; private set; }
        public Vector2 RandomPitchRange { get; private set; }

        public PlaySoundEvent(SoundData soundData, Vector3 position)
        {
            SoundData = soundData;
            Position = position;
            HasRandomPitchOverride = false;
            RandomPitchRange = Vector2.zero;
        }

        public PlaySoundEvent(SoundData soundData, Vector3 position, Vector2 randomPitchRange)
        {
            SoundData = soundData;
            Position = position;
            HasRandomPitchOverride = true;
            RandomPitchRange = randomPitchRange;
        }
    }
}
