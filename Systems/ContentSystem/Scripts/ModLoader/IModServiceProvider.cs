namespace JG.Modding
{
    /// <summary>
    /// Project-agnostic service locator that games populate before mod entry points run.
    /// Mods retrieve game-specific services (singletons, registries, etc.) via <see cref="Get{T}"/>.
    /// The framework never registers anything here — only the host game does.
    /// </summary>
    public interface IModServiceProvider
    {
        /// <summary>Returns the registered service of type <typeparamref name="T"/>, or throws if not registered.</summary>
        T Get<T>() where T : class;

        /// <summary>Returns true and sets <paramref name="service"/> if a service of type <typeparamref name="T"/> is registered.</summary>
        bool TryGet<T>(out T service) where T : class;
    }
}
