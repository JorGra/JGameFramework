using System;
using TMPro;
using UnityEngine;

namespace JGameFramework.UI.Tooltips
{
    /// <summary>
    /// Base type for tooltip content payloads.
    /// </summary>
    [Serializable]
    public abstract class TooltipContentData
    {
        public virtual string Key => GetType().FullName;
    }

    [Serializable]
    public sealed class TooltipTextBlockData : TooltipContentData
    {
        [TextArea] public string Header;
        [TextArea] public string Body;
        public bool ShowHeader = true;
        public bool UseRichText = true;
        public TMP_FontAsset HeaderFont;
        public TMP_FontAsset BodyFont;
        public Color HeaderColor = Color.white;
        public Color BodyColor = Color.white;
        public TextAlignmentOptions HeaderAlignment = TextAlignmentOptions.Left;
        public TextAlignmentOptions BodyAlignment = TextAlignmentOptions.Left;
        public float Spacing = 4f;
    }

    [Serializable]
    public sealed class TooltipImageBlockData : TooltipContentData
    {
        public Sprite Sprite;
        public Vector2 PreferredSize = new Vector2(96f, 96f);
        public bool PreserveAspect = true;
        public string Caption;
        public TextAlignmentOptions CaptionAlignment = TextAlignmentOptions.Center;
        public TMP_FontAsset CaptionFont;
        public Color CaptionColor = Color.white;
        public float CaptionSpacing = 6f;
    }

    [Serializable]
    public sealed class TooltipItemHeaderBlockData : TooltipContentData
    {
        public Sprite Sprite;
        public string Title;
        public string Caption;
    }

    [Serializable]
    public sealed class TooltipKeyValueRowData : TooltipContentData
    {
        public string Label;
        public string Value;
        public Color LabelColor = new Color(0.85f, 0.85f, 0.85f);
        public Color ValueColor = Color.white;
        public TextAlignmentOptions Alignment = TextAlignmentOptions.Justified;
        public TMP_FontAsset LabelFont;
        public TMP_FontAsset ValueFont;
    }

    [Serializable]
    public sealed class TooltipSpacerData : TooltipContentData
    {
        public float Height = 12f;
    }

    [Serializable]
    public sealed class TooltipSeparatorData : TooltipContentData
    {
        public float Height = 3f;
        public Color Color = new Color(1,1,1,1);

    }
}
