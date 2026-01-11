using UnityEngine;

public class UIPanelAnimatedSFX : UIPanelAnimated
{
    [SerializeField] string SoundProfile = "Default";

    public override void Open()
    {
        base.Open();
        EventBus<PlayUISoundEvent>.Raise(new PlayUISoundEvent(SoundProfile,UIAudioAction.OpenPanel, transform.position));
    }

    public override void Close()
    {
        base.Close();
        EventBus<PlayUISoundEvent>.Raise(new PlayUISoundEvent(SoundProfile,UIAudioAction.ClosePanel, transform.position));
    }
}
