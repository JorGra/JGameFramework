using UnityEngine;
using UnityEngine.InputSystem;

namespace JG.Vfx.Preview
{
    /// <summary>
    /// Minimal orbit camera for the Vfx preview scene (Input System based -
    /// the project has legacy Input disabled). Drag with the left or right
    /// mouse button to orbit around the pivot, scroll to zoom.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class VfxPreviewCamera : MonoBehaviour
    {
        [Tooltip("Point the camera orbits around (the preview system spawns at the origin).")]
        public Vector3 pivot = Vector3.zero;

        [Tooltip("Degrees per pixel of mouse drag.")]
        public float orbitSpeed = 0.25f;

        [Tooltip("Zoom factor per scroll step (distance is multiplied/divided by 1 + this).")]
        public float zoomStep = 0.12f;

        public float minDistance = 1.5f;
        public float maxDistance = 40f;
        public float minPitch = -80f;
        public float maxPitch = 80f;

        private float _yaw;
        private float _pitch;
        private float _distance;

        private void Start()
        {
            // Derive the initial orbit from wherever the scene placed the camera.
            var offset = transform.position - pivot;
            _distance = Mathf.Clamp(offset.magnitude, minDistance, maxDistance);
            if (offset.sqrMagnitude < 0.0001f)
            {
                offset = new Vector3(0f, 1f, -10f);
                _distance = offset.magnitude;
            }
            _yaw = Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg + 180f;
            _pitch = Mathf.Asin(Mathf.Clamp(offset.y / _distance, -1f, 1f)) * Mathf.Rad2Deg;
            Apply();
        }

        private void LateUpdate()
        {
            var mouse = Mouse.current;
            if (mouse == null)
                return;

            bool changed = false;

            if (mouse.leftButton.isPressed || mouse.rightButton.isPressed)
            {
                var delta = mouse.delta.ReadValue();
                if (delta.sqrMagnitude > 0f)
                {
                    _yaw += delta.x * orbitSpeed;
                    _pitch = Mathf.Clamp(_pitch - delta.y * orbitSpeed, minPitch, maxPitch);
                    changed = true;
                }
            }

            // Scroll magnitudes differ wildly per platform/browser - only the
            // sign is reliable, so zoom in fixed steps.
            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                float factor = scroll > 0f ? 1f / (1f + zoomStep) : 1f + zoomStep;
                _distance = Mathf.Clamp(_distance * factor, minDistance, maxDistance);
                changed = true;
            }

            if (changed)
                Apply();
        }

        private void Apply()
        {
            var rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            transform.position = pivot + rotation * new Vector3(0f, 0f, -_distance);
            transform.rotation = rotation;
        }
    }
}
