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

        private void Start()
        {
            InitializePool();
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
                    Debug.Log("SoundEmitter is already released");
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
}