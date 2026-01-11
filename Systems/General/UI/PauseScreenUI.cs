using UnityEngine;

public class PauseScreenUI : UIPanelAnimatedSFX
{
    public void PauseMenuCloseButtonPressed()
    {
        EventBus<PauseMenuEvent>.Raise(new PauseMenuEvent(PauseMenuEvent.Action.RequestResume, null));
    }
}
