using UnityEngine;
using UnityEngine.EventSystems;

namespace UI.Animations
{
    /// <summary>
    /// Generic animation helper that enlarges a UI element while it is
    /// hovered (pointer) or selected (keyboard / game-pad).
    ///
    /// ✔ Works when attached either to the target UI element itself **or** to
    ///   a parent that also receives the pointer events.
    /// ✔ No other setup needed – just add the script and tweak the slider.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class UIHoverAnimateScale : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler,
        ISelectHandler, IDeselectHandler
    {
        [Header("Target")]
        [Tooltip("UI element that should scale. When left empty, the " +
                 "RectTransform on this GameObject is used.")]
        [SerializeField] private RectTransform targetRect = null;

        [Header("Behaviour")]
        [Tooltip("How much bigger the element becomes when hovered/selected. " +
                 "1 = no change, 1.05 = +5 %.")]
        [SerializeField]
        [Range(1f, 2f)]
        private float scaleMultiplier = 1.05f;

        [Tooltip("Seconds the scale animation takes (both directions).")]
        [SerializeField] private float duration = 0.1f;

        [Tooltip("Play animation with un-scaled time (ignores Time.timeScale).")]
        [SerializeField] private bool unscaledTime = true;

        // ──────────────────────────────────────────────────────────────────
        Vector3 originalScale;
        Coroutine animRoutine;

        RectTransform Target =>
            targetRect ? targetRect : (targetRect = GetComponent<RectTransform>());

        void Awake()
        {
            originalScale = Target.localScale;
        }

        // Pointer / keyboard hooks -----------------------------------------
        public void OnPointerEnter(PointerEventData _) => AnimateTo(originalScale * scaleMultiplier);
        public void OnPointerExit(PointerEventData _) => AnimateTo(originalScale);
        public void OnSelect(BaseEventData _) => AnimateTo(originalScale * scaleMultiplier);
        public void OnDeselect(BaseEventData _) => AnimateTo(originalScale);

        // Animation driver --------------------------------------------------
        void AnimateTo(Vector3 targetScale)
        {
            if (!gameObject.activeInHierarchy) return;
            if (animRoutine != null) StopCoroutine(animRoutine);
            animRoutine = StartCoroutine(LerpScale(targetScale));
        }

        System.Collections.IEnumerator LerpScale(Vector3 to)
        {
            Vector3 from = Target.localScale;
            float t = 0f;
            float d = Mathf.Max(0.0001f, duration);

            while (t < d)
            {
                t += unscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                Target.localScale = Vector3.Lerp(from, to, t / d);
                yield return null;
            }
            Target.localScale = to;
            animRoutine = null;
        }

        // Clean-up (reset scale when disabled)
        void OnDisable()
        {
            if (animRoutine != null) StopCoroutine(animRoutine);
            if (Target) Target.localScale = originalScale;
        }
    }
}
