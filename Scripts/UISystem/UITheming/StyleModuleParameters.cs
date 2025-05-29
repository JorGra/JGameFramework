using System;
using UnityEngine;

namespace UI.Theming
{
    /// <summary>
    /// Abstract base for every style module stored inside <see cref="ThemeAsset"/>.
    /// </summary>
    [Serializable]
    public abstract class StyleModuleParameters
    {
        [SerializeField] private string styleKey = "Default";
        /// <summary>Unique identifier used by UI components.</summary>
        public string StyleKey => styleKey;
    }
}
