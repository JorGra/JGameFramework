using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

namespace JGameFramework.UI.Tooltips
{
    public static class TooltipPlayerContextUtility
    {
        public static TooltipPlayerContext FromEventSystem(EventSystem eventSystem, Camera uiCamera = null, int playerIndex = -1)
        {
            var context = new TooltipPlayerContext
            {
                EventSystem = eventSystem,
                PlayerIndex = playerIndex,
                HasPlayerIndex = playerIndex >= 0,
                UICamera = uiCamera
            };

#if ENABLE_INPUT_SYSTEM
            if (eventSystem is MultiplayerEventSystem multiplayerEventSystem)
            {
                context.MultiplayerEventSystem = multiplayerEventSystem;
            }

            if (eventSystem != null)
            {
                var inputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
                if (inputModule != null)
                {
                    context.InputModule = inputModule;
                }
            }
#endif

            return context;
        }

#if ENABLE_INPUT_SYSTEM
        public static TooltipPlayerContext FromPlayerInput(PlayerInput playerInput, Camera uiCamera = null)
        {
            if (playerInput == null)
            {
                return default;
            }

            var context = new TooltipPlayerContext
            {
                PlayerInput = playerInput,
                PlayerIndex = playerInput.playerIndex,
                HasPlayerIndex = playerInput.playerIndex >= 0,
                UICamera = uiCamera
            };

            var eventSystem = playerInput.GetComponentInChildren<MultiplayerEventSystem>();
            if (eventSystem != null)
            {
                context.MultiplayerEventSystem = eventSystem;
                context.EventSystem = eventSystem;
            }

            InputSystemUIInputModule inputModule = playerInput.GetComponentInChildren<InputSystemUIInputModule>();
            if (inputModule == null)
            {
                inputModule = playerInput.uiInputModule;
            }

            if (inputModule != null)
            {
                context.InputModule = inputModule;

                if (context.EventSystem == null)
                {
                    var moduleEventSystem = inputModule.GetComponent<EventSystem>();
                    if (moduleEventSystem != null)
                    {
                        context.EventSystem = moduleEventSystem;
                        if (context.MultiplayerEventSystem == null)
                        {
                            context.MultiplayerEventSystem = moduleEventSystem as MultiplayerEventSystem;
                        }
                    }
                }
            }

            if (context.EventSystem == null)
            {
                context.EventSystem = playerInput.GetComponentInChildren<EventSystem>();
            }

            return context;
        }
#endif
    }
}
