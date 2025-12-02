using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JG.Modding.UI
{
    /// <summary>
    /// Displays full information about a selected mod: metadata, dependencies,
    /// load constraints and any error raised by the loader.
    /// </summary>
    [AddComponentMenu("JG/Modding/Mod Detail Panel")]
    public sealed class ModDetailPanelUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image icon;
        [SerializeField] private TextMeshProUGUI textHeader;
        [SerializeField] private TextMeshProUGUI textAuthor;
        [SerializeField] private TextMeshProUGUI textDescription;
        [SerializeField] private TextMeshProUGUI textRequires;
        [SerializeField] private TextMeshProUGUI textLoadBefore;
        [SerializeField] private TextMeshProUGUI textLoadAfter;
        [SerializeField] private TextMeshProUGUI textEnabled;
        [SerializeField] private TextMeshProUGUI textError;

        [SerializeField] Transform ContentRect;

        private void Start()
        {
            ContentRect.gameObject.SetActive(false);
        }

        /// <summary>
        /// Update all fields according to the selected mod.
        /// </summary>
        public void Show(LoadedMod mod, bool enabled, string errorMsg)
        {
            ContentRect.gameObject.SetActive(true);

            textHeader.text = $"{mod.Manifest.name} <size=70%>v{mod.Manifest.version}</size>";
            textAuthor.text = string.IsNullOrEmpty(mod.Manifest.author)
                                 ? "—"
                                 : mod.Manifest.author;
            textDescription.text = mod.Manifest.description ?? string.Empty;
            textRequires.text = mod.Manifest.requires.Length == 0
                                 ? "—"
                                 : string.Join(", ", mod.Manifest.requires);
            textLoadBefore.text = mod.Manifest.loadBefore.Length == 0
                                 ? "—"
                                 : string.Join(", ", mod.Manifest.loadBefore);
            textLoadAfter.text = mod.Manifest.loadAfter.Length == 0
                                 ? "—"
                                 : string.Join(", ", mod.Manifest.loadAfter);
            textEnabled.text = enabled ? "Enabled" : "Disabled";

            textError.gameObject.SetActive(!string.IsNullOrEmpty(errorMsg));
            textError.text = errorMsg;

            // icon may not be available if the row failed to load it
            if (!string.IsNullOrEmpty(mod.Manifest.icon))
            {
                using var s = mod.Handle.OpenFile(mod.Manifest.icon);
                if (s == null) return;

                var bytes = new byte[s.Length];
                s.Read(bytes, 0, bytes.Length);

                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (tex.LoadImage(bytes))
                    icon.sprite = Sprite.Create(tex,
                        new Rect(0, 0, tex.width, tex.height),
                        new Vector2(.5f, .5f));
            }
        }
    }
}
