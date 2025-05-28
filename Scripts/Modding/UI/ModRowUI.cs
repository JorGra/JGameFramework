using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JG.Modding.UI
{
    public sealed class ModRowUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Toggle toggleEnable;
        [SerializeField] private TextMeshProUGUI textName;
        [SerializeField] private TextMeshProUGUI textVersion;
        [SerializeField] private TextMeshProUGUI textAuthor;
        [SerializeField] private Button buttonUp;
        [SerializeField] private Button buttonDown;
        [SerializeField] private CanvasGroup canvasGroup;

        ModLoader loader;
        ModListUI listUI;
        string modId;

        public void Init(LoadedMod lm, ModLoader loader, ModListUI listUI)
        {
            this.loader = loader;
            this.listUI = listUI;
            modId = lm.Manifest.id;

            textName.text = lm.Manifest.name;
            textVersion.text = lm.Manifest.version;
            textAuthor.text = lm.Manifest.author;

            var enabled = loader.IsModEnabled(modId);
            toggleEnable.isOn = enabled;
            canvasGroup.alpha = enabled ? 1f : 0.4f;

            toggleEnable.onValueChanged.AddListener(OnToggle);
            buttonUp.onClick.AddListener(() => Move(-1));
            buttonDown.onClick.AddListener(() => Move(+1));
        }

        void OnToggle(bool on)
        {
            loader.Enable(modId, on);
            canvasGroup.alpha = on ? 1f : 0.4f;
        }

        void Move(int delta)
        {
            int rowIndex = transform.GetSiblingIndex();
            loader.Move(modId, rowIndex + delta);
            listUI.Refresh();
            listUI.OnRowMoved();
        }

        public void SetMoveButtonsInteractable(bool canMoveUp, bool canMoveDown)
        {
            buttonUp.interactable = canMoveUp;
            buttonDown.interactable = canMoveDown;
        }
    }
}
