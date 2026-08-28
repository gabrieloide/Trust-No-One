using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VisualNovelSystem
{
    public class StoryUIController : MonoBehaviour
    {
        public static StoryUIController Instance { get; private set; }

        [Header("Sub-Controllers")]
        [SerializeField] private StoryDialogueUI dialogueUI;
        [SerializeField] private StoryChoiceUI choiceUI;
        [SerializeField] private StoryFader fader;
        [SerializeField] private StoryOverlayUI overlayUI;

        public StoryDialogueUI DialogueUI => dialogueUI;
        public StoryChoiceUI ChoiceUI => choiceUI;
        public StoryFader Fader => fader;
        public StoryOverlayUI OverlayUI => overlayUI;
        public bool IsModalActive => (choiceUI != null && choiceUI.IsActive) || (dialogueUI != null && dialogueUI.IsActive) || (overlayUI != null && overlayUI.IsActive);

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

            // Auto find if not assigned
            if (dialogueUI == null) dialogueUI = GetComponentInChildren<StoryDialogueUI>(true);
            if (choiceUI == null) choiceUI = GetComponentInChildren<StoryChoiceUI>(true);
            if (fader == null) fader = GetComponentInChildren<StoryFader>(true);
            if (overlayUI == null) overlayUI = GetComponentInChildren<StoryOverlayUI>(true);
        }

        public IEnumerator ShowDialogue(string speakerName, string text, Sprite portrait, AudioClip voiceClip, float speed = -1f, bool waitForClick = true)
        {
            if (dialogueUI != null)
            {
                yield return dialogueUI.ShowDialogueRoutine(speakerName, text, portrait, voiceClip, speed, waitForClick);
            }
            else
            {
                Debug.Log($"[StoryDialogue] {speakerName}: {text}");
                yield return new WaitForSeconds(1.5f);
            }
        }

        public void HideDialogue()
        {
            if (dialogueUI != null) dialogueUI.HideDialogue();
        }

        public IEnumerator ShowChoices(string prompt, List<StoryChoiceOption> options, Action<int> onSelected)
        {
            if (choiceUI != null)
            {
                yield return choiceUI.ShowChoicesRoutine(prompt, options, onSelected);
            }
            else
            {
                Debug.LogWarning("[StoryChoiceUI] No choice UI assigned. Selecting first option.");
                onSelected?.Invoke(0);
            }
        }

        public IEnumerator FadeScreen(float targetAlpha, Color color, float duration, AnimationCurve curve = null)
        {
            if (fader != null)
            {
                yield return fader.FadeRoutine(targetAlpha, color, duration, curve);
            }
            else
            {
                yield return new WaitForSeconds(duration);
            }

            if (targetAlpha >= 0.95f)
            {
                HideDialogue();
            }
        }

        public IEnumerator ShowOverlay(string title, string subtitle, OverlayDisplayMode mode, OverlayEffect effect, float duration, bool waitForClick, float fadeDuration = 0.5f)
        {
            HideDialogue();
            if (overlayUI != null)
            {
                yield return overlayUI.ShowOverlayRoutine(title, subtitle, mode, effect, duration, waitForClick, fadeDuration);
            }
            else
            {
                Debug.Log($"[StoryOverlay] {title} - {subtitle}");
                yield return new WaitForSeconds(duration);
            }
        }
    }
}
