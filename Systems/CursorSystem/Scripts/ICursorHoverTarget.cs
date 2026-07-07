using System;
using UnityEngine;

namespace JG.CursorSystem
{
    /// <summary>
    /// Implemented by anything (world collider hierarchy or UGUI element) that wants to
    /// influence the cursor while hovered. Queried by the <see cref="CursorHoverProbe"/>.
    /// </summary>
    public interface ICursorHoverTarget
    {
        /// <summary>
        /// Return the cursor to show while this target is hovered.
        /// Return a hint with a null <see cref="CursorHint.PresetId"/> to express "no opinion"
        /// (the probe keeps scanning hits behind this one).
        /// </summary>
        CursorHint GetCursorHint(in CursorQueryContext context);
    }

    /// <summary>A hover target's answer: which preset to show and how strongly.</summary>
    public readonly struct CursorHint
    {
        /// <summary>Preset to show; null = no opinion.</summary>
        public readonly string PresetId;

        /// <summary>Optional cursor set; null = current set.</summary>
        public readonly string SetId;

        /// <summary>Added to the hover layer priority; lets important targets outrank others.</summary>
        public readonly int PriorityOffset;

        public CursorHint(string presetId, string setId = null, int priorityOffset = 0)
        {
            PresetId = presetId;
            SetId = setId;
            PriorityOffset = priorityOffset;
        }

        public static CursorHint None => default;

        public bool HasOpinion => !string.IsNullOrWhiteSpace(PresetId);
    }

    /// <summary>
    /// Context handed to <see cref="ICursorHoverTarget.GetCursorHint"/> so targets can answer
    /// situationally (e.g. a different cursor depending on the active tool or player).
    /// </summary>
    public readonly struct CursorQueryContext
    {
        public readonly Vector2 ScreenPosition;
        public readonly Vector3 WorldPosition;
        public readonly Camera Camera;

        /// <summary>
        /// Opaque game-specific context supplied by <see cref="GameContextProvider"/>
        /// (e.g. the mouse-owning player). The framework never inspects it; game-side
        /// hover targets cast it to whatever the game registered.
        /// </summary>
        public readonly object GameContext;

        /// <summary>Set by game code to enrich hover queries; may be null.</summary>
        public static Func<object> GameContextProvider;

        public CursorQueryContext(Vector2 screenPosition, Vector3 worldPosition, Camera camera, object gameContext)
        {
            ScreenPosition = screenPosition;
            WorldPosition = worldPosition;
            Camera = camera;
            GameContext = gameContext;
        }
    }
}
