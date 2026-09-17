using UnityEngine;
using UnityEngine.InputSystem;

namespace VisualNovelSystem
{
    // El proyecto tiene el Input System nuevo como único backend activo (Player Settings
    // > Active Input Handling), donde UnityEngine.Input lanza InvalidOperationException.
    // Centraliza acá el polling de "avanzar/skip" y de posición del mouse para no
    // duplicar la lógica en cada acción/UI que lo necesita.
    public static class StoryInput
    {
        public static bool ContinuePressed()
        {
            // Touchscreen tap (Mobile devices)
            var touch = Touchscreen.current;
            if (touch != null && touch.primaryTouch.press.wasPressedThisFrame) return true;

            // Generic pointer press (covers touch, mouse, stylus across all platforms)
            var pointer = Pointer.current;
            if (pointer != null && pointer.press.wasPressedThisFrame) return true;

            // Mouse click (Desktop)
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame) return true;

            // Keyboard keys (Desktop)
            var keyboard = Keyboard.current;
            if (keyboard == null) return false;

            return keyboard.spaceKey.wasPressedThisFrame
                || keyboard.enterKey.wasPressedThisFrame
                || keyboard.numpadEnterKey.wasPressedThisFrame;
        }

        public static Vector3 MousePosition()
        {
            var pointer = Pointer.current;
            if (pointer != null) return (Vector3)pointer.position.ReadValue();

            var mouse = Mouse.current;
            return mouse != null ? (Vector3)mouse.position.ReadValue() : Vector3.zero;
        }
    }
}
