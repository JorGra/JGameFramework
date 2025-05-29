namespace UI.Theming
{
    /// <summary>
    /// Implemented by components that react to a theme change.
    /// </summary>
    public interface IThemeable
    {
        /// <summary>Apply the supplied theme immediately.</summary>
        /// <param name="theme">Theme data asset.</param>
        void ApplyTheme(ThemeAsset theme);
    }
}
