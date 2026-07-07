using UnityEngine;

namespace JG.CursorSystem
{
    /// <summary>
    /// Designer-friendly hover target: shows a fixed cursor preset while this object is hovered.
    /// Works on world objects (needs a Collider2D on this object or a child) and on UGUI
    /// elements (needs a raycast-target Graphic) — the <see cref="CursorHoverProbe"/> finds it
    /// via GetComponentInParent in both passes.
    /// For conditional cursors, implement <see cref="ICursorHoverTarget"/> directly instead.
    /// </summary>
    [AddComponentMenu("JGameFramework/Cursor System/Cursor Hint On Hover")]
    public sealed class CursorHintOnHover : MonoBehaviour, ICursorHoverTarget
    {
        [SerializeField] string presetId = "Default";
        [Tooltip("Optional cursor set; empty = current set.")]
        [SerializeField] string cursorSetId = "";
        [Tooltip("Added to the hover layer priority; lets important targets outrank overlapping ones.")]
        [SerializeField] int priorityOffset = 0;

        public CursorHint GetCursorHint(in CursorQueryContext context)
        {
            if (!isActiveAndEnabled || string.IsNullOrWhiteSpace(presetId))
                return CursorHint.None;

            return new CursorHint(
                presetId,
                string.IsNullOrWhiteSpace(cursorSetId) ? null : cursorSetId,
                priorityOffset);
        }
    }
}
