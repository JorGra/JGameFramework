using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

namespace JGameFramework.UI.Tooltips
{
    public sealed class TooltipActionData
    {
        public string Label { get; }
        public Sprite Icon { get; }
        public bool CloseOnTrigger { get; }
        public bool Interactable { get; }
        public UnityAction<TooltipHandle, TooltipPlayerContext> Callback { get; }

        public TooltipActionData(string label, UnityAction<TooltipHandle, TooltipPlayerContext> callback, Sprite icon = null, bool interactable = true, bool closeOnTrigger = true)
        {
            Label = label;
            Callback = callback;
            Icon = icon;
            Interactable = interactable;
            CloseOnTrigger = closeOnTrigger;
        }

        internal void Invoke(TooltipHandle handle, TooltipPlayerContext context)
        {
            Callback?.Invoke(handle, context);
            if (CloseOnTrigger)
            {
                handle.Close();
            }
        }
    }

    [Serializable]
    public struct TooltipPlayerContext
    {
        public int PlayerIndex;
#if ENABLE_INPUT_SYSTEM
        public PlayerInput PlayerInput;
        public MultiplayerEventSystem MultiplayerEventSystem;
        public InputSystemUIInputModule InputModule;
#endif
        public EventSystem EventSystem;
        public Camera UICamera;

        public bool IsValid
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                if (PlayerInput != null || MultiplayerEventSystem != null || InputModule != null)
                {
                    return true;
                }
#endif
                if (EventSystem != null)
                {
                    return true;
                }

                return PlayerIndex >= 0;
            }
        }

        public bool MatchesEvent(BaseEventData eventData)
        {
            if (!IsValid)
            {
                return true;
            }

            if (eventData == null)
            {
                return true;
            }

            var module = eventData.currentInputModule;
            if (module == null)
            {
                return EventSystem == null && PlayerIndex < 0;
            }

            var resolvedEventSystem = module.GetComponent<EventSystem>();
            if (EventSystem != null && resolvedEventSystem == EventSystem)
            {
                return true;
            }

#if ENABLE_INPUT_SYSTEM
            if (InputModule != null && ReferenceEquals(module, InputModule))
            {
                return true;
            }

            if (MultiplayerEventSystem != null && resolvedEventSystem == MultiplayerEventSystem)
            {
                return true;
            }

            if (PlayerInput != null)
            {
                var modulePlayer = module.GetComponent<PlayerInput>();
                if (modulePlayer != null && modulePlayer == PlayerInput)
                {
                    return true;
                }
            }

            var resolvedMultiplayer = resolvedEventSystem as MultiplayerEventSystem;
            if (resolvedMultiplayer != null)
            {
                if (PlayerInput != null && resolvedMultiplayer.playerRoot != null)
                {
                    var rootPlayer = resolvedMultiplayer.playerRoot.GetComponent<PlayerInput>();
                    if (rootPlayer != null && rootPlayer == PlayerInput)
                    {
                        return true;
                    }
                }

                if (PlayerIndex >= 0 && resolvedMultiplayer.playerRoot != null)
                {
                    var rootPlayer = resolvedMultiplayer.playerRoot.GetComponent<PlayerInput>();
                    if (rootPlayer != null && rootPlayer.playerIndex == PlayerIndex)
                    {
                        return true;
                    }
                }
            }

            if (PlayerIndex >= 0)
            {
                var modulePlayer = module.GetComponent<PlayerInput>();
                if (modulePlayer != null && modulePlayer.playerIndex == PlayerIndex)
                {
                    return true;
                }
            }
#endif

            return EventSystem == null && PlayerIndex < 0;
        }
    }
}

