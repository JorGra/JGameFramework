using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Minimal component that raises <see cref="PlayUISoundEvent"/> for common
/// pointer actions. Requires a <see cref="Selectable"/> to ensure the object
/// participates in the UI navigation / focus system.
/// </summary>
[RequireComponent(typeof(Selectable))]
public class UIAudioBinder :
    MonoBehaviour,
    IPointerEnterHandler,
    IPointerDownHandler,
    IPointerClickHandler,
    IPointerUpHandler,
    ISelectHandler,
    ISubmitHandler
{
    [Tooltip("Name of the sound profile defined in the UIAudioTheme. "
           + "Leave empty or \"Default\" to use the default profile.")]
    [SerializeField] private string profile = "Default";

    #region Interface Callbacks
    public void OnPointerEnter(PointerEventData eventData) => Play(UIAudioAction.Hover);
    public void OnPointerDown(PointerEventData eventData) => Play(UIAudioAction.Press);
    public void OnPointerClick(PointerEventData eventData) => Play(UIAudioAction.Click);
    public void OnPointerUp(PointerEventData eventData) => Play(UIAudioAction.Release);
    #endregion

    void Play(UIAudioAction action)
    {
        EventBus<PlayUISoundEvent>.Raise(
            new PlayUISoundEvent(profile, action, transform.position));
    }

    public void OnSelect(BaseEventData eventData)
    {
        Play(UIAudioAction.Hover);
    }

    public void OnSubmit(BaseEventData eventData)
    {
        Play(UIAudioAction.Click);
    }
}
