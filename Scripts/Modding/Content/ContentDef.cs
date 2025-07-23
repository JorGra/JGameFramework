using UnityEngine;

namespace JG.GameContent
{
    /// <summary>Common contract for every piece of mod‑able data.</summary>
    public interface IContentDef
    {
        /// <summary>Globally‑unique, case‑insensitive identifier.</summary>
        string Id { get; }
        string SourceFile { get; set; }
    }

    /// <summary>
    /// Design‑time ScriptableObject sharing the exact same data contract
    /// as the JSON that ships with mods. Never loaded at runtime – only here
    /// to give designers an Inspector view and allow copy‑pasting defaults.
    /// </summary>
    public abstract class ContentDef : ScriptableObject, IContentDef
    {
        [SerializeField, Tooltip("Globally‑unique ID; case‑insensitive.")]
        private string id;
        private string sourceFile;

        public string Id
        {
            get => id;
            set => id = value;
        }
        public string SourceFile { get => sourceFile; set => sourceFile = value; }
    }
}
