using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class RadialMenuButton : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Button Data")]
    public Image iconImage;
    public Sprite defaultIcon;
    public Text hiddenLabel;
    public UnityEvent onClick;

    /// <summary>
    /// The radial menu that owns this button.
    /// This is set by the radial menu at runtime.
    /// </summary>
    [HideInInspector]
    public UIRadialMenu hostMenu;

    /// <summary>
    /// The angle of this button in the radial menu (0 = east, 90 = north, etc.).
    /// </summary>
    [HideInInspector]
    public float angle;

    /// <summary>
    /// The data object if you're using RadialMenuItemData.
    /// (This is optional but helps tie back to your design-time data.)
    /// </summary>
    [HideInInspector]
    public RadialMenuItemData ItemData;

    // Exposed so the radial menu can show them in a context menu:
    public Sprite Icon { get; private set; }
    public string LabelText { get; private set; }

    void Awake()
    {
        // Assign fallback icon
        if (iconImage != null && defaultIcon != null)
        {
            iconImage.sprite = defaultIcon;
            Icon = defaultIcon;
        }

        // If there's a hidden label, copy its text at startup
        if (hiddenLabel != null)
        {
            LabelText = hiddenLabel.text;
        }
    }

    public void SetAngle(float angleDeg)
    {
        angle = angleDeg;
    }

    public void SetIcon(Sprite newIcon)
    {
        Icon = newIcon;
        if (iconImage != null && newIcon != null)
        {
            iconImage.sprite = newIcon;
        }
    }

    public void SetLabel(string label)
    {
        LabelText = label;
        if (hiddenLabel != null)
        {
            hiddenLabel.text = label;
        }
    }

    #region Pointer Event Handlers

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Optional: highlight logic
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Optional: unhighlight logic
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Delegate click handling to the radial menu
        hostMenu?.OnButtonClicked(this);
    }

    #endregion
}
