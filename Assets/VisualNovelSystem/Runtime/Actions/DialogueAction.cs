using System;
using System.Collections;
using UnityEngine;

namespace VisualNovelSystem
{
    [Serializable]
    public class DialogueAction : StoryAction
    {
        public string speakerName = "Personaje";

        [TextArea(2, 5)]
        public string dialogueText = "Hola, esta es una línea de diálogo.";

        public Sprite characterPortrait;
        public AudioClip voiceClip;
        public float typewriterSpeed = 0.03f;
        public bool waitForClick = true;
        public bool hideAfterFinished = false;

        public override IEnumerator Execute(StoryRunner runner)
        {
            var ui = runner != null ? runner.UIController : null;
            if (ui != null)
            {
                yield return ui.ShowDialogue(speakerName, dialogueText, characterPortrait, voiceClip, typewriterSpeed, waitForClick);
                if (hideAfterFinished)
                {
                    ui.HideDialogue();
                }
            }
            else
            {
                Debug.Log($"[{speakerName}] {dialogueText}");
                if (waitForClick)
                {
                    while (!StoryInput.ContinuePressed())
                    {
                        yield return null;
                    }
                }
                else
                {
                    yield return new WaitForSeconds(1.5f);
                }
            }
        }

        public override string GetSummary()
        {
            string snippet = dialogueText.Length > 25 ? dialogueText.Substring(0, 22) + "..." : dialogueText;
            return $"{speakerName}: \"{snippet}\"";
        }
    }
}
