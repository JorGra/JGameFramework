using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JG.Audio
{
    public class SoundBuilder
    {
        readonly SoundManager soundManager;
        SoundData soundData;
        Vector3 position = Vector3.zero;
        bool randomPitch;
        Vector2 randomPitchRange;


        public SoundBuilder(SoundManager soundManager)
        {
            this.soundManager = soundManager;
        }

        public SoundBuilder WithSoundData(SoundData soundData)
        {
            this.soundData = soundData;
            return this;
        }

        public SoundBuilder WithPosition(Vector3 position)
        {
            this.position = position;
            return this;
        }

        public SoundBuilder WithRadnomPitch(float min = -0.05f, float max = 0.05f)
        {
            this.randomPitch = true;
            this.randomPitchRange = new Vector2(min, max);
            return this;
        }

        public void Play()
        {
            if (!soundManager.CanPlaySound(soundData)) return;

            SoundEmitter soundEmitter = soundManager.Get();
            soundEmitter.Initialize(soundData);
            soundEmitter.transform.position = position;
            soundEmitter.transform.parent = SoundManager.Instance.transform;

            if (randomPitch)
            {
                soundEmitter.WithRandomPitch(randomPitchRange);
            }

            if (soundData.frequent)
            {
                soundManager.FrequentSoundEmitters.Enqueue(soundEmitter);
            }
            soundEmitter.Play();
        }
    }
}