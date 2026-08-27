using System;
using System.Collections;
using UnityEngine;

namespace VisualNovelSystem
{
    [Serializable]
    public class SetCursorAction : StoryAction
    {
        public CursorIconType cursorType = CursorIconType.Default;
        public Texture2D customTexture;

        public override IEnumerator Execute(StoryRunner runner)
        {
            if (StoryCursorManager.Instance != null)
            {
                StoryCursorManager.Instance.SetCursor(cursorType, customTexture);
            }
            yield break;
        }

        public override string GetSummary()
        {
            return $"Set Cursor -> {cursorType}";
        }
    }
}
