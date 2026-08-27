using System;
using System.Collections.Generic;
using UnityEngine;

namespace VisualNovelSystem
{
    public enum CursorIconType
    {
        Default,
        Inspect,   // Magnifying glass
        Interact,  // Hand / Grab
        Talk,      // Speech bubble
        Exit,      // Door / Direction arrow
        Custom
    }

    [Serializable]
    public class CursorDefinition
    {
        public CursorIconType iconType;
        public Texture2D cursorTexture;
        public Vector2 hotspot = Vector2.zero;
    }

    public class StoryCursorManager : MonoBehaviour
    {
        public static StoryCursorManager Instance { get; private set; }

        [SerializeField] private CursorMode cursorMode = CursorMode.Auto;
        [SerializeField] private List<CursorDefinition> cursorDefinitions = new List<CursorDefinition>();

        private CursorIconType currentIconType = CursorIconType.Default;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            ResetCursor();
        }

        public void SetCursor(CursorIconType type, Texture2D customTexture = null, Vector2? customHotspot = null)
        {
            currentIconType = type;

            if (type == CursorIconType.Custom && customTexture != null)
            {
                Cursor.SetCursor(customTexture, customHotspot ?? Vector2.zero, cursorMode);
                return;
            }

            var def = cursorDefinitions.Find(c => c.iconType == type);
            if (def != null && def.cursorTexture != null)
            {
                Cursor.SetCursor(def.cursorTexture, def.hotspot, cursorMode);
            }
            else
            {
                Cursor.SetCursor(null, Vector2.zero, cursorMode);
            }
        }

        public void ResetCursor()
        {
            SetCursor(CursorIconType.Default);
        }
    }
}
