// StyleSheets.cs
using System;
using System.Collections.Generic;
using UnityEngine;

namespace UI.Theming
{
    /// <summary>
    /// Non-generic root for every style-sheet wrapper stored in a <see cref="ThemeAsset"/>.
    /// A sheet groups *one* style type and exposes look-ups.
    /// </summary>
    [Serializable]
    public abstract class StyleSheetBase
    {
        /// <summary>Return the concrete style parameter type this sheet stores.</summary>
        public abstract Type StyleType { get; }

        /// <summary>Try to fetch a style as its base class.</summary>
        internal abstract bool TryGetUntyped(string styleKey, out StyleModuleParameters parameters);
    }

    /// <summary>
    /// Generic helper that hosts a typed list of <typeparamref name="T"/>.
    /// Concrete, serialisable subclasses (e.g. <c>TextStyleSheet</c>) provide the closed type.
    /// </summary>
    /// <typeparam name="T">Concrete style-parameter class.</typeparam>
    [Serializable]
    public abstract class StyleSheet<T> : StyleSheetBase where T : StyleModuleParameters
    {
        [SerializeField] private List<T> styles = new();

        /// <inheritdoc/>
        public override Type StyleType => typeof(T);

        /// <summary>
        /// Try to fetch a strongly-typed style from this sheet.
        /// </summary>
        public bool TryGet(string styleKey, out T style)
        {
            for (int i = 0; i < styles.Count; i++)
            {
                if (styles[i].StyleKey == styleKey)
                {
                    style = styles[i];
                    return true;
                }
            }
            style = null;
            return false;
        }

        /// <inheritdoc/>
        internal override bool TryGetUntyped(string styleKey, out StyleModuleParameters parameters)
        {
            if (TryGet(styleKey, out T concrete))
            {
                parameters = concrete;
                return true;
            }

            parameters = null;
            return false;
        }
    }

    // ─────────────────────────────────────────────────────────── concrete sheets ──
    /// <summary>Sheet that stores <see cref="TextStyleParameters"/> items.</summary>
    [Serializable] public sealed class TextStyleSheet : StyleSheet<TextStyleParameters> { }

    /// <summary>Sheet that stores <see cref="ImageStyleParameters"/> items.</summary>
    [Serializable] public sealed class ImageStyleSheet : StyleSheet<ImageStyleParameters> { }

    /// <summary>Sheet that stores <see cref="ToggleStyleParameters"/> items.</summary>
    [Serializable] public sealed class ToggleStyleSheet : StyleSheet<ToggleStyleParameters> { }
    /// <summary>Sheet that stores <see cref="ScrollRectStyleParameters"/> items.</summary>
    [Serializable]
    public sealed class ScrollRectStyleSheet : StyleSheet<ScrollRectStyleParameters> { }

    /// <summary>Sheet that stores <see cref="ButtonStyleParameters"/> items.</summary>
    [Serializable] public sealed class ButtonStyleSheet : StyleSheet<ButtonStyleParameters> { }
}
