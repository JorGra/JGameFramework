namespace JG.Modding
{
    public interface IModEntryPoint
    {
        void Initialize(IModContext context);
    }

    public interface IModContext
    {
        string ModId { get; }
        string ModPath { get; }

        /// <summary>
        /// Game-specific services registered by the host project before mod entry points run.
        /// The framework itself never populates this — the host game does via <see cref="ModServiceRegistry"/>.
        /// </summary>
        IModServiceProvider Services { get; }
    }
}
