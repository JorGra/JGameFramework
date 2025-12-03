using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;


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
        public bool HasPlayerIndex;
        public PlayerInput PlayerInput;
        public MultiplayerEventSystem MultiplayerEventSystem;
        public InputSystemUIInputModule InputModule;

        public EventSystem EventSystem;
        public Camera UICamera;

        public bool IsValid => HasFilters;

        private bool HasFilters
        {
            get
            {
                if (HasPlayerIndex)
                {
                    return true;
                }

                if (PlayerInput != null || MultiplayerEventSystem != null || InputModule != null)
                {
                    return true;
                }

                return EventSystem != null;
            }
        }

        public bool MatchesEvent(BaseEventData eventData)
        {
            if (!HasFilters)
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
                return false;
            }

            var moduleEventSystem = module.GetComponent<EventSystem>();

            var resolvedMultiplayer = moduleEventSystem as MultiplayerEventSystem;

            if (InputModule != null && ReferenceEquals(module, InputModule))
            {
                return true;
            }

            if (MultiplayerEventSystem != null && resolvedMultiplayer == MultiplayerEventSystem)
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

                if (resolvedMultiplayer != null && resolvedMultiplayer.playerRoot != null)
                {
                    var rootPlayer = resolvedMultiplayer.playerRoot.GetComponent<PlayerInput>();
                    if (rootPlayer != null && rootPlayer == PlayerInput)
                    {
                        return true;
                    }
                }
            }

            if (HasPlayerIndex)
            {
                if (resolvedMultiplayer != null && resolvedMultiplayer.playerRoot != null)
                {
                    var rootPlayer = resolvedMultiplayer.playerRoot.GetComponent<PlayerInput>();
                    if (rootPlayer != null && rootPlayer.playerIndex == PlayerIndex)
                    {
                        return true;
                    }
                }

                var modulePlayer = module.GetComponent<PlayerInput>();
                if (modulePlayer != null && modulePlayer.playerIndex == PlayerIndex)
                {
                    return true;
                }
            }


            if (EventSystem != null && moduleEventSystem == EventSystem)
            {
                return true;
            }

            return false;
        }
    }
}
