using UnityEngine;

namespace JG.CursorSystem
{
    /// <summary>
    /// Supplies the pointer position and camera used by the <see cref="CursorHoverProbe"/>.
    /// Implement this game-side (e.g. backed by a per-player aim state) and assign it to the
    /// probe to keep the framework decoupled from project-specific input handling.
    /// </summary>
    public interface ICursorPointerSource
    {
        /// <summary>Returns false when there is no usable pointer this frame (e.g. controller-only input).</summary>
        bool TryGetPointer(out Vector2 screenPosition, out Camera camera);
    }
}
