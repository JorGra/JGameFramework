using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace JG.Modding.UI
{
    /// <summary>
    /// One row in the Mod Manager list – shows basic data, handles enable/disable
    /// toggles, order buttons and signals selection back to <see cref="ModListUI"/>.
    /// </summary>
    public sealed class ModRowUI : MonoBehaviour
    {
        /* ---------- UI references ---------------------------------- */
        [Header("UI References")]
        [SerializeField] private Toggle toggleEnable;
        [SerializeField] private TextMeshProUGUI textName;
        [SerializeField] private TextMeshProUGUI textVersion;
        [SerializeField] private TextMeshProUGUI textAuthor;
        [SerializeField] private Button buttonUp;
        [SerializeField] private Button buttonDown;
        [SerializeField] private Image background;
        [SerializeField] private Image iconImage;            // NEW: mod icon

        [Header("Colours")]
        [SerializeField] private Color errorColour = new(1f, .45f, .45f, 1f);

        /* ---------- state ------------------------------------------ */
        ModLoader loader;
        ModListUI listUI;
        LoadedMod loadedMod;
        string errorMsg;

        /* ---------- initialisation --------------------------------- */
        public void Init(LoadedMod lm,
                         ModLoader loader,
                         ModListUI listUI,
                         string errorMsg = null)
        {
            this.loader = loader;
            this.listUI = listUI;
            this.loadedMod = lm;
            this.errorMsg = errorMsg;

            textName.text = lm.Manifest.name;
            textVersion.text = lm.Manifest.version;
            textAuthor.text = lm.Manifest.author;

            bool enabled = loader.IsModEnabled(lm.Manifest.id);
            toggleEnable.isOn = enabled;
            background.canvasRenderer.SetAlpha(enabled ? 1f : .4f);

            LoadIcon(lm);

            toggleEnable.onValueChanged.AddListener(OnToggle);
            buttonUp.onClick.AddListener(() => Move(-1));
            buttonDown.onClick.AddListener(() => Move(+1));

            UpdateErrorVisual();
        }

        public void SelectMod()
            => listUI.OnRowSelected(this);

        /* ---------- external accessors ----------------------------- */
        public LoadedMod LoadedMod => loadedMod;
        public string ErrorMessage => errorMsg;

        public void UpdateError(string msg)
        {
            errorMsg = msg;
            UpdateErrorVisual();
        }

        /* ---------- private helpers -------------------------------- */
        void OnToggle(bool on)
        {
            loader.Enable(loadedMod.Manifest.id, on);
            background.canvasRenderer.SetAlpha(on ? 1f : .4f);
        }

        void Move(int delta)
        {
            int rowIndex = transform.GetSiblingIndex();
            loader.Move(loadedMod.Manifest.id, rowIndex + delta);
            listUI.Refresh();          // list recreated after reload
            listUI.OnRowMoved();
        }

        void UpdateErrorVisual()
        {
            background.color = string.IsNullOrEmpty(errorMsg)
                             ? Color.white
                             : errorColour;
        }

        void LoadIcon(LoadedMod lm)
        {
            if (string.IsNullOrEmpty(lm.Manifest.icon))
                return;

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

        /* ---------- public helpers used by ModListUI --------------- */
        public void SetMoveButtonsInteractable(bool canMoveUp, bool canMoveDown)
        {
            buttonUp.interactable = canMoveUp;
            buttonDown.interactable = canMoveDown;
        }
    }
}
