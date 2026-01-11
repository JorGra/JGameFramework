using UnityEngine;


namespace JG.Audio
{
    /// <summary>
    /// Component that makes an AudioListener follow a target Camera's position (and optionally rotation).
    /// Automatically disables other AudioListeners in the scene to avoid conflicts.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioListener))]
    public sealed class AudioListenerFollower : MonoBehaviour
    {
        [SerializeField] private bool followRotation = true;
        [SerializeField] private bool disableOtherListeners = true;

        private AudioListener listener;
        private Camera target;

        private void Awake()
        {
            listener = GetComponent<AudioListener>();
        }

        private void OnEnable()
        {
            ResolveTarget();
        }

        private void LateUpdate()
        {
            if (!target)
            {
                ResolveTarget();
            }

            if (!target)
            {
                return;
            }

            var targetTransform = target.transform;
            transform.position = targetTransform.position;
            if (followRotation)
            {
                transform.rotation = targetTransform.rotation;
            }
        }

        public void SetTarget(Camera camera)
        {
            target = camera;
            DisableOtherListenersIfNeeded();
        }

        private void ResolveTarget()
        {
            if (!target)
            {
                target = Camera.main;
            }

            DisableOtherListenersIfNeeded();
        }

        private void DisableOtherListenersIfNeeded()
        {
            if (!disableOtherListeners || listener == null)
            {
                return;
            }

            var listeners = Object.FindObjectsOfType<AudioListener>(true);
            foreach (var other in listeners)
            {
                if (other == null || other == listener)
                {
                    continue;
                }

                other.enabled = false;
            }

            listener.enabled = true;
        }
    }

}