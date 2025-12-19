using System.Collections;
using UnityEngine;

namespace JG.Audio
{
    public interface IMusicCommand
    {
        void Execute();
        void Cancel();
        bool IsRunning { get; }
    }


    public abstract class MusicCommand : IMusicCommand
    {
        protected MusicController controller;
        protected MonoBehaviour coroutineOwner;
        protected Coroutine currentCoroutine;

        public bool IsRunning { get; protected set; }

        public MusicCommand(MusicController controller, MonoBehaviour owner)
        {
            this.controller = controller;
            this.coroutineOwner = owner;
        }

        public void Execute()
        {
            IsRunning = true;
            OnExecute();
        }

        protected virtual void OnExecute() { }

        public void Cancel()
        {
            if (currentCoroutine != null)
            {
                coroutineOwner.StopCoroutine(currentCoroutine);
                currentCoroutine = null;
            }
            OnCancel();
            IsRunning = false;
        }

        protected virtual void OnCancel() { }
    }

    public class PauseCommand : MusicCommand
    {
        readonly float startVolume;
        float fadeDuration;
        public PauseCommand(MusicController controller, MonoBehaviour owner, float startVolume, float fadeDuration) : base(controller, owner)
        {
            this.startVolume = Mathf.Clamp01(startVolume);
            this.fadeDuration = fadeDuration;
        }

        protected override void OnExecute()
        {
            // Fade out and then pause
            currentCoroutine = coroutineOwner.StartCoroutine(PauseRoutine());
        }

        private IEnumerator PauseRoutine()
        {
            float start = startVolume;
            float end = 0f;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeDuration;
                controller.SetMasterVolumeInstant(Mathf.Lerp(start, end, t));
                yield return null;
            }
            controller.SetMasterVolumeInstant(0f);
            controller.PauseActiveSource();
            IsRunning = false;
        }
    }

    public class ResumeCommand : MusicCommand
    {
        readonly float targetVolume;
        float fadeDuration;
        public ResumeCommand(MusicController controller, MonoBehaviour owner, float targetVolume, float fadeDuration) : base(controller, owner)
        {
            this.targetVolume = Mathf.Clamp01(targetVolume);
            this.fadeDuration = fadeDuration;
        }

        protected override void OnExecute()
        {
            currentCoroutine = coroutineOwner.StartCoroutine(ResumeRoutine());
        }

        private IEnumerator ResumeRoutine()
        {
            controller.UnPauseActiveSource();
            float start = 0f;
            float end = targetVolume;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeDuration;
                controller.SetMasterVolumeInstant(Mathf.Lerp(start, end, t));
                yield return null;
            }
            controller.SetMasterVolumeInstant(end);
            IsRunning = false;
        }
    }

    public class ChangeVolumeCommand : MusicCommand
    {
        float targetVolume;
        float duration;
        public ChangeVolumeCommand(MusicController controller, MonoBehaviour owner, float targetVolume, float duration) : base(controller, owner)
        {
            this.targetVolume = targetVolume;
            this.duration = duration;
        }

        protected override void OnExecute()
        {
            controller.FadeMasterVolume(targetVolume, duration);
            IsRunning = false; // no ongoing coroutine here since controller handles it internally
        }
    }

    public class ChangePitchCommand : MusicCommand
    {
        float targetPitch;
        float duration;

        public ChangePitchCommand(MusicController controller, MonoBehaviour owner, float targetPitch, float duration) : base(controller, owner)
        {
            this.targetPitch = targetPitch;
            this.duration = duration;
        }

        protected override void OnExecute()
        {
            controller.FadeMasterPitch(targetPitch, duration);
            IsRunning = false;
        }
    }
}
