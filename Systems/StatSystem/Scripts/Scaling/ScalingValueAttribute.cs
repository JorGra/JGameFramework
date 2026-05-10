using System;

namespace JG.Scaling
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class ScalingValueAttribute : Attribute
    {
        public string PreviewLabel { get; }

        public ScalingValueAttribute() { PreviewLabel = null; }

        public ScalingValueAttribute(string previewLabel) { PreviewLabel = previewLabel; }
    }
}
