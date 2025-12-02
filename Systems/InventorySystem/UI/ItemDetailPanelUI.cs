using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JG.Inventory.UI
{
    /// <summary>
    /// Tooltip panel local to a single inventory window.
    /// Multiple panels may exist at the same time (split-screen / local co-op).
    /// </summary>
    public class ItemDetailPanelUI : MonoBehaviour
    {
        [Header("Bindings")]
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text description;

        void Awake() => Hide();

        /// <summary>Fills the UI with <paramref name="stack"/> data and shows the panel.</summary>
        /// <param name="stack">Stack to display, or <c>null</c> to hide.</param>
        public void Show(ItemStack stack)
        {
            if (stack == null || stack.Data == null) { Hide(); return; }

            icon.sprite = stack.Data.Icon;
            icon.enabled = icon.sprite != null;

            title.text = stack.Data.DisplayName;
            description.text = $"x{stack.Count}  •  Max Stack {stack.Data.MaxStack}";

            gameObject.SetActive(true);
        }

        /// <summary>Hides the panel.</summary>
        public void Hide() => gameObject.SetActive(false);
    }
}
