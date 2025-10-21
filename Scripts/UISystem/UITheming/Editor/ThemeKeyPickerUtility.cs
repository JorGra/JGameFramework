using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace UI.Theming.Editor
{
    static class ThemeKeyPickerUtility
    {
        static readonly Dictionary<ThemeKeyRequest, List<ThemeKeyInfo>> cache = new();
        static readonly FieldInfo colorsField = typeof(ThemeAsset).GetField("colors", BindingFlags.NonPublic | BindingFlags.Instance);
        static readonly FieldInfo spritesField = typeof(ThemeAsset).GetField("sprites", BindingFlags.NonPublic | BindingFlags.Instance);
        static readonly FieldInfo fontsField = typeof(ThemeAsset).GetField("fonts", BindingFlags.NonPublic | BindingFlags.Instance);
        static readonly FieldInfo paletteField = typeof(ThemeAsset).GetField("palette", BindingFlags.NonPublic | BindingFlags.Instance);
        static readonly FieldInfo spriteKeyField;
        static readonly FieldInfo spriteSpriteField;
        static readonly FieldInfo spriteImageTypeField;
        static readonly FieldInfo spritePixelPerUnitField;
        static readonly FieldInfo colorKeyField;
        static readonly FieldInfo colorColorField;
        static readonly FieldInfo fontKeyField;
        static readonly FieldInfo fontAssetField;
        static readonly FieldInfo paletteColorsField;
        static readonly FieldInfo paletteColorKeyField;
        static readonly FieldInfo paletteColorColorField;
        static readonly FieldInfo styleSheetsField = typeof(ThemeAsset).GetField("styleSheets", BindingFlags.NonPublic | BindingFlags.Instance);

        static ThemeKeyPickerUtility()
        {
            EditorApplication.projectChanged += ClearCache;
            AssemblyReloadEvents.afterAssemblyReload += ClearCache;

            if (spritesField != null)
            {
                var spriteEntryType = spritesField.FieldType.GenericTypeArguments.Length > 0
                    ? spritesField.FieldType.GenericTypeArguments[0]
                    : spritesField.FieldType.GetElementType();

                if (spriteEntryType != null)
                {
                    spriteKeyField = spriteEntryType.GetField("key", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    spriteSpriteField = spriteEntryType.GetField("sprite", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    spriteImageTypeField = spriteEntryType.GetField("imageType", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    spritePixelPerUnitField = spriteEntryType.GetField("pixelPerUnit", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                }
            }

            if (colorsField != null)
            {
                var colorEntryType = colorsField.FieldType.GenericTypeArguments.Length > 0
                    ? colorsField.FieldType.GenericTypeArguments[0]
                    : colorsField.FieldType.GetElementType();

                if (colorEntryType != null)
                {
                    colorKeyField = colorEntryType.GetField("key", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    colorColorField = colorEntryType.GetField("color", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                }
            }

            if (fontsField != null)
            {
                var fontEntryType = fontsField.FieldType.GenericTypeArguments.Length > 0
                    ? fontsField.FieldType.GenericTypeArguments[0]
                    : fontsField.FieldType.GetElementType();

                if (fontEntryType != null)
                {
                    fontKeyField = fontEntryType.GetField("key", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    fontAssetField = fontEntryType.GetField("font", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                }
            }

            if (paletteField != null)
            {
                var paletteType = paletteField.FieldType;
                paletteColorsField = paletteType.GetField("colors", BindingFlags.NonPublic | BindingFlags.Instance);
                if (paletteColorsField != null)
                {
                    var paletteEntryType = paletteColorsField.FieldType.GenericTypeArguments.Length > 0
                        ? paletteColorsField.FieldType.GenericTypeArguments[0]
                        : paletteColorsField.FieldType.GetElementType();
                    if (paletteEntryType != null)
                    {
                        paletteColorKeyField = paletteEntryType.GetField("key", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        paletteColorColorField = paletteEntryType.GetField("color", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    }
                }
            }
        }

        public static List<ThemeKeyInfo> GetKeys(ThemeKeyKind kind, Type styleType)
        {
            var request = new ThemeKeyRequest(kind, styleType);
            if (!cache.TryGetValue(request, out var list) || list == null)
            {
                list = BuildKeyList(kind, styleType);
                cache[request] = list;
            }
            return list;
        }

        public static void ClearCache()
        {
            cache.Clear();
        }

        static List<ThemeKeyInfo> BuildKeyList(ThemeKeyKind kind, Type styleType)
        {
            var result = new Dictionary<string, ThemeKeyInfo>(StringComparer.Ordinal);
            var guids = AssetDatabase.FindAssets("t:ThemeAsset");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var theme = AssetDatabase.LoadAssetAtPath<ThemeAsset>(path);
                if (!theme) continue;

                switch (kind)
                {
                    case ThemeKeyKind.Color:
                        CollectColors(theme, result);
                        break;
                    case ThemeKeyKind.Sprite:
                        CollectSprites(theme, result);
                        break;
                    case ThemeKeyKind.Font:
                        CollectFonts(theme, result);
                        break;
                    case ThemeKeyKind.Style:
                        CollectStyles(theme, styleType, result);
                        break;
                }
            }

            var sorted = new List<ThemeKeyInfo>(result.Values);
            sorted.Sort((a, b) => string.Compare(a.Key, b.Key, StringComparison.OrdinalIgnoreCase));
            return sorted;
        }

        static void CollectColors(ThemeAsset theme, Dictionary<string, ThemeKeyInfo> destination)
        {
            // Palette colours first so overrides can replace them later.
            var palette = paletteField?.GetValue(theme);
            if (palette != null && paletteColorsField != null)
            {
                if (paletteColorsField.GetValue(palette) is IEnumerable paletteList)
                {
                    foreach (var entry in paletteList)
                    {
                        string key = paletteColorKeyField?.GetValue(entry) as string;
                        if (string.IsNullOrEmpty(key)) continue;
                        var color = paletteColorColorField != null ? (Color)paletteColorColorField.GetValue(entry) : Color.white;
                        destination[key] = new ThemeKeyInfo
                        {
                            Key = key,
                            Theme = theme,
                            Color = color
                        };
                    }
                }
            }

            if (colorsField?.GetValue(theme) is IEnumerable colorList)
            {
                foreach (var entry in colorList)
                {
                    string key = colorKeyField?.GetValue(entry) as string;
                    if (string.IsNullOrEmpty(key)) continue;
                    var color = colorColorField != null ? (Color)colorColorField.GetValue(entry) : Color.white;
                    destination[key] = new ThemeKeyInfo
                    {
                        Key = key,
                        Theme = theme,
                        Color = color
                    };
                }
            }
        }

        static void CollectSprites(ThemeAsset theme, Dictionary<string, ThemeKeyInfo> destination)
        {
            if (spritesField?.GetValue(theme) is IEnumerable spriteList)
            {
                foreach (var entry in spriteList)
                {
                    string key = spriteKeyField?.GetValue(entry) as string;
                    if (string.IsNullOrEmpty(key)) continue;
                    var sprite = spriteSpriteField?.GetValue(entry) as Sprite;
                    destination[key] = new ThemeKeyInfo
                    {
                        Key = key,
                        Theme = theme,
                        Sprite = sprite
                    };
                }
            }
        }

        static void CollectFonts(ThemeAsset theme, Dictionary<string, ThemeKeyInfo> destination)
        {
            if (fontsField?.GetValue(theme) is IEnumerable fontList)
            {
                foreach (var entry in fontList)
                {
                    string key = fontKeyField?.GetValue(entry) as string;
                    if (string.IsNullOrEmpty(key)) continue;
                    var font = fontAssetField?.GetValue(entry) as TMP_FontAsset;
                    destination[key] = new ThemeKeyInfo
                    {
                        Key = key,
                        Theme = theme,
                        Font = font
                    };
                }
            }
        }

        static void CollectStyles(ThemeAsset theme, Type styleType, Dictionary<string, ThemeKeyInfo> destination)
        {
            if (styleType == null) return;
            if (styleSheetsField?.GetValue(theme) is IEnumerable sheetList)
            {
                foreach (var sheetObj in sheetList)
                {
                    if (sheetObj is not StyleSheetBase sheet) continue;
                    var concreteType = sheet.StyleType;
                    if (concreteType == null || !styleType.IsAssignableFrom(concreteType))
                        continue;

                    var stylesField = FindStylesField(sheetObj.GetType());
                    if (stylesField == null) continue;
                    var stylesList = stylesField.GetValue(sheetObj) as IEnumerable;
                    if (stylesList == null) continue;

                    foreach (var style in stylesList)
                    {
                        if (style is not StyleModuleParameters module) continue;
                        string key = module.StyleKey;
                        if (string.IsNullOrEmpty(key)) continue;

                        destination[key] = new ThemeKeyInfo
                        {
                            Key = key,
                            Theme = theme,
                            Style = module,
                            StyleType = concreteType
                        };
                    }
                }
            }
        }

        static FieldInfo FindStylesField(Type type)
        {
            while (type != null)
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(StyleSheet<>))
                {
                    return type.GetField("styles", BindingFlags.NonPublic | BindingFlags.Instance);
                }
                type = type.BaseType;
            }
            return null;
        }

        public static ThemeKeyInfo? Find(List<ThemeKeyInfo> keys, string value)
        {
            if (string.IsNullOrEmpty(value)) return null;
            for (int i = 0; i < keys.Count; i++)
            {
                if (keys[i].Key == value)
                    return keys[i];
            }
            return null;
        }
    }

    readonly struct ThemeKeyRequest : IEquatable<ThemeKeyRequest>
    {
        public ThemeKeyKind Kind { get; }
        public Type StyleType { get; }

        public ThemeKeyRequest(ThemeKeyKind kind, Type styleType)
        {
            Kind = kind;
            StyleType = styleType;
        }

        public bool Equals(ThemeKeyRequest other) => Kind == other.Kind && StyleType == other.StyleType;

        public override bool Equals(object obj) => obj is ThemeKeyRequest other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)Kind * 397) ^ (StyleType != null ? StyleType.GetHashCode() : 0);
            }
        }
    }

    struct ThemeKeyInfo
    {
        public string Key;
        public ThemeAsset Theme;
        public Color? Color;
        public Sprite Sprite;
        public TMP_FontAsset Font;
        public StyleModuleParameters Style;
        public Type StyleType;

        public string SourceName => Theme ? Theme.name : "Unknown";
    }
}
