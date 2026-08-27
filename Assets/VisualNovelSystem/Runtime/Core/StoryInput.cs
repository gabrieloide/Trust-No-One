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
            var mouse = Mouse.current;
            var keyboard = Keyboard.current;

            if (mouse != null && mouse.leftButton.wasPressedThisFrame) return true;
            if (keyboard == null) return false;

            return keyboard.spaceKey.wasPressedThisFrame
                || keyboard.enterKey.wasPressedThisFrame
                || keyboard.numpadEnterKey.wasPressedThisFrame;
        }

        public static Vector3 MousePosition()
        {
            var mouse = Mouse.current;
            return mouse != null ? (Vector3)mouse.position.ReadValue() : Vector3.zero;
        }
    }
}
