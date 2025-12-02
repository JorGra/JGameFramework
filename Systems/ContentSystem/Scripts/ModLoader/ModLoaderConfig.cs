namespace JG.Modding
{
    /// <summary>Runtime configuration for <see cref="ModLoader"/>.</summary>
    public sealed class ModLoaderConfig
    {
        public string modsRoot = "Mods";               // discovery root
        public string stateFile = "modstate.json";     // persisted UI state
    }
}
