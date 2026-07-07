using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace JG.CursorSystem
{
    /// <summary>How the resolved cursor preset is shown on screen.</summary>
    public enum CursorPresenterMode
    {
        /// <summary>Overlay on Linux (avoids dual-cursor and OS scaling issues), hardware elsewhere.</summary>
        Auto,
        /// <summary>OS hardware cursor via <see cref="Cursor.SetCursor(Texture2D, Vector2, CursorMode)"/>.</summary>
        Hardware,
        /// <summary>Software cursor rendered as a top-most UI image following the pointer.</summary>
        Overlay,
    }

    /// <summary>Presentation backend used by <see cref="MouseCursorController"/>.</summary>
    public interface ICursorPresenter
    {
        /// <summary>Show the given preset. <paramref name="claimVisibility"/> overrides the preset's own visibility override.</summary>
        bool Apply(CursorPreset preset, bool? claimVisibility);

        /// <summary>Per-frame hook (overlay follows the pointer); no-op for hardware.</summary>
        void Tick();

        /// <summary>Release created textures/objects.</summary>
        void Cleanup();
    }

    /// <summary>
    /// Classic hardware cursor path. Includes the Linux downscale fallback for oversized textures.
    /// </summary>
    public sealed class HardwareCursorPresenter : ICursorPresenter
    {
        readonly int linuxMaxCursorSize;
        readonly int linuxTargetCursorSize;
        readonly bool linuxForceSoftwareCursor;
        readonly bool logWarnings;

        Texture2D cachedScaledTexture;

        public HardwareCursorPresenter(int linuxMaxCursorSize, int linuxTargetCursorSize, bool linuxForceSoftwareCursor, bool logWarnings)
        {
            this.linuxMaxCursorSize = linuxMaxCursorSize;
            this.linuxTargetCursorSize = linuxTargetCursorSize;
            this.linuxForceSoftwareCursor = linuxForceSoftwareCursor;
            this.logWarnings = logWarnings;
        }

        public bool Apply(CursorPreset preset, bool? claimVisibility)
        {
            if (!TryGetPlatformCursorData(preset, out var texture, out var hotSpot, out var mode, out var newScaledTexture))
                return false;

            Cursor.SetCursor(texture, hotSpot, mode);

            // Destroy the previously scaled texture only after the new cursor is set,
            // so the hardware cursor never points at a destroyed texture.
            if (newScaledTexture != cachedScaledTexture && cachedScaledTexture != null)
                Object.Destroy(cachedScaledTexture);
            cachedScaledTexture = newScaledTexture;

            if (claimVisibility.HasValue)
                Cursor.visible = claimVisibility.Value;
            else if (preset.OverrideCursorVisibility)
                Cursor.visible = preset.CursorVisible;

            return true;
        }

        public void Tick() { }

        public void Cleanup()
        {
            if (cachedScaledTexture != null)
            {
                Object.Destroy(cachedScaledTexture);
                cachedScaledTexture = null;
            }
        }

        bool TryGetPlatformCursorData(CursorPreset preset, out Texture2D texture, out Vector2 hotSpot, out CursorMode mode, out Texture2D scaledTexture)
        {
            texture = preset.Texture;
            hotSpot = preset.HotSpot;
            mode = preset.Mode;
            scaledTexture = cachedScaledTexture;

            if (texture == null)
                return false;

            if (!IsLinuxPlatform())
            {
                hotSpot = ClampHotspot(hotSpot, texture.width, texture.height);
                scaledTexture = null;
                return true;
            }

            if (linuxForceSoftwareCursor)
                mode = CursorMode.ForceSoftware;

            var desiredSize = linuxTargetCursorSize > 0 ? linuxTargetCursorSize : linuxMaxCursorSize;
            var clampedMaxSize = Mathf.Max(16, desiredSize);
            var maxDimension = Mathf.Max(texture.width, texture.height);

            if (maxDimension <= clampedMaxSize)
            {
                hotSpot = ClampHotspot(hotSpot, texture.width, texture.height);
                scaledTexture = null;
                return true;
            }

            if (!texture.isReadable)
            {
                if (logWarnings)
                {
                    Debug.LogWarning(
                        "[HardwareCursorPresenter] Cursor texture is not readable; cannot downscale for Linux. " +
                        "Enable Read/Write in the texture import settings. Forcing software cursor to avoid OS scaling.");
                }
                mode = CursorMode.ForceSoftware;
                hotSpot = ClampHotspot(hotSpot, texture.width, texture.height);
                scaledTexture = null;
                return true;
            }

            var scale = clampedMaxSize / (float)maxDimension;
            var targetWidth = Mathf.Max(1, Mathf.RoundToInt(texture.width * scale));
            var targetHeight = Mathf.Max(1, Mathf.RoundToInt(texture.height * scale));
            var scaledHotSpot = hotSpot * scale;

            scaledTexture = CreateScaledTexture(texture, targetWidth, targetHeight);
            texture = scaledTexture;
            hotSpot = ClampHotspot(scaledHotSpot, targetWidth, targetHeight);
            return true;
        }

        static bool IsLinuxPlatform() =>
            Application.platform == RuntimePlatform.LinuxEditor ||
            Application.platform == RuntimePlatform.LinuxPlayer;

        static Vector2 ClampHotspot(Vector2 hotSpot, int width, int height)
        {
            var clampedX = Mathf.Clamp(hotSpot.x, 0, Mathf.Max(0, width - 1));
            var clampedY = Mathf.Clamp(hotSpot.y, 0, Mathf.Max(0, height - 1));
            return new Vector2(clampedX, clampedY);
        }

        static Texture2D CreateScaledTexture(Texture2D source, int targetWidth, int targetHeight)
        {
            var result = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32, mipChain: false)
            {
                name = string.IsNullOrWhiteSpace(source.name) ? "Cursor_LinuxScaled" : $"{source.name}_LinuxScaled",
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            for (int y = 0; y < targetHeight; y++)
            {
                var v = (y + 0.5f) / targetHeight;
                for (int x = 0; x < targetWidth; x++)
                {
                    var u = (x + 0.5f) / targetWidth;
                    result.SetPixel(x, y, source.GetPixelBilinear(u, v));
                }
            }

            result.Apply(updateMipmaps: false, makeNoLongerReadable: false);
            return result;
        }
    }

    /// <summary>
    /// Software cursor rendered as a top-most screen-space image following the pointer.
    /// Hides the OS cursor entirely, so it cannot produce the Linux dual-cursor problem,
    /// and its size is a fraction of screen height, so it stays consistent across resolutions.
    /// Textures do not need Read/Write enabled.
    /// </summary>
    public sealed class OverlayCursorPresenter : ICursorPresenter
    {
        readonly Transform parent;
        readonly float heightFraction;
        readonly Dictionary<Texture2D, Sprite> spriteCache = new Dictionary<Texture2D, Sprite>();

        GameObject overlayRoot;
        Image cursorImage;
        RectTransform cursorRect;
        CursorPreset activePreset;
        bool cursorShown = true;

        public OverlayCursorPresenter(Transform parent, float heightFraction)
        {
            this.parent = parent;
            this.heightFraction = Mathf.Clamp(heightFraction, 0.005f, 0.25f);
        }

        public bool Apply(CursorPreset preset, bool? claimVisibility)
        {
            if (preset.Texture == null)
                return false;

            EnsureOverlay();

            activePreset = preset;
            cursorImage.sprite = GetSprite(preset.Texture);

            // Hotspot is authored in texture pixels from the top-left corner (hardware-cursor
            // convention). Map it to a rect pivot: pivot origin is bottom-left, normalized.
            var tex = preset.Texture;
            var pivot = new Vector2(
                tex.width > 0 ? Mathf.Clamp01(preset.HotSpot.x / tex.width) : 0f,
                tex.height > 0 ? 1f - Mathf.Clamp01(preset.HotSpot.y / tex.height) : 1f);
            cursorRect.pivot = pivot;

            cursorShown = claimVisibility ?? (preset.OverrideCursorVisibility ? preset.CursorVisible : true);

            UpdateSize();
            Tick();
            return true;
        }

        public void Tick()
        {
            if (cursorImage == null || activePreset == null)
                return;

            // The OS cursor must stay hidden while the overlay is active; reassert every frame
            // because other systems (alt-tab, lock-state changes) can flip it back on.
            Cursor.visible = false;

            var mouse = Mouse.current;
            if (mouse == null || !cursorShown)
            {
                cursorImage.enabled = false;
                return;
            }

            var pos = mouse.position.ReadValue();
            var onScreen = pos.x >= 0 && pos.y >= 0 && pos.x <= Screen.width && pos.y <= Screen.height;
            cursorImage.enabled = onScreen && Application.isFocused;

            if (cursorImage.enabled)
            {
                UpdateSize();
                cursorRect.position = new Vector3(pos.x, pos.y, 0f);
            }
        }

        public void Cleanup()
        {
            Cursor.visible = true;

            foreach (var sprite in spriteCache.Values)
            {
                if (sprite != null)
                    Object.Destroy(sprite);
            }
            spriteCache.Clear();

            if (overlayRoot != null)
            {
                Object.Destroy(overlayRoot);
                overlayRoot = null;
                cursorImage = null;
                cursorRect = null;
            }

            activePreset = null;
        }

        void EnsureOverlay()
        {
            if (overlayRoot != null)
                return;

            overlayRoot = new GameObject("CursorOverlay", typeof(Canvas));
            overlayRoot.transform.SetParent(parent, worldPositionStays: false);

            var canvas = overlayRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;

            var imageGo = new GameObject("CursorImage", typeof(Image));
            imageGo.transform.SetParent(overlayRoot.transform, worldPositionStays: false);

            cursorImage = imageGo.GetComponent<Image>();
            cursorImage.raycastTarget = false; // must never block the EventSystem
            cursorImage.preserveAspect = true;
            cursorRect = cursorImage.rectTransform;
        }

        void UpdateSize()
        {
            var tex = activePreset?.Texture;
            if (tex == null || cursorRect == null)
                return;

            var height = Screen.height * heightFraction;
            var width = tex.height > 0 ? height * (tex.width / (float)tex.height) : height;
            cursorRect.sizeDelta = new Vector2(width, height);
        }

        Sprite GetSprite(Texture2D texture)
        {
            if (spriteCache.TryGetValue(texture, out var sprite) && sprite != null)
                return sprite;

            sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit: 100f,
                extrude: 0,
                SpriteMeshType.FullRect);
            sprite.name = $"{texture.name}_CursorSprite";
            sprite.hideFlags = HideFlags.HideAndDontSave;

            spriteCache[texture] = sprite;
            return sprite;
        }
    }
}
