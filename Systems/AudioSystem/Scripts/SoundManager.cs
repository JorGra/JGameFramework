using JG.Tools;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace JG.Audio
{

    [DefaultExecutionOrder(-50)]
    public class SoundManager : PersistentSingleton<SoundManager>
    {
        IObjectPool<SoundEmitter> soundEmitterPool;
        readonly List<SoundEmitter> activeSoundEmitters = new List<SoundEmitter>();
        public readonly Queue<SoundEmitter> FrequentSoundEmitters = new Queue<SoundEmitter>();

        [SerializeField] SoundEmitter soundEmitterPrefab;
        [SerializeField] bool collecitonCheck = true;
        [SerializeField] int defaultCapacity = 10;
        [SerializeField] int maxPoolSize = 100;
        [SerializeField] int maxSoundInstances = 30;

        EventSubscription<PlaySoundEvent> soundEventSubscription;

        private void Start()
        {
            soundEventSubscription = EventBus<PlaySoundEvent>.Subscribe(OnPlaySoundEvent, this);
            InitializePool();
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

        public bool CanPlaySound(SoundData data)
        {
            if (!data.frequent) return true;

            if (FrequentSoundEmitters.Count >= maxSoundInstances && FrequentSoundEmitters.TryDequeue(out var soundEmitter))
            {
                try
                {
                    soundEmitter.Stop();
                    return true;
                }
                catch
                {
                    //Debug.Log("SoundEmitter is already released");
                }

                return false;
            }
            return true;

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
