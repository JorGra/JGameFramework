using System;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JGameFramework.UI.Tooltips
{
    public readonly struct TooltipBindingContext
    {
        public readonly TooltipHandle Handle;
        public readonly TooltipSystemRoot System;
        public readonly TooltipPlayerContext PlayerContext;

        public TooltipBindingContext(TooltipHandle handle, TooltipSystemRoot system, TooltipPlayerContext playerContext)
        {
            Handle = handle;
            System = system;
            PlayerContext = playerContext;
        }
    }

    public abstract class TooltipContentViewBase : MonoBehaviour
    {
        public abstract Type SupportedDataType { get; }
        public abstract void Bind(TooltipContentData data, TooltipBindingContext context);
        public virtual void Unbind() { }
    }

    public abstract class TooltipContentView<TData> : TooltipContentViewBase where TData : TooltipContentData
    {
        public override Type SupportedDataType => typeof(TData);

        public override void Bind(TooltipContentData data, TooltipBindingContext context)
        {
            if (data is not TData typed)
            {
                Debug.LogError($"Tooltip view '{name}' expected data type {typeof(TData).Name} but received {data?.GetType().Name}.");
                return;
            }

            Bind(typed, context);
        }

        protected abstract void Bind(TData data, TooltipBindingContext context);
    }

    internal static class TMProUtilities
    {
        private static readonly Regex RichTextRegex = new Regex("<.*?>", RegexOptions.Compiled);

        public static string ToPlainText(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            return RichTextRegex.Replace(input, string.Empty);
        }
    }
}
