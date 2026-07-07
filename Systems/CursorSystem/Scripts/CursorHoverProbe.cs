using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace JG.CursorSystem
{
    /// <summary>
    /// Watches what the pointer hovers and holds a single hover claim on the
    /// <see cref="CursorClaimStack"/>. Two passes per evaluation: UI first (any raycast-target
    /// UI under the pointer blocks world hover), then 2D world colliders. Hit objects are
    /// queried for <see cref="ICursorHoverTarget"/> via GetComponentInParent.
    /// One active probe per application.
    /// </summary>
    [AddComponentMenu("JGameFramework/Cursor System/Cursor Hover Probe")]
    [DisallowMultipleComponent]
    public sealed class CursorHoverProbe : MonoBehaviour
    {
        const float PointerMoveThreshold = 0.5f; // pixels

        [Header("Pointer Source")]
        [Tooltip("Optional ICursorPointerSource; when unset, Mouse.current plus the camera below is used.")]
        [SerializeField] MonoBehaviour pointerSourceBehaviour;
        [Tooltip("Camera for the fallback pointer source and world raycasts; null = Camera.main.")]
        [SerializeField] Camera fallbackCamera;

        [Header("World Pass")]
        [SerializeField] LayerMask worldLayerMask = ~0;

        [Header("Timing")]
        [Tooltip("Re-evaluate at least this often (unscaled seconds) even when the pointer is still — catches objects moving under a stationary cursor and keeps working at timeScale 0.")]
        [SerializeField, Min(0.02f)] float revalidateInterval = 0.15f;

        static CursorHoverProbe activeProbe;

        readonly RaycastHit2D[] worldHitBuffer = new RaycastHit2D[8];
        readonly List<RaycastResult> uiRaycastResults = new List<RaycastResult>(16);

        ICursorPointerSource pointerSource;
        CursorClaimHandle hoverClaim;
        ICursorHoverTarget currentTarget;
        int currentClaimPriority;
        string currentPresetId;
        string currentSetId;
        Vector2 lastEvaluatedPosition = new Vector2(float.NaN, float.NaN);
        float lastEvaluationTime = float.NegativeInfinity;

        /// <summary>Assign a pointer source at runtime (game-side, e.g. backed by the mouse player's aim state).</summary>
        public void SetPointerSource(ICursorPointerSource source) => pointerSource = source;

        void OnValidate()
        {
            if (pointerSourceBehaviour != null && !(pointerSourceBehaviour is ICursorPointerSource))
            {
                Debug.LogWarning($"[CursorHoverProbe] '{pointerSourceBehaviour.name}' does not implement ICursorPointerSource.", this);
                pointerSourceBehaviour = null;
            }
        }

        void OnEnable()
        {
            if (activeProbe != null && activeProbe != this)
            {
                Debug.LogWarning("[CursorHoverProbe] Another probe is already active; disabling this one.", this);
                enabled = false;
                return;
            }

            activeProbe = this;

            if (pointerSource == null && pointerSourceBehaviour is ICursorPointerSource serialized)
                pointerSource = serialized;
        }

        void OnDisable()
        {
            if (activeProbe == this)
                activeProbe = null;

            ReleaseClaim();
            lastEvaluatedPosition = new Vector2(float.NaN, float.NaN);
        }

        void Update()
        {
            if (!TryGetPointer(out var screenPos, out var camera))
            {
                ReleaseClaim();
                return;
            }

            var moved = float.IsNaN(lastEvaluatedPosition.x) ||
                        (screenPos - lastEvaluatedPosition).sqrMagnitude >= PointerMoveThreshold * PointerMoveThreshold;
            var stale = Time.unscaledTime - lastEvaluationTime >= revalidateInterval;
            var targetDied = currentTarget is Object unityTarget && unityTarget == null;

            if (!moved && !stale && !targetDied)
                return;

            lastEvaluatedPosition = screenPos;
            lastEvaluationTime = Time.unscaledTime;

            Evaluate(screenPos, camera);
        }

        bool TryGetPointer(out Vector2 screenPos, out Camera camera)
        {
            if (pointerSource != null && pointerSource.TryGetPointer(out screenPos, out camera) && camera != null)
                return true;

            var mouse = Mouse.current;
            if (mouse == null)
            {
                screenPos = default;
                camera = null;
                return false;
            }

            screenPos = mouse.position.ReadValue();
            camera = fallbackCamera != null ? fallbackCamera : Camera.main;
            return camera != null;
        }

        void Evaluate(Vector2 screenPos, Camera camera)
        {
            var worldPos = camera.ScreenToWorldPoint(
                new Vector3(screenPos.x, screenPos.y, -camera.transform.position.z));
            var context = new CursorQueryContext(
                screenPos, worldPos, camera, CursorQueryContext.GameContextProvider?.Invoke());

            // UI pass: any raycast-target UI under the pointer blocks the world pass entirely.
            if (TryRaycastUI(screenPos, out var uiHit))
            {
                var uiTarget = uiHit.GetComponentInParent<ICursorHoverTarget>();
                if (uiTarget != null)
                {
                    var hint = uiTarget.GetCursorHint(in context);
                    if (hint.HasOpinion)
                    {
                        ApplyHover(uiTarget, hint, CursorLayer.HoverUI);
                        return;
                    }
                }

                // UI hovered but no cursor opinion: no hover claim, lower layers show through.
                ReleaseClaim();
                return;
            }

            // World pass (2D).
            var ray = camera.ScreenPointToRay(screenPos);
            int hitCount = Physics2D.GetRayIntersectionNonAlloc(ray, worldHitBuffer, Mathf.Infinity, worldLayerMask);
            for (int i = 0; i < hitCount; i++)
            {
                var collider = worldHitBuffer[i].collider;
                if (collider == null)
                    continue;

                var target = collider.GetComponentInParent<ICursorHoverTarget>();
                if (target == null)
                    continue;

                var worldContext = new CursorQueryContext(
                    screenPos, worldHitBuffer[i].point, camera, context.GameContext);
                var hint = target.GetCursorHint(in worldContext);
                if (!hint.HasOpinion)
                    continue;

                ApplyHover(target, hint, CursorLayer.HoverWorld);
                return;
            }

            ReleaseClaim();
        }

        bool TryRaycastUI(Vector2 screenPos, out GameObject topmost)
        {
            topmost = null;

            var eventSystem = EventSystem.current;
            if (eventSystem == null)
                return false;

            var pointerData = new PointerEventData(eventSystem) { position = screenPos };
            uiRaycastResults.Clear();
            eventSystem.RaycastAll(pointerData, uiRaycastResults);

            if (uiRaycastResults.Count == 0)
                return false;

            topmost = uiRaycastResults[0].gameObject;
            return topmost != null;
        }

        void ApplyHover(ICursorHoverTarget target, in CursorHint hint, int basePriority)
        {
            var priority = basePriority + hint.PriorityOffset;

            if (hoverClaim != null && hoverClaim.IsActive && priority == currentClaimPriority)
            {
                currentTarget = target;
                if (hint.PresetId != currentPresetId || hint.SetId != currentSetId)
                {
                    currentPresetId = hint.PresetId;
                    currentSetId = hint.SetId;
                    hoverClaim.Update(hint.PresetId, hint.SetId);
                }
                return;
            }

            hoverClaim?.Dispose();
            hoverClaim = CursorClaimStack.Push(hint.PresetId, hint.SetId, priority, owner: this);
            currentTarget = target;
            currentClaimPriority = priority;
            currentPresetId = hint.PresetId;
            currentSetId = hint.SetId;
        }

        void ReleaseClaim()
        {
            if (hoverClaim != null)
            {
                hoverClaim.Dispose();
                hoverClaim = null;
            }

            currentTarget = null;
            currentPresetId = null;
            currentSetId = null;
        }
    }
}
