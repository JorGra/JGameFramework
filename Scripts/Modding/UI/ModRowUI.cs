using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JG.Modding.UI
{
    /// <summary>One row in the Mod Manager list.</summary>
    public sealed class ModRowUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Toggle toggleEnable;
        [SerializeField] private TextMeshProUGUI textName;
        [SerializeField] private TextMeshProUGUI textVersion;
        [SerializeField] private TextMeshProUGUI textAuthor;
        [SerializeField] private Button buttonUp;
        [SerializeField] private Button buttonDown;
        [SerializeField] private Image background;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image iconImage;

        [Header("Colours")]
        [SerializeField] private Color errorColour = new(1f, .45f, .45f, 0.4f);

        ModLoader loader;
        ModListUI listUI;
        LoadedMod loadedMod;
        string errorMsg;

        public void Init(LoadedMod lm, ModLoader loader, ModListUI listUI, string errorMsg = null)
        {
            this.loader = loader;
            this.listUI = listUI;
            loadedMod = lm;
            this.errorMsg = errorMsg;

            textName.text = lm.Manifest.name;
            textVersion.text = lm.Manifest.version;
            textAuthor.text = lm.Manifest.author;

            bool enabled = loader.IsModEnabled(lm.Manifest.id);
            toggleEnable.isOn = enabled;
            background.canvasRenderer.SetAlpha(enabled ? 1f : .4f);
            canvasGroup.alpha = enabled ? 1f : .4f;

            LoadIcon(lm);

            toggleEnable.onValueChanged.AddListener(OnToggle);
            buttonUp.onClick.AddListener(() => Move(-1));
            buttonDown.onClick.AddListener(() => Move(+1));

            UpdateErrorVisual();
        }

        public void SelectMod() => listUI.OnRowSelected(this);

        public LoadedMod LoadedMod => loadedMod;
        public string ErrorMessage => errorMsg;

        public void UpdateError(string msg)
        {
            errorMsg = msg;
            UpdateErrorVisual();
        }

        void OnToggle(bool on)
        {
            loader.Enable(loadedMod.Manifest.id, on);
            background.canvasRenderer.SetAlpha(on ? 1f : .4f);
            canvasGroup.alpha = on ? 1f : .4f;
        }

        void Move(int delta)
        {
            int rowIndex = listUI.IndexOf(this);
            if (rowIndex == -1) return;

            if (loader.Move(loadedMod.Manifest.id, rowIndex + delta))
            {
                listUI.Refresh();
                listUI.OnRowMoved();
            }
        }

        void UpdateErrorVisual()
        {
            background.color = string.IsNullOrEmpty(errorMsg)
                             ? Color.white
                             : errorColour;
        }

        void LoadIcon(LoadedMod lm)
        {
            if (string.IsNullOrEmpty(lm.Manifest.icon)) return;

            using var s = lm.Handle.OpenFile(lm.Manifest.icon);
            if (s == null) return;

            var bytes = new byte[s.Length];
            s.Read(bytes, 0, bytes.Length);

            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (tex.LoadImage(bytes))
                iconImage.sprite = Sprite.Create(tex,
                    new Rect(0, 0, tex.width, tex.height),
                    new Vector2(.5f, .5f));
        }

        public void SetMoveButtonsInteractable(bool up, bool down)
        {
            buttonUp.interactable = up;
            buttonDown.interactable = down;
        }
    }
}
