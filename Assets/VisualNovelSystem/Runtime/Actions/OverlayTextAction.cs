using System;
using System.Collections;
using UnityEngine;

namespace VisualNovelSystem
{
    [Serializable]
    public class OverlayTextAction : StoryAction
    {
        [TextArea(1, 3)]
        public string titleText = "Capítulo 1";

        [TextArea(1, 2)]
        public string subtitleText = "El Inicio";

        public OverlayDisplayMode displayMode = OverlayDisplayMode.CenterTitleCard;
        public OverlayEffect effect = OverlayEffect.Typewriter;
        public float duration = 2.0f;
        public bool waitForClick = true;
        public float fadeDuration = 0.5f;

        public override IEnumerator Execute(StoryRunner runner)
        {
            if (runner != null && runner.UIController != null)
            {
                runner.UIController.HideDialogue();
            }

            var overlayUI = runner != null && runner.UIController != null ? runner.UIController.OverlayUI : null;

            if (overlayUI != null)
            {
                yield return overlayUI.ShowOverlayRoutine(titleText, subtitleText, displayMode, effect, duration, waitForClick, fadeDuration);
            }
            else
            {
                Debug.Log($"[OverlayTextAction] {titleText} - {subtitleText}");
                if (waitForClick)
                {
                    while (!StoryInput.ContinuePressed())
                    {
                        yield return null;
                    }
                }
                else
                {
                    yield return new WaitForSeconds(duration);
                }
            }
        }

        public override string GetSummary()
        {
            string snippet = titleText.Length > 20 ? titleText.Substring(0, 17) + "..." : titleText;
            return $"Overlay Text: \"{snippet}\"";
        }
    }
}
