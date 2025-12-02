using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace JG.Inventory.UI
{
    /// <summary>
    /// Per-player helper that opens / closes the inventory UI using the new
    /// Input System and works with PlayerInputManager (split-screen).
    /// </summary>
    [RequireComponent(typeof(PlayerInput))]
    public class InventoryInputHandler : MonoBehaviour
    {
        [SerializeField] private InputActionReference toggleInventoryAction;
        [SerializeField] private Canvas inventoryCanvas;          // the UI to show/hide

        void OnEnable()
        {
            toggleInventoryAction.action.performed += Toggle;
            toggleInventoryAction.action.Enable();
        }

        void OnDisable()
        {
            toggleInventoryAction.action.performed -= Toggle;
            toggleInventoryAction.action.Disable();
        }

        void Toggle(InputAction.CallbackContext _)
        {
            bool newState = !inventoryCanvas.enabled;
            inventoryCanvas.enabled = newState;

            /* automatically give UI focus when opening with a pad */
            if (newState && Gamepad.current != null)
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(
                    inventoryCanvas.GetComponentInChildren<Button>()?.gameObject);
        }
    }
}
