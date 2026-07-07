namespace JG.CursorSystem
{
    /// <summary>
    /// Well-known priority layers for cursor claims. Higher values win.
    /// Use offsets (e.g. <see cref="Tool"/> + 5) to order claims within a layer.
    /// </summary>
    public static class CursorLayer
    {
        /// <summary>Controller-owned baseline claim; always present.</summary>
        public const int Default = 0;

        /// <summary>Scene-scoped defaults (menu pointer, gameplay crosshair) and legacy event requests.</summary>
        public const int Scene = 50;

        /// <summary>Game-state overrides such as pause, stage end, cinematics with input.</summary>
        public const int GameState = 100;

        /// <summary>Active tool/mode cursors such as build mode.</summary>
        public const int Tool = 200;

        /// <summary>Hover over world objects (colliders).</summary>
        public const int HoverWorld = 300;

        /// <summary>Hover over UI elements; beats world hover.</summary>
        public const int HoverUI = 400;

        /// <summary>System-level overrides (cursor hidden for cinematic, hard locks).</summary>
        public const int System = 500;
    }
}
