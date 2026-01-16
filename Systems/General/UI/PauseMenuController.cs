using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Generic pause menu controller that listens for input actions and pause menu events,
/// opens/closes the configured UI panel, and raises time flow events. Project-specific
/// behaviour (like local-coop control swapping) should live in derived classes outside
/// the framework.
/// </summary>
public class PauseMenuController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] protected UIPanel pauseMenuPanel;
    [SerializeField] protected GameObject firstSelected;

    [Header("Input")]
    [SerializeField] private InputActionReference toggleAction;
    [SerializeField] private InputActionReference resumeAction;

    [Header("Behaviour")]
    [SerializeField] protected bool pauseTime = true;
    [SerializeField] protected int timeEffectPriority = 1000;
    [SerializeField] protected string pauseEffectName = "PauseMenu";

    public bool IsPaused => _isPaused;

    private bool _isPaused;

    protected virtual void Awake()
    {
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.Close();
        }
    }

    protected virtual void OnEnable()
    {
        this.SubscribeEvent<PauseMenuEvent>(OnPauseMenuEvent);
        HookInputActions();
    }

    protected virtual void OnDisable()
    {
        UnhookInputActions();
    }

    private void HookInputActions()
    {
        AttachAction(toggleAction?.action, OnToggleActionPerformed);
        AttachAction(resumeAction?.action, OnResumeActionPerformed);
    }

    private void UnhookInputActions()
    {
        DetachAction(toggleAction?.action, OnToggleActionPerformed);
        DetachAction(resumeAction?.action, OnResumeActionPerformed);
    }

    private void AttachAction(InputAction action, System.Action<InputAction.CallbackContext> handler)
    {
        if (action == null)
        {
            return;
        }

        action.performed += handler;
        if (!action.enabled)
        {
            action.Enable();
        }
    }

    private void DetachAction(InputAction action, System.Action<InputAction.CallbackContext> handler)
    {
        if (action == null)
        {
            return;
        }

        action.performed -= handler;
    }

    private void OnToggleActionPerformed(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            EventBus<PauseMenuEvent>.Raise(new PauseMenuEvent(PauseMenuEvent.Action.Toggle, GetComponent<PlayerInput>()));
        }
    }

    private void OnResumeActionPerformed(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            EventBus<PauseMenuEvent>.Raise(new PauseMenuEvent(PauseMenuEvent.Action.RequestResume, GetComponent<PlayerInput>()));
        }
    }

    private void OnPauseMenuEvent(PauseMenuEvent pauseEvent)
    {
        switch (pauseEvent.action)
        {
            case PauseMenuEvent.Action.Toggle:
                if (_isPaused)
                {
                    TryResume(pauseEvent.playerInput);
                }
                else
                {
                    TryPause(pauseEvent.playerInput);
                }
                break;
            case PauseMenuEvent.Action.RequestPause:
                TryPause(pauseEvent.playerInput);
                break;
            case PauseMenuEvent.Action.RequestResume:
                TryResume(pauseEvent.playerInput);
                break;
        }
    }

    private void TryPause(PlayerInput requester)
    {
        if (_isPaused)
        {
            return;
        }

        if (BeginPause(requester))
        {
            _isPaused = true;
            OnPauseStarted(requester);
        }
    }

    private void TryResume(PlayerInput requester)
    {
        if (!_isPaused || !CanResume(requester))
        {
            return;
        }

        EndPause(requester);
        _isPaused = false;
        OnPauseEnded(requester);
    }

    protected virtual bool BeginPause(PlayerInput requester)
    {
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.Open();
            EventSystem.current.SetSelectedGameObject(firstSelected);
        }

        ApplyTimePause(true);
        return true;
    }

    protected virtual void EndPause(PlayerInput requester)
    {
        ApplyTimePause(false);

        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.Close();
        }
    }

    protected virtual bool CanResume(PlayerInput requester)
    {
        return true;
    }

    protected virtual void OnPauseStarted(PlayerInput requester) { }
    protected virtual void OnPauseEnded(PlayerInput requester) { }

    protected void ApplyTimePause(bool pause)
    {
        if (!pauseTime)
        {
            return;
        }

        if (pause)
        {
            EventBus<TimeFlowEvent>.Raise(new TimeFlowEvent(true, pauseEffectName, timeEffectPriority, 0f, 0f, 0f));
        }
        else
        {
            EventBus<TimeFlowEvent>.Raise(new TimeFlowEvent(false, pauseEffectName, 0, 1f, 0f, 0f));
        }
    }
}

public struct PauseMenuEvent : IEvent
{
    public enum Action
    {
        Toggle,
        RequestPause,
        RequestResume
    }

    public Action action;
    public PlayerInput playerInput;

    public PauseMenuEvent(Action action, PlayerInput playerInput)
    {
        this.action = action;
        this.playerInput = playerInput;
    }
}
